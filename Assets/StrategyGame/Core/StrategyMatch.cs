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
        public Wallet Wallet { get; private set; }
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
        void Start()
        {
            Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            Wallet = new Wallet(settings.startingMinerals);
            Waves = new WaveState(settings.waveCount, settings.preparationSeconds, settings.betweenWaveSeconds);
            foreach (var deposit in FindObjectsByType<MineralDeposit>(FindObjectsSortMode.None)) deposit.Initialize(settings.depositMinerals);
            Headquarters = Spawn(EntityKind.Headquarters, HomePosition);
            for (int i = 0; i < 3; i++) TrySpawnUnit(EntityKind.Worker, HomePosition + new Vector3(-3 + i * 3, 0, -3), out _);
        }
        void Update()
        {
            if (!Running) return;
            if (noticeTime > 0) { noticeTime -= Time.deltaTime; if (noticeTime <= 0) Notice = ""; }
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
        public void SetPaused(bool paused) { Paused = paused; Time.timeScale = paused || Waves.Result != MatchResult.Playing ? 0 : 1; }
        public void Restart() { Time.timeScale = 1; SceneManager.LoadScene("Survival"); }
        public void MainMenu() { Time.timeScale = 1; SceneManager.LoadScene("MainMenu"); }
        void OnDestroy() { Time.timeScale = 1; }
        public StrategyEntity Spawn(EntityKind kind, Vector3 position)
        {
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
                entity = Spawn(kind, hit.position); return true;
            }
            return false;
        }
        public bool Train(StrategyEntity producer)
        {
            if (!Running || producer == null || !producer.Alive || producer.IsEnemy) return false;
            if (producer.kind != EntityKind.Headquarters && producer.kind != EntityKind.Barracks) return false;
            var kind = producer.kind == EntityKind.Headquarters ? EntityKind.Worker : EntityKind.Soldier;
            bool ok = producer.Production.Enqueue(Wallet, settings.Cost(kind), kind == EntityKind.Worker ? settings.workerTraining : settings.soldierTraining);
            if (!ok) Notify("Not enough minerals, or production queue is full (5).");
            return ok;
        }
        public bool CanPlace(EntityKind kind, Vector3 point, out string reason)
        {
            reason = "";
            if (kind != EntityKind.Barracks && kind != EntityKind.Turret) { reason = "Select a buildable structure."; return false; }
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
            Spawn(kind, point); Notify(kind + " ready."); return true;
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
