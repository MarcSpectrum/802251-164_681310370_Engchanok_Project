using System.Collections.Generic;
using UnityEngine;
namespace Engchanok.StrategyGame
{
    public sealed class StrategyFeedback : MonoBehaviour
    {
        public static bool Muted;
        static readonly Color Frost = new(.6f, .85f, 1);
        static readonly Dictionary<(int, int), AudioClip> clips = new();
        StrategyEntity entity;
        StrategyMatch match;
        Renderer[] bodies;
        MaterialPropertyBlock tint;
        LineRenderer ring, rally;
        float flash;
        public void Initialize(StrategyEntity target, StrategyMatch owner)
        {
            entity = target; match = owner; bodies = GetComponentsInChildren<Renderer>(); tint = new MaterialPropertyBlock();
            ring = Ring(transform, owner.beamMaterial, owner.settings.Radius(entity.kind) + .25f, Color.cyan);
            rally = Ring(transform, owner.beamMaterial, .8f, new Color(.2f, 1, .7f));
        }
        public static LineRenderer Ring(Transform parent, Material material, float radius, Color color)
        {
            var line = new GameObject("Ground indicator").AddComponent<LineRenderer>(); line.transform.SetParent(parent, false);
            line.sharedMaterial = material; line.useWorldSpace = false; line.loop = true; line.positionCount = 40;
            line.startWidth = line.endWidth = .07f; line.startColor = line.endColor = color;
            var tint = new MaterialPropertyBlock(); tint.SetColor("_BaseColor", color); line.SetPropertyBlock(tint);
            for (int i = 0; i < 40; i++) { float a = i * Mathf.PI * 2 / 40; line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, .07f, Mathf.Sin(a) * radius)); }
            return line;
        }
        public void Hit() { flash = .12f; StrategyEffects.For(match).Emit(transform.position+Vector3.up, transform.position+Vector3.up*1.6f, entity.IsEnemy ? new Color(1,.35f,.3f) : Color.cyan, .16f, .18f); }
        public void Fire() { StrategyEffects.For(match).Emit(transform.position+Vector3.up*1.5f, transform.position+Vector3.up*1.8f, new Color(1,.8f,.35f), .09f, .25f); }
        void Update()
        {
            ring.enabled = entity.Selected;
            rally.enabled = entity.Selected && entity.RallyPoint.HasValue;
            if (entity.RallyPoint.HasValue) rally.transform.position = entity.RallyPoint.Value;
            if (!match.Running) return;
            flash = Mathf.Max(0, flash - Time.deltaTime);
            // A hit flash wins over the cryo tint, so a frozen hostile still shows every shot that lands.
            foreach (var body in bodies)
            {
                if (flash > 0) { tint.SetColor("_BaseColor", Color.white); body.SetPropertyBlock(tint); }
                else if (entity.Chilled) { tint.SetColor("_BaseColor", Frost); body.SetPropertyBlock(tint); }
                else body.SetPropertyBlock(null);
            }
        }
        public static void Burst(StrategyMatch owner, Vector3 position, Color color)
        {
            var pool=StrategyEffects.For(owner);
            for(int i=0;i<6;i++) { float angle=i*Mathf.PI/3; pool.Emit(position,position+new Vector3(Mathf.Cos(angle),.4f,Mathf.Sin(angle))*1.2f,color,.3f,.12f); }
            pool.Emit(new Vector3(position.x,.1f,position.z),Vector3.zero,color,.4f,1.3f,true);
            Sound(owner,100,.16f);
        }
        public static void Marker(StrategyMatch owner, Vector3 position, Color color)
        {
            StrategyEffects.For(owner).Emit(position+Vector3.up*.1f,Vector3.zero,color,.8f,.9f,true); Sound(owner,700,.06f);
        }
        public static void Construct(StrategyMatch owner, Vector3 position)
        {
            StrategyEffects.For(owner).Emit(position+Vector3.up*.12f,Vector3.zero,new Color(.2f,1,.7f),.7f,2.5f,true); Sound(owner,420,.2f);
        }
        public static void Sound(StrategyMatch owner, int frequency, float duration)
        {
            if (Muted || !owner.Running) return;
            var key=(frequency, Mathf.CeilToInt(22050 * duration));
            if (!clips.TryGetValue(key, out var clip) || clip == null)
            {
                int count = Mathf.CeilToInt(22050 * duration); var data = new float[count];
                for (int i = 0; i < count; i++) data[i] = Mathf.Sin(i * 2 * Mathf.PI * frequency / 22050) * .12f * (1f - (float)i / count);
                clip = AudioClip.Create("Outpost cue", count, 1, 22050, false); clip.SetData(data, 0); clips[key] = clip;
            }
            var source = owner.GetComponent<AudioSource>(); if (source == null) source = owner.gameObject.AddComponent<AudioSource>();
            source.PlayOneShot(clip);
        }
    }
}
