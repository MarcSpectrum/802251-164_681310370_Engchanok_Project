using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
namespace Engchanok.StrategyGame
{
    public sealed class StrategyMatch : MonoBehaviour
    {
        public StrategySettings settings;
        public StrategyEntity[] prefabs;
        public Material beamMaterial;
        public readonly List<StrategyEntity> Entities = new();
        public ResearchState Research { get; private set; }
        public bool Practice { get; private set; }
        public int TutorialStep { get; private set; }
        public int DeliveredMinerals { get; private set; }
        public bool TutorialAttackIssued { get; set; }
        public int WorkerCapacity => Mathf.RoundToInt(settings.workerCapacity * (Research.Completed(UpgradeKind.Mining) ? 1 + settings.miningResearchBonus : 1));
        // Soldier Weapons research covers every barracks-line trooper; Turret Weapons covers emplacements.
        public float CombatDamage(EntityKind kind) => kind == EntityKind.Turret ? settings.turretDamage * (Research.Completed(UpgradeKind.TurretWeapons) ? 1 + settings.turretResearchBonus : 1)
            : settings.Damage(kind) * (!IsHostileKind(kind) && Research.Completed(UpgradeKind.SoldierWeapons) ? 1 + settings.soldierResearchBonus : 1);
        // One list, so a new hostile is declared in exactly one place. Static because StrategyEntity.IsEnemy has no settings to consult,
        // and load-bearing: IsUnitKind, supply exclusion, prefab generation and targeting all derive from it.
        public static readonly EntityKind[] Hostiles = { EntityKind.Enemy, EntityKind.Runner, EntityKind.Brute, EntityKind.Lancer, EntityKind.Breaker, EntityKind.Warden, EntityKind.Juggernaut };
        public static bool IsHostileKind(EntityKind kind) => System.Array.IndexOf(Hostiles, kind) >= 0;
        // Research bonus and the armor counter compose multiplicatively.
        public float CombatDamage(EntityKind attacker, EntityKind target) => CombatDamage(attacker) * settings.DamageScale(attacker, target);
        public void Deliver(int amount) { Wallet.Deposit(amount); DeliveredMinerals += amount; }
        public bool CanResearch(UpgradeKind kind, out string reason)
        {
            reason = !System.Enum.IsDefined(typeof(UpgradeKind), kind) ? "Unknown research" : !Running ? "Mission is paused or finished" : Headquarters == null || !Headquarters.Alive ? "Headquarters unavailable" : Research.Completed(kind) ? "Completed" : Research.Active.HasValue ? "Research already in progress" : Wallet.Minerals < settings.ResearchCost(kind) ? "Need " + (settings.ResearchCost(kind) - Wallet.Minerals) + " more minerals" : "Ready to research";
            return reason == "Ready to research";
        }
        public bool StartResearch(UpgradeKind kind)
        {
            if (!CanResearch(kind, out var reason)) { Notify(reason); return false; }
            return Research.Start(kind, Wallet, settings.ResearchCost(kind), settings.ResearchSeconds(kind));
        }
        public void FinishPractice()
        {
            if (!Practice) return;
            PlayerPrefs.SetInt(StrategySession.TutorialKey, 1); PlayerPrefs.Save();
            StrategySession.PracticeRequested = false; Time.timeScale = 1; SceneManager.LoadScene("Survival");
        }
        void UpdateTutorial()
        {
            var commander = GetComponent<StrategyCommander>();
            bool done = TutorialStep switch {
                0 => commander.Selection.Exists(e => e != null && e.kind == EntityKind.Worker),
                1 => DeliveredMinerals > 0,
                2 => Entities.Exists(e => e != null && e.kind == EntityKind.Barracks),
                3 => Entities.Exists(e => e != null && e.kind == EntityKind.Soldier),
                4 => TutorialAttackIssued,
                5 => Research.Completed(UpgradeKind.Mining) || Research.Completed(UpgradeKind.SoldierWeapons) || Research.Completed(UpgradeKind.TurretWeapons),
                _ => false };
            if (done) TutorialStep++;
        }
        public Wallet Wallet { get; private set; }
        public SupplyModel Supply { get; private set; }
        public WaveState Waves { get; private set; }
        public StrategyEntity Headquarters { get; private set; }
        public bool Paused { get; private set; }
        public bool Running => !Paused && Waves != null && Waves.Result == MatchResult.Playing;
        public static readonly Vector3 HomePosition = new(0, 0, -12);
        public static readonly Vector3[] SpawnPoints = { new(-29, 0, 31), new(0, 0, 33), new(29, 0, 31) };
        public string Notice { get; private set; } = "Select workers, then right-click a mineral deposit.";
        float noticeTime;
        readonly Queue<EntityKind> pendingEnemies = new();
        public int HostileCount => pendingEnemies.Count + Entities.FindAll(e => e != null && e.IsEnemy && e.Alive).Count;
        public int IdleWorkerCount => Entities.FindAll(e => e != null && e.IsIdleWorker).Count;
        public MatchStats Stats { get; private set; }
        // The most recent friendly position under fire, for the minimap ring and the Space shortcut.
        public Vector3 LastAlertPosition { get; private set; }
        public float LastAlertTime { get; private set; } = -999;
        public bool HasAlert => LastAlertTime > -999;
        public const float AlertCooldown = 8, AlertSeparation = 12, AlertMinimumGap = 2;
        readonly List<(Vector3 position, float time)> alertSites = new();
        // Throttled so a long fight produces one notice rather than one per hit. Every recent site is remembered, not just the
        // last one, or two simultaneous fights far apart would alternate and alert on nearly every hit; the minimum gap caps the rest.
        public void ReportAttack(StrategyEntity victim)
        {
            if (Practice || victim == null || victim.IsEnemy) return;
            float now = Time.time;
            if (now - LastAlertTime < AlertMinimumGap) return;
            Vector3 position = victim.transform.position;
            for (int i = alertSites.Count - 1; i >= 0; i--)
            {
                if (now - alertSites[i].time >= AlertCooldown) alertSites.RemoveAt(i);
                else if (Vector3.Distance(position, alertSites[i].position) <= AlertSeparation) return;
            }
            alertSites.Add((position, now));
            LastAlertPosition = position; LastAlertTime = now;
            Notify(StrategySettings.Label(victim.kind) + " under attack. [Space] to view.");
            StrategyFeedback.Sound(this, 330, .18f);
        }
        public void RecordDeath(StrategyEntity entity)
        {
            if (entity != null && Stats != null && !Practice) Stats.RecordDeath(entity.IsEnemy, entity.IsUnit);
        }
        public void RecordTrained() { if (Stats != null && !Practice) Stats.UnitsTrained++; }
        void Start()
        {
            Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            Practice = StrategySession.PracticeRequested; StrategySession.PracticeRequested = false;
            Stats = new MatchStats();
            Research = new ResearchState();
            Wallet = new Wallet(Practice ? 1000 : settings.startingMinerals);
            Supply = new SupplyModel(settings.supplyLimit);
            Waves = new WaveState(settings.waveCount, settings.preparationSeconds, settings.betweenWaveSeconds);
            foreach (var deposit in FindObjectsByType<MineralDeposit>(FindObjectsSortMode.None)) deposit.Initialize(settings.depositMinerals);
            Headquarters = Spawn(EntityKind.Headquarters, HomePosition);
            for (int i = 0; i < 3; i++) TrySpawnUnit(EntityKind.Worker, HomePosition + new Vector3(-3 + i * 3, 0, -3), out _);
        }
        // Supply is derived from the live entity list every frame, so deaths and cancelled producers correct themselves with no bookkeeping.
        public void RecountSupply()
        {
            if (Supply == null || settings == null) return;
            int used = 0, cap = 0;
            foreach (var entity in Entities)
            {
                if (entity == null || !entity.Alive || entity.IsEnemy) continue;
                used += settings.Supply(entity.kind);
                cap += settings.SupplyProvided(entity.kind);
                // Queued jobs reserve their own supply up front, so a mixed queue reserves what it will actually cost.
                foreach (var queued in entity.Production.Queued) used += settings.Supply(queued);
            }
            Supply.Recount(used, cap);
        }
        void Update()
        {
            RecountSupply();
            if (!Running) return;
            if (Headquarters == null || !Headquarters.Alive) { Waves.Tick(0, HostileCount, false); Time.timeScale = 0; return; }
            if (Research.Tick(Time.deltaTime)) { Notify("Research complete. Your forces are upgraded."); StrategyFeedback.Sound(this, 1050, .28f); }
            if (noticeTime > 0) { noticeTime -= Time.deltaTime; if (noticeTime <= 0) Notice = ""; }
            if (Practice) { UpdateTutorial(); return; }
            Stats.Elapsed += Time.deltaTime;
            if (pendingEnemies.Count > 0)
                for (int i = 0; i < SpawnPoints.Length && pendingEnemies.Count > 0; i++)
                    if (TrySpawnUnit(pendingEnemies.Peek(), SpawnPoints[i], out _)) pendingEnemies.Dequeue();
            int enemies = HostileCount;
            if (Waves.Tick(Time.deltaTime, enemies, Headquarters != null && Headquarters.Alive))
            {
                foreach (var kind in settings.Composition(Waves.Wave).Enemies()) pendingEnemies.Enqueue(kind);
                StrategyFeedback.Sound(this, 220, .3f);
                Notify("Wave " + Waves.Wave + " incoming!");
            }
            if (Waves.Result != MatchResult.Playing) Time.timeScale = 0;
        }
        public void Notify(string message) { Notice = message; noticeTime = 6; }
        public void SetPaused(bool paused) { Paused = paused; ApplyPause(); }
        void ApplyPause() { Time.timeScale = Paused || (Waves != null && Waves.Result != MatchResult.Playing) ? 0 : 1; }
        public void Restart() { StrategySession.PracticeRequested = Practice; Time.timeScale = 1; SceneManager.LoadScene("Survival"); }
        public void MainMenu() { Time.timeScale = 1; SceneManager.LoadScene("MainMenu"); }
        void OnDestroy() { Time.timeScale = 1; }
        public StrategyEntity Spawn(EntityKind kind, Vector3 position)
        {
            // A scene saved before a new kind existed still carries the old, shorter prefab array.
            if (prefabs == null || (int)kind >= prefabs.Length || prefabs[(int)kind] == null)
            { Notify("No " + kind + " prefab. Run Strategy Game > Rebuild Prototype."); return null; }
            var entity = Instantiate(prefabs[(int)kind], position, Quaternion.identity);
            entity.name = kind.ToString(); entity.Initialize(this); return entity;
        }
        public bool TrySpawnUnit(EntityKind kind, Vector3 origin, out StrategyEntity entity)
        {
            entity = null;
            for (int i = 0; i < 24; i++)
            {
                float angle = i * 137.5f * Mathf.Deg2Rad;
                Vector3 point = origin + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (3.6f + i * .22f);
                if (!NavMesh.SamplePosition(point, out var hit, 1.5f, NavMesh.AllAreas)) continue;
                bool occupied = Entities.Exists(e => e != null && e.Alive && Vector3.Distance(e.transform.position, hit.position) < settings.Radius(e.kind) + .65f);
                if (occupied) continue;
                entity = Spawn(kind, hit.position); return entity != null;
            }
            return false;
        }
        // The producer's first roster entry, used by the one-argument overload and by rally/progress defaults.
        public EntityKind? DefaultTrained(EntityKind producer)
        {
            if (settings.units != null) foreach (var profile in settings.units) if (profile != null && profile.producer == producer) return profile.kind;
            return null;
        }
        public bool Train(StrategyEntity producer)
        {
            if (producer == null) return false;
            var kind = DefaultTrained(producer.kind);
            return kind.HasValue && Train(producer, kind.Value);
        }
        public bool Train(StrategyEntity producer, EntityKind kind)
        {
            if (!Running || producer == null || !producer.Alive || producer.IsEnemy) return false;
            var profile = settings.Profile(kind);
            if (profile == null || profile.producer != producer.kind) return false;
            RecountSupply();
            if (!Supply.Fits(profile.supply)) { Notify("Supply is full (" + Supply.Used + "/" + Supply.Cap + "). Build a supply relay."); return false; }
            bool ok = producer.Production.Enqueue(kind, Wallet, profile.cost, profile.trainSeconds);
            if (!ok) Notify("Not enough minerals, or production queue is full (5).");
            else RecountSupply();
            return ok;
        }
        public bool CanPlace(EntityKind kind, Vector3 point, out string reason)
        {
            reason = "";
            if (!IsBuildable(kind)) { reason = "Select a buildable structure."; return false; }
            float radius = settings.Radius(kind);
            if (Headquarters == null || !Headquarters.Alive) { reason = "Headquarters is unavailable."; return false; }
            if (Vector3.Distance(point, HomePosition) + radius > settings.buildRadius) { reason = "Build inside the headquarters perimeter."; return false; }
            foreach (var start in SpawnPoints)
            {
                Vector3 line = HomePosition - start;
                float t = Mathf.Clamp01(Vector3.Dot(point - start, line) / line.sqrMagnitude);
                if (Vector3.Distance(point, start + line * t) < radius + 2.5f) { reason = "Keep the marked approach lanes clear."; return false; }
            }
            if (Entities.Exists(e => e != null && e.Alive && Vector3.Distance(point, e.transform.position) < radius + settings.Radius(e.kind) + .6f)) { reason = "Space is occupied."; return false; }
            foreach (var deposit in FindObjectsByType<MineralDeposit>(FindObjectsSortMode.None))
                if (Vector3.Distance(point, deposit.transform.position) < radius + 2.8f) { reason = "Leave room around mineral deposits."; return false; }
            if (!NavMesh.SamplePosition(point, out var hit, .3f, NavMesh.AllAreas) || Vector3.Distance(point, hit.position) > .4f) { reason = "Choose navigable ground."; return false; }
            if (Wallet.Minerals < settings.Cost(kind)) { reason = "Not enough minerals."; return false; }
            return true;
        }
        public bool Build(EntityKind kind, Vector3 point)
        {
            if (!Running) return false;
            if (!CanPlace(kind, point, out var reason)) { Notify(reason); return false; }
            if (!Wallet.TrySpend(settings.Cost(kind))) return false;
            if (Spawn(kind, point) == null) { Wallet.Refund(settings.Cost(kind)); return false; }
            if (!Practice) Stats.StructuresBuilt++;
            StrategyFeedback.Construct(this, point); Notify(StrategySettings.Label(kind) + " ready."); RecountSupply(); return true;
        }
        public static readonly EntityKind[] Buildable = { EntityKind.Barracks, EntityKind.RangerPost, EntityKind.SupportBay, EntityKind.Turret, EntityKind.SupplyRelay };
        public static bool IsBuildable(EntityKind kind) => System.Array.IndexOf(Buildable, kind) >= 0;
        public static bool IsDefence(EntityKind kind) => IsBuildable(kind);
        // Runners harass anything soft, breakers hunt the armored screen, brutes and siege units take the structures,
        // everything else pushes the headquarters. Each rung falls back to the next so no hostile is ever left without a target.
        public StrategyEntity PriorityTarget(StrategyEntity hunter)
        {
            if (hunter == null || !hunter.IsEnemy) return null;
            return settings.Priority(hunter.kind) switch
            {
                HostilePriority.SoftTargets => NearestFriendly(hunter, IsSoft) ?? NearestFriendly(hunter, IsDefence) ?? Headquarters,
                HostilePriority.ArmoredTargets => NearestFriendly(hunter, IsArmored) ?? NearestFriendly(hunter, IsDefence) ?? Headquarters,
                HostilePriority.Structures => NearestFriendly(hunter, IsDefence) ?? Headquarters,
                _ => Headquarters,
            };
        }
        // Light armor is exactly the set runners are built to punish: workers, rangers, medics and engineers.
        bool IsSoft(EntityKind kind) => settings.Armor(kind) == ArmorClass.Light;
        // Heavy armor is the defender screen a breaker exists to break.
        bool IsArmored(EntityKind kind) => settings.Armor(kind) == ArmorClass.Heavy;
        StrategyEntity NearestFriendly(StrategyEntity source, System.Func<EntityKind, bool> wanted)
        {
            StrategyEntity best = null; float distance = float.MaxValue;
            foreach (var entity in Entities)
            {
                if (entity == null || !entity.Alive || entity.IsEnemy || !wanted(entity.kind)) continue;
                float d = Vector3.Distance(source.transform.position, entity.transform.position);
                if (d < distance) { best = entity; distance = d; }
            }
            return best;
        }
        // Medics mend units, engineers mend structures; the two never compete for the same target.
        // Same-side rather than friendly-side, so a hostile warden mends its own wave through this identical scan.
        public StrategyEntity NearestWounded(StrategyEntity source, float range, bool buildings)
        {
            StrategyEntity best = null; float distance = range;
            foreach (var entity in Entities)
            {
                if (entity == null || !entity.Alive || entity.IsEnemy != source.IsEnemy || entity == source) continue;
                if (entity.IsUnit == buildings || entity.Health.Current >= entity.Health.Maximum) continue;
                float d = Vector3.Distance(source.transform.position, entity.transform.position) - settings.Radius(entity.kind);
                if (d < distance) { best = entity; distance = d; }
            }
            return best;
        }
        public StrategyEntity NearestOpponent(StrategyEntity source, float range)
        {
            StrategyEntity best = null; float distance = range;
            foreach (var entity in Entities)
            {
                if (entity == null || !entity.Alive || entity.IsEnemy == source.IsEnemy) continue;
                float d = Vector3.Distance(source.transform.position, entity.transform.position) - settings.Radius(entity.kind);
                if (d < distance) { best = entity; distance = d; }
            }
            return best;
        }
    }
}
