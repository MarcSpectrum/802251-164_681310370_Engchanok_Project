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
        [Test] public void SpendingIsAtomicAndRejectsNegativeAmounts()
        { var wallet = new Wallet(100); Assert.IsFalse(wallet.TrySpend(101)); Assert.IsFalse(wallet.TrySpend(-1)); Assert.AreEqual(100, wallet.Minerals); Assert.IsTrue(wallet.TrySpend(100)); Assert.AreEqual(0, wallet.Minerals); }
        [Test] public void MiningPreservesTotalAndCapsFinalLoad()
        { var stock = new MineralStock(25); var wallet = new Wallet(0); wallet.Deposit(stock.Extract(20)); wallet.Deposit(stock.Extract(20)); Assert.AreEqual(25, wallet.Minerals); Assert.AreEqual(0, stock.Extract(20)); }
        [Test] public void ProductionWaitsForTimeAndSuccessfulSpawn()
        { var wallet = new Wallet(100); var q = new ProductionQueue(); Assert.IsTrue(q.Enqueue(wallet, 50, 2)); Assert.IsTrue(q.Enqueue(wallet, 50, 3)); q.Tick(1); Assert.IsFalse(q.Complete()); q.Tick(10); Assert.IsTrue(q.Ready); Assert.AreEqual(2, q.Count); Assert.IsTrue(q.Complete()); Assert.AreEqual(3, q.Remaining); }
        [Test] public void FullQueueDoesNotCharge()
        { var w = new Wallet(100); var q = new ProductionQueue(); for(int i=0;i<5;i++) Assert.IsTrue(q.Enqueue(w,10,1)); Assert.IsFalse(q.Enqueue(w,10,1)); Assert.AreEqual(50,w.Minerals); }
        [Test] public void HealthClampsDeathAndRejectsHealingThroughDamage()
        { var h = new HealthModel(100); Assert.AreEqual(0,h.Damage(-10)); Assert.AreEqual(100,h.Damage(200)); Assert.IsFalse(h.IsAlive); Assert.AreEqual(0,h.Damage(20)); }
        [Test] public void FiveClearedWavesWinOnlyAfterLastEnemy()
        { var w = new WaveState(5, 2, 1); Assert.IsFalse(w.Tick(1,0,true)); for(int i=1;i<=5;i++) { Assert.IsTrue(w.Tick(1,0,true)); Assert.AreEqual(i,w.Wave); w.Tick(100,1,true); Assert.AreEqual(MatchResult.Playing,w.Result); w.Tick(0,0,true); } Assert.AreEqual(MatchResult.Victory,w.Result); }
        [Test] public void HeadquartersDeathOverridesFinalWaveClear()
        { var w = new WaveState(1,0,0); w.Tick(0,0,true); w.Tick(0,0,false); Assert.AreEqual(MatchResult.Defeat,w.Result); Assert.IsFalse(w.Tick(100,0,true)); }
    }
}
