using System.Collections.Generic;
using UnityEngine;
namespace Engchanok.StrategyGame
{
    // Carries out commander powers on the match clock: strike impacts, lingering fields, the jet and the zone outlines.
    // Scene-owned like StrategyEffects, so it freezes on pause, clears when the mission ends and unloads with the scene.
    // StrategyMatch.UsePower owns the rules (price, recharge, target); this component only executes what was paid for.
    public sealed class StrategyPowers : MonoBehaviour
    {
        sealed class Strike
        {
            public PowerProfile power;
            public Vector3 point;
            public float age;
            public List<(float time, float x, float z)> impacts;
            public int nextRelease, nextImpact;
            public readonly List<(Transform bomb, Vector3 from, float released)> bombs = new();
            public Transform jet;
            public LineRenderer outline;
        }
        sealed class Field
        {
            public PowerProfile power;
            public Vector3 point;
            public float age, pulse;
            public LineRenderer outline;
        }
        // Bombs fall and shells streak in for this long before they land.
        public const float FallSeconds = .35f, JetHeight = 14, JetExit = 45;
        static readonly Color StrikeColor = new(1, .45f, .3f), BlastColor = new(1, .6f, .25f), CryoColor = new(.55f, .85f, 1), RepairColor = new(.4f, 1, .6f);
        // Coral for anything that explodes, ice for cryo, green for repair: the zone, the aiming preview and the HUD share these.
        public static Color ColorOf(PowerKind kind) => kind == PowerKind.CryoField ? CryoColor : kind == PowerKind.RepairField ? RepairColor : StrikeColor;
        readonly List<Strike> strikes = new();
        readonly List<Field> fields = new();
        StrategyMatch match;
        Material hull, glow;
        int seed = 7919;
        public int ActiveStrikes => strikes.Count;
        public int ActiveFields => fields.Count;
        public int ActiveJets { get { int count = 0; foreach (var s in strikes) if (s.jet != null && s.jet.gameObject.activeSelf) count++; return count; } }
        public static StrategyPowers For(StrategyMatch owner)
        {
            var powers = owner.GetComponent<StrategyPowers>();
            if (powers == null) { powers = owner.gameObject.AddComponent<StrategyPowers>(); powers.match = owner; }
            return powers;
        }
        public void Launch(PowerProfile power, Vector3 point)
        {
            if (match == null || power == null || !match.Running) return;
            point.y = 0;
            if (power.IsStrike)
            {
                var strike = new Strike { power = power, point = point, impacts = PowerPlan.Impacts(power, seed = seed * 31 + 17), outline = Outline(power, point) };
                if (power.kind == PowerKind.Airstrike) { strike.jet = BuildJet(); PlaceJet(strike); StrategyFeedback.Sound(match, 140, .9f); }
                else StrategyFeedback.Sound(match, 260, .25f);
                strikes.Add(strike);
            }
            else
            {
                fields.Add(new Field { power = power, point = point, age = -power.delay, outline = Outline(power, point) });
                StrategyFeedback.Sound(match, power.kind == PowerKind.CryoField ? 1300 : 620, .3f);
            }
        }
        // The jet is over each release point exactly FallSeconds before that bomb lands, so it flies one straight line at JetSpeed.
        static Vector3 JetPosition(Strike s)
        {
            float releaseTime = s.power.delay - FallSeconds;
            return new Vector3(s.point.x, JetHeight, s.point.z - s.power.radius + (s.age - releaseTime) * PowerPlan.JetSpeed);
        }
        static void PlaceJet(Strike s) { if (s.jet != null) s.jet.position = JetPosition(s); }
        void Update()
        {
            if (match == null) return;
            if (match.Waves != null && match.Waves.Result != MatchResult.Playing) { Clear(); return; }
            if (!match.Running) return;
            float dt = Time.deltaTime;
            for (int i = strikes.Count - 1; i >= 0; i--) if (AdvanceStrike(strikes[i], dt)) { Dispose(strikes[i]); strikes.RemoveAt(i); }
            for (int i = fields.Count - 1; i >= 0; i--) if (AdvanceField(fields[i], dt)) { if (fields[i].outline != null) Destroy(fields[i].outline.gameObject); fields.RemoveAt(i); }
        }
        // Returns true once every impact has landed and the jet, if any, has left the battlefield.
        bool AdvanceStrike(Strike s, float dt)
        {
            s.age += dt;
            bool airstrike = s.power.kind == PowerKind.Airstrike;
            while (s.nextRelease < s.impacts.Count && s.age >= s.impacts[s.nextRelease].time - FallSeconds)
            {
                var (_, x, z) = s.impacts[s.nextRelease++];
                var ground = s.point + new Vector3(x, 0, z);
                if (airstrike) s.bombs.Add((Bomb(ground + Vector3.up * JetHeight), ground + Vector3.up * JetHeight, s.age));
                else StrategyEffects.For(match).Emit(ground + new Vector3(-2, 18, -6), ground, BlastColor, FallSeconds, .16f);
            }
            for (int b = s.bombs.Count - 1; b >= 0; b--)
            {
                var (bomb, from, released) = s.bombs[b];
                if (bomb == null) { s.bombs.RemoveAt(b); continue; }
                float t = Mathf.Clamp01((s.age - released) / FallSeconds);
                bomb.position = Vector3.Lerp(from, new Vector3(from.x, 0, from.z), t * t);
            }
            while (s.nextImpact < s.impacts.Count && s.age >= s.impacts[s.nextImpact].time)
            {
                var (_, x, z) = s.impacts[s.nextImpact++];
                Detonate(s.point + new Vector3(x, 0, z), s.power);
                // Bombs are released in impact order, so the oldest one is the one landing.
                if (airstrike && s.bombs.Count > 0) { if (s.bombs[0].bomb != null) Destroy(s.bombs[0].bomb.gameObject); s.bombs.RemoveAt(0); }
            }
            PlaceJet(s);
            bool landed = s.nextImpact >= s.impacts.Count;
            if (landed && s.outline != null) { Destroy(s.outline.gameObject); s.outline = null; }
            if (s.jet != null && s.jet.position.z > s.point.z + JetExit) s.jet.gameObject.SetActive(false);
            return landed && (s.jet == null || !s.jet.gameObject.activeSelf);
        }
        void Detonate(Vector3 ground, PowerProfile power)
        {
            StrategyFeedback.Burst(match, ground + Vector3.up * .5f, BlastColor);
            StrategyEffects.For(match).Emit(ground + Vector3.up * .12f, Vector3.zero, BlastColor, .5f, power.blast / 1.2f, true);
            match.ApplyBlast(ground, power.blast, power.amount);
        }
        // Returns true when the field has expired.
        bool AdvanceField(Field f, float dt)
        {
            f.age += dt; f.pulse -= dt;
            bool cryo = f.power.kind == PowerKind.CryoField;
            if (f.age < 0) return false;
            if (f.age >= f.power.duration) return true;
            bool pulse = f.pulse <= 0;
            if (pulse) { f.pulse = .6f; StrategyEffects.For(match).Emit(f.point + Vector3.up * .12f, Vector3.zero, cryo ? CryoColor : RepairColor, .6f, f.power.radius / 1.2f, true); }
            foreach (var entity in match.Entities.ToArray())
            {
                if (entity == null || !entity.Alive || entity.IsEnemy != cryo) continue;
                var offset = entity.transform.position - f.point; offset.y = 0;
                if (offset.magnitude > f.power.radius + match.settings.Radius(entity.kind)) continue;
                // Cryo is refreshed every frame, so hostiles walking in are slowed and those walking out recover shortly after.
                if (cryo) entity.Chill(f.power.amount, .3f);
                else if (entity.Health.Heal(f.power.amount * dt) > 0 && pulse)
                    StrategyEffects.For(match).Emit(entity.transform.position + Vector3.up * .6f, entity.transform.position + Vector3.up * 2.2f, RepairColor, .45f, .12f);
            }
            return false;
        }
        LineRenderer Outline(PowerProfile power, Vector3 point)
        {
            var line = StrategyFeedback.Ring(transform, match.beamMaterial, power.radius, ColorOf(power.kind));
            line.name = StrategySettings.PowerName(power.kind) + " zone"; line.gameObject.layer = 2;
            line.startWidth = line.endWidth = .14f;
            Shape(line, power); line.transform.position = point;
            return line;
        }
        // The shape a power covers, in the line's local space: a stadium along the airstrike's flight line, a circle otherwise.
        public static void Shape(LineRenderer line, PowerProfile power)
        {
            int count = line.positionCount = 40; line.loop = true;
            if (power.kind == PowerKind.Airstrike)
            {
                int half = count / 2;
                for (int i = 0; i < count; i++)
                {
                    bool far = i < half;
                    float a = Mathf.PI * (far ? i / (float)(half - 1) : 1 + (i - half) / (float)(half - 1));
                    line.SetPosition(i, new Vector3(Mathf.Cos(a) * power.blast, .07f, (far ? power.radius : -power.radius) + Mathf.Sin(a) * power.blast));
                }
                return;
            }
            for (int i = 0; i < count; i++) { float a = i * Mathf.PI * 2 / count; line.SetPosition(i, new Vector3(Mathf.Cos(a) * power.radius, .07f, Mathf.Sin(a) * power.radius)); }
        }
        void EnsureMaterials()
        {
            if (hull != null) return;
            var lit = Shader.Find("Universal Render Pipeline/Lit"); var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            hull = lit != null ? new Material(lit) : new Material(match.beamMaterial); hull.SetColor("_BaseColor", new Color(.13f, .18f, .26f));
            glow = unlit != null ? new Material(unlit) : new Material(match.beamMaterial); glow.SetColor("_BaseColor", StrategyUI.Accent);
        }
        // A geometric strike jet in the outpost's navy-and-cyan style. Render-only: no colliders, and it ignores raycasts.
        Transform BuildJet()
        {
            EnsureMaterials();
            var root = new GameObject("Strike jet").transform; root.SetParent(transform, false); root.gameObject.layer = 2;
            Part(root, new Vector3(0, 0, 0), new Vector3(.8f, .55f, 4.2f), Vector3.zero, hull);
            Part(root, new Vector3(0, -.02f, 2.35f), new Vector3(.5f, .38f, .9f), Vector3.zero, hull);
            Part(root, new Vector3(1.3f, -.05f, -.4f), new Vector3(2.6f, .12f, 1.2f), new Vector3(0, 20, 0), hull);
            Part(root, new Vector3(-1.3f, -.05f, -.4f), new Vector3(2.6f, .12f, 1.2f), new Vector3(0, -20, 0), hull);
            Part(root, new Vector3(0, .08f, -1.85f), new Vector3(2.1f, .1f, .6f), Vector3.zero, hull);
            Part(root, new Vector3(0, .55f, -1.8f), new Vector3(.12f, .85f, .75f), Vector3.zero, hull);
            Part(root, new Vector3(0, .32f, .95f), new Vector3(.42f, .22f, 1.1f), Vector3.zero, glow);
            Part(root, new Vector3(0, 0, -2.13f), new Vector3(.5f, .34f, .1f), Vector3.zero, glow);
            return root;
        }
        Transform Bomb(Vector3 position)
        {
            EnsureMaterials();
            var bomb = Part(transform, Vector3.zero, new Vector3(.35f, .35f, .7f), Vector3.zero, hull, PrimitiveType.Sphere);
            bomb.name = "Strike bomb"; bomb.position = position; return bomb;
        }
        static Transform Part(Transform parent, Vector3 position, Vector3 scale, Vector3 euler, Material material, PrimitiveType shape = PrimitiveType.Cube)
        {
            var part = GameObject.CreatePrimitive(shape); part.layer = 2;
            Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false); part.transform.localPosition = position; part.transform.localScale = scale; part.transform.localEulerAngles = euler;
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part.transform;
        }
        void Dispose(Strike s)
        {
            if (s.jet != null) Destroy(s.jet.gameObject);
            if (s.outline != null) Destroy(s.outline.gameObject);
            foreach (var (bomb, _, _) in s.bombs) if (bomb != null) Destroy(bomb.gameObject);
            s.bombs.Clear();
        }
        void Clear()
        {
            foreach (var s in strikes) Dispose(s);
            foreach (var f in fields) if (f.outline != null) Destroy(f.outline.gameObject);
            strikes.Clear(); fields.Clear();
        }
        void OnDestroy()
        {
            if (hull != null) Destroy(hull);
            if (glow != null) Destroy(glow);
        }
    }
}
