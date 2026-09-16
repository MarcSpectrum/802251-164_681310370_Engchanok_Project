using NUnit.Framework;
using System.Linq;
using UnityEngine;
namespace Engchanok.StrategyGame.Tests
{
    public sealed class MatchModelTests
    {
        [Test] public void ResearchSpendingProgressAndCompletionAreAtomic()
        {
            var wallet=new Wallet(300); var research=new ResearchState();
            Assert.IsFalse(research.Start(UpgradeKind.Mining,wallet,301,20));
            Assert.IsFalse(research.Start((UpgradeKind)99,wallet,10,20));
            Assert.AreEqual(300,wallet.Minerals);
            Assert.IsTrue(research.Start(UpgradeKind.Mining,wallet,125,20));
            Assert.IsFalse(research.Start(UpgradeKind.SoldierWeapons,wallet,150,25));
            Assert.AreEqual(175,wallet.Minerals);
            Assert.IsFalse(research.Tick(19)); Assert.AreEqual(1,research.Remaining);
            research.Tick(-10); Assert.AreEqual(1,research.Remaining);
            Assert.IsTrue(research.Tick(1)); Assert.IsTrue(research.Completed(UpgradeKind.Mining));
            Assert.IsFalse(research.Start(UpgradeKind.Mining,wallet,125,20)); Assert.AreEqual(175,wallet.Minerals);
            Assert.IsTrue(research.Start(UpgradeKind.TurretWeapons,wallet,150,25));
            Assert.IsFalse(new ResearchState().Completed(UpgradeKind.Mining));
        }
        [Test] public void WaveCompositionPreservesTypesAndRejectsNegativeCounts()
        {
            var wave = new WaveComposition(2, 3, 1).Enemies().ToArray();
            Assert.AreEqual(6, wave.Length); Assert.AreEqual(3, wave.Count(k => k == EntityKind.Runner));
            Assert.AreEqual(EntityKind.Brute, wave.Last()); Assert.IsEmpty(new WaveComposition(-1,-2,-3).Enemies());
        }
        [Test] public void LegacySettingsKeepCountFormulaAndVariantsScaleFromBase()
        {
            var settings = ScriptableObject.CreateInstance<StrategySettings>();
            try { Assert.AreEqual(21, settings.Composition(5).Enemies().Count());
                Assert.AreEqual(settings.enemyHealth * 2.5f,settings.Health(EntityKind.Brute));
                Assert.AreEqual(settings.enemySpeed * 1.6f,settings.EnemySpeed(EntityKind.Runner)); }
            finally { Object.DestroyImmediate(settings); }
        }
        [Test] public void SupplyCountsQueuedJobsAndClampsToLimit()
        {
            var supply=new SupplyModel(20);
            supply.Recount(0,10); Assert.AreEqual(10,supply.Cap); Assert.AreEqual(10,supply.Free); Assert.IsFalse(supply.Full);
            // A caller that folds queued jobs into the used count cannot overshoot the cap.
            supply.Recount(9,10); Assert.IsTrue(supply.Fits(1)); Assert.IsFalse(supply.Fits(2)); Assert.IsFalse(supply.Fits(-1));
            supply.Recount(10,10); Assert.IsTrue(supply.Full); Assert.AreEqual(0,supply.Free); Assert.IsTrue(supply.Fits(0));
            supply.Recount(-5,999); Assert.AreEqual(0,supply.Used); Assert.AreEqual(20,supply.Cap);
            supply.Recount(0,-3); Assert.AreEqual(0,supply.Cap);
            Assert.AreEqual(1,new SupplyModel(0).Limit);
        }
        // The shipped asset, not a fresh instance: these tables are hand-authored YAML and Unity drops mistyped keys silently.
        static StrategySettings Shipped() => UnityEditor.AssetDatabase.LoadAssetAtPath<StrategySettings>("Assets/StrategyGame/Data/DefaultStrategy.asset");
        static readonly EntityKind[] Trainable = { EntityKind.Worker, EntityKind.Soldier, EntityKind.Ranger, EntityKind.Defender, EntityKind.Medic, EntityKind.Engineer };
        static readonly EntityKind[] Combatants = { EntityKind.Soldier, EntityKind.Ranger, EntityKind.Defender, EntityKind.Turret, EntityKind.Enemy, EntityKind.Runner, EntityKind.Brute, EntityKind.Lancer, EntityKind.Breaker, EntityKind.Warden, EntityKind.Juggernaut };
        [Test] public void EveryTrainableKindHasACompleteProfile()
        {
            var settings=Shipped();
            Assert.IsNotNull(settings,"DefaultStrategy.asset must load.");
            foreach(var kind in Trainable)
            {
                var profile=settings.Profile(kind);
                Assert.IsNotNull(profile,kind+" has no unit profile.");
                Assert.Greater(profile.cost,0,kind+" cost");
                Assert.Greater(profile.supply,0,kind+" supply");
                Assert.Greater(profile.health,0,kind+" health");
                Assert.Greater(profile.speed,0,kind+" speed");
                Assert.Greater(profile.trainSeconds,0,kind+" training time");
                Assert.IsTrue(StrategyMatch.IsBuildable(profile.producer) || profile.producer==EntityKind.Headquarters,kind+" is produced by a non-building.");
                Assert.IsTrue(StrategyEntity.IsUnitKind(kind),kind+" must count as a unit or the generator builds it as a structure.");
            }
            foreach(var kind in Combatants)
            {
                var armor=settings.ArmorOf(kind);
                Assert.IsNotNull(armor,kind+" has no armor profile.");
                Assert.Greater(armor.vsLight,0,kind+" vsLight");
                Assert.Greater(armor.vsMedium,0,kind+" vsMedium");
                Assert.Greater(armor.vsHeavy,0,kind+" vsHeavy");
            }
            // Every producer named by the roster must itself be placeable, or the unit is unreachable.
            foreach(var kind in Trainable)
                Assert.IsTrue(settings.IsProducer(settings.Profile(kind).producer),kind+"'s producer is not recognised as a producer.");
        }
        [Test] public void CounterTableAnswersEachArmorClassAndCoversHostiles()
        {
            var settings=Shipped();
            Assert.AreEqual(ArmorClass.Light,settings.Armor(EntityKind.Runner));
            Assert.AreEqual(ArmorClass.Medium,settings.Armor(EntityKind.Enemy));
            Assert.AreEqual(ArmorClass.Heavy,settings.Armor(EntityKind.Brute));
            Assert.AreEqual(ArmorClass.Heavy,settings.Armor(EntityKind.Defender));
            Assert.AreEqual(ArmorClass.Structure,settings.Armor(EntityKind.SupplyRelay));
            // Rangers answer Light, defenders and turrets answer Heavy, and neither covers the other.
            Assert.Greater(settings.DamageScale(EntityKind.Ranger,EntityKind.Runner),settings.DamageScale(EntityKind.Soldier,EntityKind.Runner));
            Assert.Greater(settings.DamageScale(EntityKind.Defender,EntityKind.Brute),settings.DamageScale(EntityKind.Soldier,EntityKind.Brute));
            Assert.Less(settings.DamageScale(EntityKind.Ranger,EntityKind.Brute),1);
            Assert.Less(settings.DamageScale(EntityKind.Turret,EntityKind.Runner),1);
            // Armor now defends: brutes flatten Light and glance off Heavy, which is what makes a Defender screen work.
            Assert.Greater(settings.DamageScale(EntityKind.Brute,EntityKind.Ranger),1);
            Assert.Less(settings.DamageScale(EntityKind.Brute,EntityKind.Defender),1);
            Assert.Greater(settings.DamageScale(EntityKind.Runner,EntityKind.Worker),settings.DamageScale(EntityKind.Runner,EntityKind.Defender));
            // Structures are exempt in both directions, so headquarters and turret balance are untouched.
            Assert.AreEqual(1,settings.DamageScale(EntityKind.Soldier,EntityKind.Headquarters));
            Assert.AreEqual(1,settings.DamageScale(EntityKind.Brute,EntityKind.Turret));
            Assert.AreEqual(1,settings.DamageScale(EntityKind.Worker,EntityKind.Runner));
        }
        // Unity drops mistyped YAML keys without complaint, so the shipped hostile table is asserted the same way the unit table is.
        [Test] public void EveryHostileKindHasACompleteProfile()
        {
            var settings=Shipped();
            Assert.IsNotNull(settings,"DefaultStrategy.asset must load.");
            foreach(var kind in StrategyMatch.Hostiles)
            {
                var hostile=settings.HostileOf(kind);
                Assert.IsNotNull(hostile,kind+" has no hostile profile.");
                Assert.Greater(hostile.health,0,kind+" health multiplier");
                Assert.GreaterOrEqual(hostile.damage,0,kind+" damage multiplier");
                Assert.Greater(hostile.speed,0,kind+" speed multiplier");
                Assert.Greater(hostile.range,0,kind+" range");
                Assert.Greater(hostile.radius,0,kind+" radius");
                Assert.Greater(hostile.aggro,0,kind+" aggro radius");
                Assert.IsNotNull(settings.ArmorOf(kind),kind+" has no armor profile.");
                Assert.IsTrue(StrategyEntity.IsUnitKind(kind),kind+" must count as a unit or the generator builds it as a structure.");
                Assert.IsTrue(StrategyMatch.IsHostileKind(kind),kind+" must be recognised as hostile.");
            }
            // A wave that names a friendly kind would spawn an enemy the player cannot fight.
            for(int wave=1;wave<=settings.waveCount;wave++)
                foreach(var (kind,count) in settings.Composition(wave).Groups())
                {
                    Assert.IsTrue(StrategyMatch.IsHostileKind(kind),"Wave "+wave+" contains non-hostile "+kind+".");
                    Assert.Greater(count,0,"Wave "+wave+" has an empty "+kind+" group.");
                }
        }
        // The hostile table is an override, not a replacement: settings authored before it must still describe their variants.
        [Test] public void EmptyHostileTableFallsBackToLegacyVariants()
        {
            var settings=ScriptableObject.CreateInstance<StrategySettings>();
            try
            {
                Assert.IsNull(settings.HostileOf(EntityKind.Brute));
                Assert.AreEqual(settings.enemyHealth*settings.bruteHealth,settings.Health(EntityKind.Brute));
                Assert.AreEqual(settings.enemySpeed*settings.runnerSpeed,settings.EnemySpeed(EntityKind.Runner));
                Assert.AreEqual(settings.enemyDamage*settings.runnerDamage,settings.EnemyDamage(EntityKind.Runner));
                Assert.AreEqual(HostilePriority.SoftTargets,settings.Priority(EntityKind.Runner));
                Assert.AreEqual(HostilePriority.Structures,settings.Priority(EntityKind.Brute));
                Assert.AreEqual(HostilePriority.Headquarters,settings.Priority(EntityKind.Enemy));
                Assert.AreEqual(.5f,settings.Radius(EntityKind.Enemy));
                Assert.AreEqual(8,settings.Aggro(EntityKind.Enemy));
                Assert.IsFalse(settings.MendsUnits(EntityKind.Enemy));
            }
            finally { Object.DestroyImmediate(settings); }
        }
        [Test] public void WaveGroupsSupersedeLegacyFieldsAndDropEmptyCounts()
        {
            var wave=new WaveComposition(new WaveGroup(EntityKind.Lancer,2),new WaveGroup(EntityKind.Juggernaut,1)).Enemies().ToArray();
            Assert.AreEqual(3,wave.Length); Assert.AreEqual(EntityKind.Lancer,wave.First()); Assert.AreEqual(EntityKind.Juggernaut,wave.Last());
            var mixed=new WaveComposition(5,5,5){ groups=new[]{ new WaveGroup(EntityKind.Warden,1) } };
            Assert.AreEqual(EntityKind.Warden,mixed.Enemies().Single(),"Authored groups must win over the legacy fields.");
            Assert.IsEmpty(new WaveComposition(new WaveGroup(EntityKind.Lancer,0),new WaveGroup(EntityKind.Breaker,-3)).Enemies());
        }
        // The hole this roster closes: before the breaker, every hostile was weak against Heavy and a defender wall always worked.
        [Test] public void BreakerPunishesTheDefenderScreenAndLancerOutrangesIt()
        {
            var settings=Shipped();
            Assert.Greater(settings.DamageScale(EntityKind.Breaker,EntityKind.Defender),1,"A breaker must beat Heavy armor.");
            Assert.Greater(settings.DamageScale(EntityKind.Breaker,EntityKind.Defender),settings.DamageScale(EntityKind.Brute,EntityKind.Defender));
            Assert.Less(settings.DamageScale(EntityKind.Breaker,EntityKind.Worker),1,"A breaker must stay poor against Light, or it answers everything.");
            Assert.Greater(settings.DamageScale(EntityKind.Soldier,EntityKind.Breaker),settings.DamageScale(EntityKind.Ranger,EntityKind.Breaker),"Soldiers are the intended answer to a breaker.");
            // A lancer shoots past a defender screen, but a turret still reaches it first.
            Assert.Greater(settings.Range(EntityKind.Lancer),settings.Range(EntityKind.Defender));
            Assert.Less(settings.Range(EntityKind.Lancer),settings.turretRange);
            Assert.Greater(settings.Aggro(EntityKind.Lancer),settings.Range(EntityKind.Lancer),"A ranged hostile must see at least as far as it shoots.");
            // Exactly one hostile mends, and it is unarmed.
            Assert.AreEqual(0,settings.Damage(EntityKind.Warden));
            foreach(var kind in StrategyMatch.Hostiles)
                Assert.AreEqual(kind==EntityKind.Warden,settings.MendsUnits(kind),kind+" mending is wrong.");
        }
        [Test] public void SupplyAndCostsAreLookedUpByKind()
        {
            var settings=Shipped();
            Assert.AreEqual(settings.Profile(EntityKind.Worker).supply,settings.Supply(EntityKind.Worker));
            Assert.AreEqual(settings.Profile(EntityKind.Defender).supply,settings.Supply(EntityKind.Defender));
            Assert.Greater(settings.Supply(EntityKind.Defender),settings.Supply(EntityKind.Soldier),"A defender must cost more supply than a soldier.");
            Assert.AreEqual(0,settings.Supply(EntityKind.Turret));
            Assert.AreEqual(settings.headquartersSupply,settings.SupplyProvided(EntityKind.Headquarters));
            Assert.AreEqual(settings.relaySupply,settings.SupplyProvided(EntityKind.SupplyRelay));
            Assert.AreEqual(settings.producerSupply,settings.SupplyProvided(EntityKind.SupportBay));
            Assert.AreEqual(0,settings.SupplyProvided(EntityKind.Worker));
            Assert.AreEqual(settings.relayCost,settings.Cost(EntityKind.SupplyRelay));
            // Structures must not inherit the unit-sized default footprint.
            Assert.AreEqual(settings.buildingHealth,settings.Health(EntityKind.RangerPost));
            Assert.Greater(settings.Radius(EntityKind.SupportBay),settings.Radius(EntityKind.Worker));
        }
        [Test] public void HealingClampsToMaximumAndNeverResurrects()
        {
            var health=new HealthModel(100);
            health.Damage(30);
            Assert.AreEqual(0,health.Heal(-5),"Negative healing is a no-op.");
            Assert.AreEqual(20,health.Heal(20)); Assert.AreEqual(90,health.Current);
            Assert.AreEqual(10,health.Heal(999),"Healing clamps to maximum."); Assert.AreEqual(100,health.Current);
            health.Damage(1000);
            Assert.IsFalse(health.IsAlive);
            Assert.AreEqual(0,health.Heal(50),"The dead are not revived."); Assert.IsFalse(health.IsAlive);
        }
        [Test] public void SpendingIsAtomicAndRejectsNegativeAmounts()
        { var wallet = new Wallet(100); Assert.IsFalse(wallet.TrySpend(101)); Assert.IsFalse(wallet.TrySpend(-1)); Assert.AreEqual(100, wallet.Minerals); Assert.IsTrue(wallet.TrySpend(100)); Assert.AreEqual(0, wallet.Minerals); }
        [Test] public void MiningPreservesTotalAndCapsFinalLoad()
        { var stock = new MineralStock(25); var wallet = new Wallet(0); wallet.Deposit(stock.Extract(20)); wallet.Deposit(stock.Extract(20)); Assert.AreEqual(25, wallet.Minerals); Assert.AreEqual(0, stock.Extract(20)); }
        [Test] public void ProductionWaitsForTimeAndSuccessfulSpawn()
        { var wallet = new Wallet(100); var q = new ProductionQueue(); Assert.IsTrue(q.Enqueue(EntityKind.Soldier, wallet, 50, 2)); Assert.IsTrue(q.Enqueue(EntityKind.Defender, wallet, 50, 3)); q.Tick(1); Assert.IsFalse(q.Complete()); q.Tick(10); Assert.IsTrue(q.Ready); Assert.AreEqual(2, q.Count); Assert.IsTrue(q.Complete()); Assert.AreEqual(3, q.Remaining); }
        [Test] public void QueueKeepsEachJobsKindInOrder()
        {
            var wallet = new Wallet(500); var q = new ProductionQueue();
            Assert.IsTrue(q.Enqueue(EntityKind.Soldier, wallet, 10, 1));
            Assert.IsTrue(q.Enqueue(EntityKind.Defender, wallet, 10, 2));
            Assert.IsTrue(q.Enqueue(EntityKind.Defender, wallet, 10, 2));
            CollectionAssert.AreEqual(new[] { EntityKind.Soldier, EntityKind.Defender, EntityKind.Defender }, q.Queued);
            Assert.AreEqual(EntityKind.Soldier, q.Next);
            q.Tick(5); Assert.IsTrue(q.Complete());
            Assert.AreEqual(EntityKind.Defender, q.Next, "The next job's kind must survive a completion.");
            Assert.AreEqual(2, q.Remaining);
            q.Tick(5); q.Complete(); q.Tick(5); q.Complete();
            Assert.IsNull(q.Next); Assert.IsEmpty(q.Queued);
        }
        [Test] public void FullQueueDoesNotCharge()
        { var w = new Wallet(100); var q = new ProductionQueue(); for(int i=0;i<5;i++) Assert.IsTrue(q.Enqueue(EntityKind.Soldier,w,10,1)); Assert.IsFalse(q.Enqueue(EntityKind.Soldier,w,10,1)); Assert.AreEqual(50,w.Minerals); }
        [Test] public void HealthClampsDeathAndRejectsHealingThroughDamage()
        { var h = new HealthModel(100); Assert.AreEqual(0,h.Damage(-10)); Assert.AreEqual(100,h.Damage(200)); Assert.IsFalse(h.IsAlive); Assert.AreEqual(0,h.Damage(20)); }
        [Test] public void FiveClearedWavesWinOnlyAfterLastEnemy()
        { var w = new WaveState(5, 2, 1); Assert.IsFalse(w.Tick(1,0,true)); for(int i=1;i<=5;i++) { Assert.IsTrue(w.Tick(1,0,true)); Assert.AreEqual(i,w.Wave); w.Tick(100,1,true); Assert.AreEqual(MatchResult.Playing,w.Result); w.Tick(0,0,true); } Assert.AreEqual(MatchResult.Victory,w.Result); }
        [Test] public void HeadquartersDeathOverridesFinalWaveClear()
        { var w = new WaveState(1,0,0); w.Tick(0,0,true); w.Tick(0,0,false); Assert.AreEqual(MatchResult.Defeat,w.Result); Assert.IsFalse(w.Tick(100,0,true)); }
    }
}
