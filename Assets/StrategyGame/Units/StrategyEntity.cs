using UnityEngine;
using UnityEngine.AI;
namespace Engchanok.StrategyGame
{
    public sealed class StrategyEntity : MonoBehaviour
    {
        public EntityKind kind;
        public bool IsEnemy => kind == EntityKind.Enemy || kind == EntityKind.Runner || kind == EntityKind.Brute;
        public bool IsUnit => kind == EntityKind.Worker || kind == EntityKind.Soldier || IsEnemy;
        public HealthModel Health { get; private set; }
        public ProductionQueue Production { get; } = new();
        public bool Alive => Health != null && Health.IsAlive;
        public bool Selected { get; set; }
        public int Cargo { get; private set; }
        public NavMeshAgent Agent { get; private set; }
        public MineralDeposit MiningTarget { get; private set; }
        public StrategyEntity AttackTarget { get; private set; }
        StrategyMatch match;
        float attackTimer, mineTimer, stuckTimer;
        Vector3 lastPosition;
        public UnitOrder Order { get; private set; }
        public Vector3 OrderDestination { get; private set; }
        public Vector3? RallyPoint { get; private set; }
        bool commandedMove;
        Vector3 navigationGoal;
        float navigationStop = -1;
        LineRenderer shot;
        float shotTimer;
        public void Initialize(StrategyMatch owner)
        {
            match = owner; Health = new HealthModel(match.settings.Health(kind));
            Agent = GetComponent<NavMeshAgent>();
            if (Agent != null) Agent.speed = kind == EntityKind.Worker ? match.settings.workerSpeed : IsEnemy ? match.settings.EnemySpeed(kind) : match.settings.soldierSpeed;
            lastPosition = transform.position;
            match.Entities.Add(this);
            gameObject.AddComponent<StrategyFeedback>().Initialize(this, owner);
        }
        void OnDestroy() { if (match != null) match.Entities.Remove(this); }
        public void Damage(float amount)
        {
            if (!Alive) return;
            if (!match.Running || amount <= 0) return;
            Health.Damage(amount);
            GetComponent<StrategyFeedback>().Hit();
            if (!Alive) { StrategyFeedback.Burst(match, transform.position + Vector3.up, IsEnemy ? Color.red : Color.cyan); Selected = false; match.Entities.Remove(this); gameObject.SetActive(false); Destroy(gameObject); }
        }
        public bool Move(Vector3 destination)
        {
            if (!match.Running || !Alive || !IsUnit) return false;
            MiningTarget = null; AttackTarget = null; commandedMove = true; Order = UnitOrder.Move; OrderDestination = destination; mineTimer = 0;
            return Navigate(destination, .3f);
        }
        public void Attack(StrategyEntity target)
        {
            if (!match.Running || !Alive || kind != EntityKind.Soldier || target == null || !target.Alive || !target.IsEnemy) return;
            MiningTarget = null; commandedMove = false; AttackTarget = target; Order = UnitOrder.Attack;
        }
        public bool AttackMove(Vector3 destination)
        {
            if (!match.Running || !Alive || kind != EntityKind.Soldier) return false;
            if (!Move(destination)) return false;
            commandedMove = false; Order = UnitOrder.AttackMove; return true;
        }
        public bool SetRallyPoint(Vector3 destination)
        {
            if (!match.Running || !Alive || kind != EntityKind.Barracks || !NavMesh.SamplePosition(destination, out var hit, 1, NavMesh.AllAreas)) return false;
            var path = new NavMeshPath();
            if (!NavMesh.SamplePosition(transform.position + Vector3.forward * (match.settings.Radius(kind) + 1), out var origin, 3, NavMesh.AllAreas)
                || !NavMesh.CalculatePath(origin.position, hit.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) return false;
            RallyPoint = hit.position; return true;
        }
        public void Gather(MineralDeposit deposit)
        {
            if (!match.Running || !Alive || kind != EntityKind.Worker || deposit == null) return;
            MiningTarget = deposit; AttackTarget = null; commandedMove = false; Order = UnitOrder.Gather; mineTimer = 0;
        }
        bool Navigate(Vector3 destination, float stop)
        {
            if (Agent == null || !Agent.isOnNavMesh) return false;
            if (Agent.hasPath && Agent.pathStatus == NavMeshPathStatus.PathComplete && Vector3.Distance(destination, navigationGoal) < .25f && Mathf.Approximately(stop, navigationStop)) return true;
            if (!NavMesh.SamplePosition(destination, out var hit, Mathf.Max(3, stop + 1), NavMesh.AllAreas)) { Stop(); return false; }
            var path = new NavMeshPath();
            if (!Agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete) { Stop(); return false; }
            navigationGoal = destination; navigationStop = stop;
            Agent.stoppingDistance = Mathf.Max(.1f, stop - Vector3.Distance(destination, hit.position));
            Agent.isStopped = false; Agent.SetPath(path); return true;
        }
        void Stop() { if (Agent != null && Agent.isOnNavMesh) Agent.ResetPath(); commandedMove = false; }
        void Update()
        {
            if (match == null || !match.Running || !Alive) return;
            float dt = Time.deltaTime;
            shotTimer -= dt; if (shot != null && shotTimer <= 0) shot.enabled = false;
            attackTimer = Mathf.Max(0, attackTimer - dt);
            if (kind == EntityKind.Headquarters || kind == EntityKind.Barracks)
            {
                Production.Tick(dt);
                if (Production.Ready && match.TrySpawnUnit(kind == EntityKind.Headquarters ? EntityKind.Worker : EntityKind.Soldier, transform.position, out var trained)) { Production.Complete(); if (RallyPoint.HasValue) trained.AttackMove(RallyPoint.Value); }
            }
            if (kind == EntityKind.Worker) { UpdateMining(dt); return; }
            if (kind != EntityKind.Soldier && kind != EntityKind.Turret && !IsEnemy) return;
            if (AttackTarget == null || !AttackTarget.Alive) { AttackTarget = null; if (Order == UnitOrder.Attack) Order = UnitOrder.Idle; }
            float range = kind == EntityKind.Turret ? match.settings.turretRange : IsEnemy ? match.settings.enemyRange : match.settings.soldierRange;
            if (commandedMove)
            {
                if (!Agent.pathPending && (!Agent.hasPath || Agent.remainingDistance <= Agent.stoppingDistance + .2f)) commandedMove = false;
                DetectStuck(dt); return;
            }
            var nearby = match.NearestOpponent(this, IsEnemy ? 8 : range);
            if (nearby != null && Order != UnitOrder.Attack) AttackTarget = nearby;
            if (AttackTarget == null && IsEnemy) AttackTarget = match.Headquarters;
            if (AttackTarget == null)
            {
                if (Order == UnitOrder.AttackMove && Vector3.Distance(transform.position, OrderDestination) > .7f)
                { if (!Navigate(OrderDestination, .3f)) Order = UnitOrder.Idle; }
                else { Stop(); Order = UnitOrder.Idle; }
                return;
            }
            float reach = range + match.settings.Radius(AttackTarget.kind);
            if (Vector3.Distance(transform.position, AttackTarget.transform.position) <= reach)
            {
                Stop();
                if (attackTimer <= 0)
                {
                    attackTimer = match.settings.attackInterval;
                    ShowShot(AttackTarget.transform.position + Vector3.up);
                    AttackTarget.Damage(match.CombatDamage(kind));
                }
            }
            else if (Agent != null && !Navigate(AttackTarget.transform.position, reach * .85f)) { AttackTarget = null; if (Order == UnitOrder.Attack) Order = UnitOrder.Idle; }
        }
        void UpdateMining(float dt)
        {
            if (commandedMove)
            {
                if (Agent != null && Agent.isOnNavMesh && !Agent.pathPending && (!Agent.hasPath || Agent.remainingDistance <= Agent.stoppingDistance + .2f)) commandedMove = false;
                DetectStuck(dt);
                return;
            }
            if (Cargo > 0 && match.Headquarters != null && match.Headquarters.Alive)
            {
                Vector3 home = match.Headquarters.transform.position;
                if (Vector3.Distance(transform.position, home) < 4.2f) { match.Deliver(Cargo); Cargo = 0; Stop(); }
                else if (!Navigate(home, 3.6f)) { MiningTarget = null; }
                return;
            }
            if (MiningTarget == null || MiningTarget.Stock.Remaining <= 0) { MiningTarget = null; DetectStuck(dt); return; }
            if (Vector3.Distance(transform.position, MiningTarget.transform.position) > 2.6f)
            {
                if (!Navigate(MiningTarget.transform.position, 2.2f)) MiningTarget = null;
                return;
            }
            Stop(); mineTimer += dt;
            if (mineTimer >= match.settings.miningSeconds) { mineTimer = 0; Cargo = MiningTarget.Extract(match.WorkerCapacity); }
        }
        void DetectStuck(float dt)
        {
            if (Agent == null || !Agent.isOnNavMesh || !Agent.hasPath) return;
            stuckTimer = Vector3.Distance(transform.position, lastPosition) < .01f ? stuckTimer + dt : 0;
            lastPosition = transform.position;
            if (stuckTimer > 3) { Stop(); MiningTarget = null; AttackTarget = null; stuckTimer = 0; }
        }
        void ShowShot(Vector3 end)
        {
            if (shot == null)
            {
                shot = gameObject.AddComponent<LineRenderer>(); shot.positionCount = 2;
                shot.startWidth = .09f; shot.endWidth = .035f;
                shot.material = match.beamMaterial; shot.useWorldSpace = true;
            }
            GetComponent<StrategyFeedback>().Fire();
            shot.enabled = true; shot.SetPosition(0, transform.position + Vector3.up * 1.5f); shot.SetPosition(1, end); shotTimer = .12f;
        }
    }
}
