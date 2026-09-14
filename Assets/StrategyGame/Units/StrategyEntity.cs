using UnityEngine;
using UnityEngine.AI;
namespace Engchanok.StrategyGame
{
    public sealed class StrategyEntity : MonoBehaviour
    {
        public EntityKind kind;
        public bool IsEnemy => StrategyMatch.IsHostileKind(kind);
        // Load-bearing: the prefab generator keys on this to decide agent-vs-obstacle, so a unit missing here cannot move.
        public static bool IsUnitKind(EntityKind kind) => kind == EntityKind.Worker || kind == EntityKind.Soldier || kind == EntityKind.Ranger
            || kind == EntityKind.Defender || kind == EntityKind.Medic || kind == EntityKind.Engineer || StrategyMatch.IsHostileKind(kind);
        public static bool IsFighter(EntityKind kind) => kind == EntityKind.Soldier || kind == EntityKind.Ranger || kind == EntityKind.Defender || kind == EntityKind.Engineer;
        public bool IsUnit => IsUnitKind(kind);
        public HealthModel Health { get; private set; }
        public ProductionQueue Production { get; } = new();
        public bool Alive => Health != null && Health.IsAlive;
        public bool Selected { get; set; }
        public int Cargo { get; private set; }
        public NavMeshAgent Agent { get; private set; }
        public MineralDeposit MiningTarget { get; private set; }
        public StrategyEntity AttackTarget { get; private set; }
        StrategyMatch match;
        float attackTimer, mineTimer, stuckTimer, supportTimer, repairDebt;
        Vector3 lastPosition;
        public UnitOrder Order { get; private set; }
        public Vector3 OrderDestination { get; private set; }
        public Vector3? RallyPoint { get; private set; }
        bool commandedMove;
        Vector3 navigationGoal;
        float navigationStop = -1;
        public void Initialize(StrategyMatch owner)
        {
            match = owner; Health = new HealthModel(match.settings.Health(kind));
            Agent = GetComponent<NavMeshAgent>();
            if (Agent != null) Agent.speed = match.settings.Speed(kind);
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
            if (!match.Running || !Alive || !IsFighter(kind) || target == null || !target.Alive || !target.IsEnemy) return;
            MiningTarget = null; commandedMove = false; AttackTarget = target; Order = UnitOrder.Attack;
        }
        // Medics and engineers accept attack-move so they advance with the army; they simply support instead of shooting.
        public bool AttackMove(Vector3 destination)
        {
            if (!match.Running || !Alive || !IsUnit || IsEnemy || kind == EntityKind.Worker) return false;
            if (!Move(destination)) return false;
            commandedMove = false; Order = UnitOrder.AttackMove; return true;
        }
        public bool SetRallyPoint(Vector3 destination)
        {
            if (!match.Running || !Alive || !match.settings.IsProducer(kind) || !NavMesh.SamplePosition(destination, out var hit, 1, NavMesh.AllAreas)) return false;
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

            attackTimer = Mathf.Max(0, attackTimer - dt);
            if (match.settings.IsProducer(kind))
            {
                Production.Tick(dt);
                // The queue owns the kind, so one producer can hold several unit types at once.
                if (Production.Ready && Production.Next.HasValue && match.TrySpawnUnit(Production.Next.Value, transform.position, out var trained)) { Production.Complete(); if (RallyPoint.HasValue) trained.AttackMove(RallyPoint.Value); }
            }
            if (kind == EntityKind.Worker) { UpdateMining(dt); return; }
            if (kind == EntityKind.Medic) { UpdateSupport(dt, true); return; }
            // An engineer only fights when there is nothing left to mend.
            if (kind == EntityKind.Engineer && !commandedMove && UpdateSupport(dt, false)) return;
            if (!IsFighter(kind) && kind != EntityKind.Turret && !IsEnemy) return;
            if (AttackTarget == null || !AttackTarget.Alive) { AttackTarget = null; if (Order == UnitOrder.Attack) Order = UnitOrder.Idle; }
            float range = match.settings.Range(kind);
            if (commandedMove)
            {
                if (!Agent.pathPending && (!Agent.hasPath || Agent.remainingDistance <= Agent.stoppingDistance + .2f)) commandedMove = false;
                DetectStuck(dt); return;
            }
            var nearby = match.NearestOpponent(this, IsEnemy ? 8 : range);
            if (nearby != null && Order != UnitOrder.Attack) AttackTarget = nearby;
            if (AttackTarget == null && IsEnemy) AttackTarget = match.PriorityTarget(this);
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
                    AttackTarget.Damage(match.CombatDamage(kind, AttackTarget.kind));
                }
            }
            else if (Agent != null && !Navigate(AttackTarget.transform.position, reach * .85f)) { AttackTarget = null; if (Order == UnitOrder.Attack) Order = UnitOrder.Idle; }
        }
        // Medics mend wounded units for free; engineers mend damaged structures and pay minerals for every point restored.
        bool UpdateSupport(float dt, bool units)
        {
            if (commandedMove)
            {
                if (Agent != null && Agent.isOnNavMesh && !Agent.pathPending && (!Agent.hasPath || Agent.remainingDistance <= Agent.stoppingDistance + .2f)) commandedMove = false;
                DetectStuck(dt); return true;
            }
            float range = match.settings.Range(kind);
            var patient = match.NearestWounded(this, Mathf.Max(range, 30), !units);
            if (patient == null)
            {
                if (Order == UnitOrder.AttackMove && Vector3.Distance(transform.position, OrderDestination) > .7f)
                { if (!Navigate(OrderDestination, .3f)) Order = UnitOrder.Idle; return true; }
                AttackTarget = null; if (Order == UnitOrder.Heal || Order == UnitOrder.Repair) Order = UnitOrder.Idle;
                return false;
            }
            AttackTarget = patient; Order = units ? UnitOrder.Heal : UnitOrder.Repair;
            float reach = range + match.settings.Radius(patient.kind);
            if (Vector3.Distance(transform.position, patient.transform.position) > reach)
            { if (!Navigate(patient.transform.position, reach * .85f)) { AttackTarget = null; return false; } return true; }
            Stop();
            float amount = (units ? match.settings.medicHealPerSecond : match.settings.engineerRepairPerSecond) * dt;
            if (!units)
            {
                // Repair is paid for in minerals, so holding a turret together competes with building the next one.
                repairDebt += amount * match.settings.repairMineralsPerHundredHealth / 100f;
                int due = Mathf.FloorToInt(repairDebt);
                if (due > 0)
                {
                    if (!match.Wallet.TrySpend(due)) { repairDebt = 0; return true; }
                    repairDebt -= due;
                }
            }
            if (patient.Health.Heal(amount) > 0 && supportTimer <= 0)
            {
                supportTimer = .25f;
                StrategyEffects.For(match).Emit(transform.position + Vector3.up * 1.4f, patient.transform.position + Vector3.up, units ? new Color(.45f, 1, .6f) : new Color(1, .85f, .4f), .18f, .06f);
            }
            supportTimer -= dt;
            return true;
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
            GetComponent<StrategyFeedback>().Fire();
            if(!IsEnemy) StrategyEffects.For(match).Emit(transform.position+Vector3.up*1.5f,end,new Color(.65f,1,1),.12f,.07f);
        }
    }
}
