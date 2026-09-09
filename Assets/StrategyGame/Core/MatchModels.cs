using System;
using System.Collections.Generic;

namespace Engchanok.StrategyGame
{
    public enum EntityKind { Headquarters, Worker, Soldier, Barracks, Turret, Enemy, Runner, Brute }
    public enum UnitOrder { Idle, Move, Attack, AttackMove, Gather }
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
    }
    public sealed class Wallet
    {
        public int Minerals { get; private set; }
        public Wallet(int amount) { Minerals = Math.Max(0, amount); }
        public bool TrySpend(int amount) { if (amount < 0 || amount > Minerals) return false; Minerals -= amount; return true; }
        public void Deposit(int amount) { Minerals += Math.Max(0, amount); }
    }
    public sealed class MineralStock
    {
        public int Remaining { get; private set; }
        public MineralStock(int amount) { Remaining = Math.Max(0, amount); }
        public int Extract(int capacity) { int amount = Math.Min(Remaining, Math.Max(0, capacity)); Remaining -= amount; return amount; }
    }
    public sealed class ProductionQueue
    {
        readonly Queue<float> jobs = new();
        public int Count => jobs.Count;
        public float Remaining { get; private set; }
        public bool Enqueue(Wallet wallet, int cost, float seconds)
        {
            if (Count >= 5 || !wallet.TrySpend(cost)) return false;
            jobs.Enqueue(Math.Max(.1f, seconds));
            if (Count == 1) Remaining = jobs.Peek();
            return true;
        }
        // A ready job remains queued until its producer finds a valid spawn point.
        public void Tick(float delta) { if (Count > 0) Remaining = Math.Max(0, Remaining - Math.Max(0, delta)); }
        public bool Ready => Count > 0 && Remaining <= 0;
        public bool Complete() { if (!Ready) return false; jobs.Dequeue(); Remaining = Count > 0 ? jobs.Peek() : 0; return true; }
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
