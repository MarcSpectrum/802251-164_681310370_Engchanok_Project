using System;
using System.Collections.Generic;

namespace Engchanok.StrategyGame
{
    public enum UpgradeKind { Mining, SoldierWeapons, TurretWeapons }
    public sealed class ResearchState
    {
        readonly HashSet<UpgradeKind> completed = new();
        public UpgradeKind? Active { get; private set; }
        public float Remaining { get; private set; }
        public bool Completed(UpgradeKind kind) => completed.Contains(kind);
        public bool Start(UpgradeKind kind, Wallet wallet, int cost, float seconds)
        {
            if (!Enum.IsDefined(typeof(UpgradeKind), kind) || Active.HasValue || Completed(kind) || seconds <= 0 || !wallet.TrySpend(cost)) return false;
            Active = kind; Remaining = seconds; return true;
        }
        public bool Tick(float delta)
        {
            if (!Active.HasValue) return false;
            Remaining = Math.Max(0, Remaining - Math.Max(0, delta));
            if (Remaining > 0) return false;
            completed.Add(Active.Value); Active = null; return true;
        }
    }
    // New kinds append to the end: StrategyMatch indexes its prefab array by enum value and the scene serializes that array by index.
    public enum EntityKind { Headquarters, Worker, Soldier, Barracks, Turret, Enemy, Runner, Brute, SupplyRelay, Ranger, Defender, Medic, Engineer, RangerPost, SupportBay }
    public enum ArmorClass { Light, Medium, Heavy, Structure }
    public enum UnitOrder { Idle, Move, Attack, AttackMove, Gather, Heal, Repair }
    // The full stat line for one trainable unit. Authored in DefaultStrategy; see EveryTrainableKindHasACompleteProfile.
    [Serializable]
    public sealed class UnitProfile
    {
        public EntityKind kind, producer;
        public int cost, supply;
        public float health, damage, range, speed, trainSeconds;
    }
    // Armor class and outgoing counter multipliers. Every entity that attacks or can be attacked needs a row.
    [Serializable]
    public sealed class ArmorProfile
    {
        public EntityKind kind;
        public ArmorClass armor = ArmorClass.Structure;
        public float vsLight = 1, vsMedium = 1, vsHeavy = 1;
        public float Scale(ArmorClass target) => target == ArmorClass.Light ? vsLight : target == ArmorClass.Medium ? vsMedium : target == ArmorClass.Heavy ? vsHeavy : 1;
    }
    [Serializable]
    public sealed class WaveComposition
    {
        public int standard, runners, brutes;
        public WaveComposition(int standard, int runners, int brutes) { this.standard = standard; this.runners = runners; this.brutes = brutes; }
        public IEnumerable<EntityKind> Enemies()
        {
            for (int i = 0; i < Math.Max(0, standard); i++) yield return EntityKind.Enemy;
            for (int i = 0; i < Math.Max(0, runners); i++) yield return EntityKind.Runner;
            for (int i = 0; i < Math.Max(0, brutes); i++) yield return EntityKind.Brute;
        }
    }
    public enum MatchResult { Playing, Victory, Defeat }
    public sealed class HealthModel
    {
        public float Maximum { get; }
        public float Current { get; private set; }
        public bool IsAlive => Current > 0;
        public HealthModel(float maximum) { Maximum = Math.Max(1, maximum); Current = Maximum; }
        public float Damage(float amount) { float before = Current; Current = Math.Max(0, Current - Math.Max(0, amount)); return before - Current; }
        // Healing never resurrects: a dead entity has already been removed from the match.
        public float Heal(float amount) { if (!IsAlive || amount <= 0) return 0; float before = Current; Current = Math.Min(Maximum, Current + amount); return Current - before; }
    }
    public sealed class Wallet
    {
        public int Minerals { get; private set; }
        public Wallet(int amount) { Minerals = Math.Max(0, amount); }
        public bool TrySpend(int amount) { if (amount < 0 || amount > Minerals) return false; Minerals -= amount; return true; }
        public void Deposit(int amount) { Minerals += Math.Max(0, amount); }
    }
    public sealed class SupplyModel
    {
        public int Used { get; private set; }
        public int Cap { get; private set; }
        public int Limit { get; }
        public SupplyModel(int limit) { Limit = Math.Max(1, limit); }
        public int Free => Math.Max(0, Cap - Used);
        public bool Full => Used >= Cap;
        // Callers pass a used count that already includes queued jobs, so a full queue cannot overshoot the cap.
        public void Recount(int used, int cap) { Used = Math.Max(0, used); Cap = Math.Clamp(cap, 0, Limit); }
        public bool Fits(int cost) => cost >= 0 && Used + cost <= Cap;
    }
    public sealed class MineralStock
    {
        public int Remaining { get; private set; }
        public MineralStock(int amount) { Remaining = Math.Max(0, amount); }
        public int Extract(int capacity) { int amount = Math.Min(Remaining, Math.Max(0, capacity)); Remaining -= amount; return amount; }
    }
    public sealed class ProductionQueue
    {
        // Each job carries its own kind, so one producer can queue several unit types.
        readonly Queue<(EntityKind kind, float seconds)> jobs = new();
        public int Count => jobs.Count;
        public float Remaining { get; private set; }
        public EntityKind? Next => jobs.Count > 0 ? jobs.Peek().kind : null;
        public IEnumerable<EntityKind> Queued { get { foreach (var job in jobs) yield return job.kind; } }
        public bool Enqueue(EntityKind kind, Wallet wallet, int cost, float seconds)
        {
            if (Count >= 5 || !wallet.TrySpend(cost)) return false;
            jobs.Enqueue((kind, Math.Max(.1f, seconds)));
            if (Count == 1) Remaining = jobs.Peek().seconds;
            return true;
        }
        // A ready job remains queued until its producer finds a valid spawn point.
        public void Tick(float delta) { if (Count > 0) Remaining = Math.Max(0, Remaining - Math.Max(0, delta)); }
        public bool Ready => Count > 0 && Remaining <= 0;
        public bool Complete() { if (!Ready) return false; jobs.Dequeue(); Remaining = Count > 0 ? jobs.Peek().seconds : 0; return true; }
    }
    public sealed class WaveState
    {
        public int Wave { get; private set; }
        public int Total { get; }
        public float Countdown { get; private set; }
        public bool Active { get; private set; }
        public MatchResult Result { get; private set; }
        readonly float breakSeconds;
        public WaveState(int total, float preparation, float between) { Total = Math.Max(1, total); Countdown = preparation; breakSeconds = between; }
        public bool Tick(float delta, int enemies, bool headquartersAlive)
        {
            if (Result != MatchResult.Playing) return false;
            if (!headquartersAlive) { Result = MatchResult.Defeat; return false; }
            if (Active)
            {
                if (enemies > 0) return false;
                Active = false;
                if (Wave >= Total) { Result = MatchResult.Victory; return false; }
                Countdown = breakSeconds;
                return false;
            }
            Countdown -= Math.Max(0, delta);
            if (Countdown > 0) return false;
            Wave++; Active = true; Countdown = 0; return true;
        }
    }
}
