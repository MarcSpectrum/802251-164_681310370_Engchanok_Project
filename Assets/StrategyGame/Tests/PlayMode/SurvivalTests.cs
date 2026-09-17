using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Engchanok.StrategyGame.Tests
{
    public sealed class SurvivalTests
    {
        StrategyMatch match;
        StrategySettings original;
        [UnitySetUp] public IEnumerator Setup()
        {
            SceneManager.LoadScene("Survival");
            yield return null; yield return null;
            match = Object.FindFirstObjectByType<StrategyMatch>();
            original = match.settings;
            match.settings = Object.Instantiate(original);
            Assert.AreEqual(3, match.Entities.Count(e => e.kind == EntityKind.Worker));
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (match != null) { var copy = match.settings; match.settings = original; Object.Destroy(copy); }
            Time.timeScale = 1; SceneManager.LoadScene("MainMenu"); yield return null;
        }
        [UnityTest] public IEnumerator PresentationEffectsPauseExpireAndUnloadWithScene()
        {
            var pool=StrategyEffects.For(match);
            pool.Emit(Vector3.up,Vector3.one,Color.cyan,.3f,.1f);
            Assert.AreEqual(1,pool.ActiveCount);
            match.SetPaused(true);
            yield return new WaitForSecondsRealtime(.4f);
            Assert.AreEqual(1,pool.ActiveCount,"Pause must freeze cosmetic lifetimes.");
            pool.Emit(Vector3.zero,Vector3.one,Color.white,1,.1f);
            Assert.AreEqual(1,pool.ActiveCount,"Paused matches reject feedback requests.");
            match.SetPaused(false);
            yield return new WaitForSeconds(.4f);
            Assert.AreEqual(0,pool.ActiveCount);
            for(int i=0;i<200;i++) pool.Emit(Vector3.zero,Vector3.one,Color.white,1,.1f);
            Assert.AreEqual(128,pool.ActiveCount,"Feedback allocation must be bounded.");
            var copy=match.settings; match.settings=original; Object.Destroy(copy);
            match.Restart(); yield return null; yield return null;
            Assert.IsTrue(pool==null,"Scene restart must destroy the pool.");
            match=Object.FindFirstObjectByType<StrategyMatch>(); original=match.settings; match.settings=Object.Instantiate(original);
            Assert.AreEqual(0,StrategyEffects.For(match).ActiveCount);
        }
        [UnityTest] public IEnumerator ResearchAppliesToExistingAndFutureUnitsAndPauses()
        {
            match.Wallet.Deposit(1000); var worker=match.Entities.First(e=>e.kind==EntityKind.Worker);
            Assert.IsTrue(match.StartResearch(UpgradeKind.Mining));
            float remaining=match.Research.Remaining; match.SetPaused(true);
            Assert.IsFalse(match.StartResearch(UpgradeKind.SoldierWeapons));
            yield return new WaitForSecondsRealtime(.15f); Assert.AreEqual(remaining,match.Research.Remaining);
            match.SetPaused(false); Assert.IsTrue(match.Train(match.Headquarters));
            match.Research.Tick(100); Assert.AreEqual(30,match.WorkerCapacity);
            Assert.IsTrue(match.StartResearch(UpgradeKind.SoldierWeapons)); match.Research.Tick(100);
            var soldier=match.Spawn(EntityKind.Soldier,new Vector3(-5,0,-15));
            Assert.AreEqual(match.settings.Profile(EntityKind.Soldier).damage*1.25f,match.CombatDamage(soldier.kind));
            Assert.AreEqual(match.settings.turretDamage,match.CombatDamage(EntityKind.Turret));
            match.Headquarters.Damage(100000); yield return null;
            Assert.IsFalse(match.StartResearch(UpgradeKind.TurretWeapons));
        }
        [UnityTest] public IEnumerator PracticeWaitsForActionsAndNeverStartsWaves()
        {
            var copy=match.settings; match.settings=original; Object.Destroy(copy);
            StrategySession.PracticeRequested=true; SceneManager.LoadScene("Survival"); yield return null; yield return null;
            match=Object.FindFirstObjectByType<StrategyMatch>(); original=match.settings; match.settings=Object.Instantiate(original);
            Assert.IsTrue(match.Practice); Assert.AreEqual(0,match.TutorialStep);
            var commander=match.GetComponent<StrategyCommander>(); commander.Select(match.Entities.First(e=>e.kind==EntityKind.Worker));
            yield return null; Assert.AreEqual(1,match.TutorialStep);
            match.Deliver(20); yield return null; Assert.AreEqual(2,match.TutorialStep);
            Assert.IsTrue(match.Build(EntityKind.Barracks,new Vector3(-9,0,-11))); yield return null;
            Assert.AreEqual(3,match.TutorialStep); Assert.AreEqual(0,match.Waves.Wave);
            commander.BeginOrder(UnitOrder.Gather); Assert.AreEqual(UnitOrder.Gather,commander.TargetingOrder);
            commander.BeginPlacement(EntityKind.Turret); Assert.IsFalse(commander.Targeting); commander.CancelPlacement();
            var barracks=match.Entities.First(e=>e.kind==EntityKind.Barracks); match.settings.Profile(EntityKind.Soldier).trainSeconds=.1f;
            Assert.IsTrue(match.Train(barracks)); yield return new WaitForSeconds(.5f);
            Assert.AreEqual(4,match.TutorialStep);
            commander.ClearSelection(); commander.Select(match.Entities.First(e=>e.kind==EntityKind.Soldier));
            Assert.IsTrue(commander.IssueAttackMove(new Vector3(0,0,-2))); yield return null;
            Assert.AreEqual(5,match.TutorialStep);
            Assert.IsTrue(match.StartResearch(UpgradeKind.Mining)); match.Research.Tick(100); yield return null;
            Assert.AreEqual(6,match.TutorialStep); Assert.AreEqual(0,match.Waves.Wave);
            int previous=PlayerPrefs.GetInt(StrategySession.TutorialKey,0); bool existed=PlayerPrefs.HasKey(StrategySession.TutorialKey);
            try
            {
                copy=match.settings; match.settings=original; Object.Destroy(copy);
                match.FinishPractice(); yield return null; yield return null;
                match=Object.FindFirstObjectByType<StrategyMatch>(); original=match.settings; match.settings=Object.Instantiate(original);
                Assert.IsFalse(match.Practice); Assert.AreEqual(original.startingMinerals,match.Wallet.Minerals);
                Assert.IsFalse(match.Research.Completed(UpgradeKind.Mining)); Assert.AreEqual(1,PlayerPrefs.GetInt(StrategySession.TutorialKey));
            }
            finally { if(existed) PlayerPrefs.SetInt(StrategySession.TutorialKey,previous); else PlayerPrefs.DeleteKey(StrategySession.TutorialKey); PlayerPrefs.Save(); }

        }
        [UnityTest] public IEnumerator WorkerMinesAndDeposits()
        {
            var worker = match.Entities.First(e => e.kind == EntityKind.Worker);
            var deposit = Object.FindObjectsByType<MineralDeposit>(FindObjectsSortMode.None).First();
            int before = match.Wallet.Minerals;
            worker.Gather(deposit);
            Time.timeScale = 10;
            float deadline = Time.realtimeSinceStartup + 10;
            while (match.Wallet.Minerals == before && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.Greater(match.Wallet.Minerals, before, "Worker at " + worker.transform.position + ", cargo " + worker.Cargo + ", target " + (worker.MiningTarget != null) + ", HQ distance " + Vector3.Distance(worker.transform.position,match.Headquarters.transform.position));
            Assert.Less(deposit.Stock.Remaining, match.settings.depositMinerals);
        }
        [UnityTest] public IEnumerator PlacementProductionAndPause()
        {
            match.Wallet.Deposit(1000);
            int before = match.Wallet.Minerals;
            Assert.IsFalse(match.Build(EntityKind.Turret, StrategyMatch.HomePosition));
            Assert.AreEqual(before, match.Wallet.Minerals);
            Assert.IsFalse(match.CanPlace(EntityKind.Turret, new Vector3(0,0,0), out _));
            Assert.IsFalse(match.CanPlace(EntityKind.Turret, new Vector3(39,0,39), out _));
            Assert.IsTrue(match.Build(EntityKind.Barracks, new Vector3(-9,0,-11)));
            var barracks = match.Entities.First(e => e.kind == EntityKind.Barracks);
            match.settings.Profile(EntityKind.Soldier).trainSeconds = .1f;
            Assert.IsTrue(match.Train(barracks));
            match.SetPaused(true);
            yield return new WaitForSecondsRealtime(.15f);
            Assert.AreEqual(1, barracks.Production.Count);
            Assert.IsFalse(match.Build(EntityKind.Turret,new Vector3(9,0,-11)));
            match.SetPaused(false);
            yield return new WaitForSeconds(.5f);
            Assert.AreEqual(1, match.Entities.Count(e => e.kind == EntityKind.Soldier));
            Assert.AreEqual(0, barracks.Production.Count);
        }
        [UnityTest] public IEnumerator SupplyBlocksTrainingUntilRelayIsBuilt()
        {
            match.Wallet.Deposit(5000);
            // Three starting workers at 3 supply each leave no room under the headquarters' own cap.
            match.settings.Profile(EntityKind.Worker).supply = 3;
            match.RecountSupply();
            Assert.AreEqual(match.settings.headquartersSupply, match.Supply.Cap);
            Assert.AreEqual(9, match.Supply.Used);
            Assert.IsTrue(match.Supply.Fits(1)); Assert.IsFalse(match.Supply.Fits(3));
            Assert.IsFalse(match.Train(match.Headquarters), "A full supply cap must refuse training.");
            Assert.AreEqual(0, match.Headquarters.Production.Count);
            Assert.IsTrue(match.Build(EntityKind.SupplyRelay, new Vector3(0,0,-22)));
            match.RecountSupply();
            Assert.AreEqual(match.settings.headquartersSupply + match.settings.relaySupply, match.Supply.Cap);
            for (int i = 0; i < 3; i++) Assert.IsTrue(match.Train(match.Headquarters), "A relay must reopen training.");
            Assert.IsTrue(match.Supply.Full);
            Assert.AreEqual(3, match.Headquarters.Production.Count);
            Assert.IsFalse(match.Train(match.Headquarters), "Queued jobs reserve supply, so a queue cannot outrun the cap.");
            // A destroyed relay takes its contribution with it.
            match.Entities.First(e => e.kind == EntityKind.SupplyRelay).Damage(100000);
            yield return null;
            Assert.AreEqual(match.settings.headquartersSupply, match.Supply.Cap);
        }
        [UnityTest] public IEnumerator HostilesPreferTheirPriorityTargetsAndArmorCounters()
        {
            match.Wallet.Deposit(2000);
            Assert.IsTrue(match.Build(EntityKind.Turret, new Vector3(0,0,-22)));
            var turret = match.Entities.First(e => e.kind == EntityKind.Turret);
            var runner = match.Spawn(EntityKind.Runner, new Vector3(-25,0,25));
            var brute = match.Spawn(EntityKind.Brute, new Vector3(25,0,25));
            var standard = match.Spawn(EntityKind.Enemy, new Vector3(0,0,28));
            Assert.AreEqual(EntityKind.Worker, match.PriorityTarget(runner).kind, "Runners must harass the mining lines.");
            Assert.IsTrue(StrategyMatch.IsDefence(match.PriorityTarget(brute).kind), "Brutes must siege structures.");
            Assert.AreSame(match.Headquarters, match.PriorityTarget(standard), "Standard hostiles must push the headquarters.");
            Assert.IsNull(match.PriorityTarget(match.Headquarters), "Friendly entities have no priority target.");
            // Turrets and soldiers answer opposite halves of a mixed wave.
            Assert.Greater(match.CombatDamage(EntityKind.Turret,EntityKind.Brute), match.CombatDamage(EntityKind.Turret,EntityKind.Runner));
            Assert.Greater(match.CombatDamage(EntityKind.Soldier,EntityKind.Runner), match.CombatDamage(EntityKind.Soldier,EntityKind.Brute));
            Assert.AreEqual(match.CombatDamage(EntityKind.Soldier), match.CombatDamage(EntityKind.Soldier,EntityKind.Headquarters), "Structures take unscaled damage.");
            foreach (var worker in match.Entities.Where(e => e.kind == EntityKind.Worker).ToArray()) worker.Damage(100000);
            yield return null;
            Assert.IsTrue(StrategyMatch.IsDefence(match.PriorityTarget(runner).kind), "With no workers left a runner falls back to structures.");
            turret.Damage(100000);
            yield return null;
            Assert.AreSame(match.Headquarters, match.PriorityTarget(runner));
            Assert.AreSame(match.Headquarters, match.PriorityTarget(brute));
        }
        [UnityTest] public IEnumerator NewHostilesHuntTheirOwnObjectives()
        {
            match.Wallet.Deposit(2000);
            Assert.IsTrue(match.Build(EntityKind.Turret, new Vector3(0,0,-22)));
            Assert.IsTrue(match.TrySpawnUnit(EntityKind.Defender, new Vector3(-6,0,-14), out var defender));
            var lancer = match.Spawn(EntityKind.Lancer, new Vector3(-25,0,25));
            var breaker = match.Spawn(EntityKind.Breaker, new Vector3(25,0,25));
            var warden = match.Spawn(EntityKind.Warden, new Vector3(0,0,28));
            var juggernaut = match.Spawn(EntityKind.Juggernaut, new Vector3(10,0,28));
            yield return null;
            Assert.AreSame(defender, match.PriorityTarget(breaker), "A breaker must hunt the Heavy screen it exists to break.");
            Assert.IsTrue(StrategyMatch.IsDefence(match.PriorityTarget(lancer).kind), "Lancers siege structures.");
            Assert.IsTrue(StrategyMatch.IsDefence(match.PriorityTarget(juggernaut).kind), "Juggernauts siege structures.");
            Assert.AreSame(match.Headquarters, match.PriorityTarget(warden), "A warden walks with the wave toward headquarters.");
            // With no Heavy unit left the breaker falls through structures rather than standing still.
            defender.Damage(100000);
            yield return null;
            Assert.IsTrue(StrategyMatch.IsDefence(match.PriorityTarget(breaker).kind));
            Assert.Greater(match.CombatDamage(EntityKind.Breaker,EntityKind.Defender), match.CombatDamage(EntityKind.Brute,EntityKind.Defender));
            Assert.Greater(match.settings.Radius(EntityKind.Juggernaut), match.settings.Radius(EntityKind.Enemy), "The boss must occupy more ground than a standard hostile.");
        }
        // The lancer is the first hostile that shoots; before it, every hostile swept a fixed eight units regardless of weapon range.
        [UnityTest] public IEnumerator LancerStrikesFromRangeWithoutClosingToMelee()
        {
            match.Wallet.Deposit(2000);
            Assert.IsTrue(match.Build(EntityKind.Turret, new Vector3(0,0,-20)));
            var turret = match.Entities.First(e => e.kind == EntityKind.Turret);
            match.settings.turretDamage = 0;
            float lancerRange = match.settings.Range(EntityKind.Lancer);
            Assert.Greater(match.settings.Aggro(EntityKind.Lancer), lancerRange);
            var lancer = match.Spawn(EntityKind.Lancer, turret.transform.position - new Vector3(0,0,lancerRange - .5f));
            float before = turret.Health.Current;
            Time.timeScale = 10;
            float deadline = Time.realtimeSinceStartup + 10;
            while (turret.Health.Current >= before && Time.realtimeSinceStartup < deadline) yield return null;
            Time.timeScale = 1;
            Assert.Less(turret.Health.Current, before, "A lancer must damage a turret from its own range.");
            float distance = Vector3.Distance(lancer.transform.position, turret.transform.position);
            Assert.Greater(distance, match.settings.enemyRange + match.settings.Radius(EntityKind.Turret), "A lancer must not walk into melee to fire.");
        }
        [UnityTest] public IEnumerator WardenMendsHostilesOnlyAndNeverSpendsMinerals()
        {
            var warden = match.Spawn(EntityKind.Warden, new Vector3(0,0,26));
            var hurtHostile = match.Spawn(EntityKind.Enemy, new Vector3(1,0,26));
            var worker = match.Entities.First(e => e.kind == EntityKind.Worker);
            hurtHostile.Health.Damage(40);
            worker.Health.Damage(30);
            float hostileBefore = hurtHostile.Health.Current, workerBefore = worker.Health.Current;
            int mineralsBefore = match.Wallet.Minerals;
            Assert.IsTrue(match.settings.MendsUnits(EntityKind.Warden));
            Assert.AreEqual(0, match.CombatDamage(EntityKind.Warden), "A warden is unarmed.");
            Assert.AreSame(hurtHostile, match.NearestWounded(warden, 30, false), "A warden mends its own side.");
            Time.timeScale = 10;
            float deadline = Time.realtimeSinceStartup + 10;
            while (hurtHostile.Health.Current <= hostileBefore && Time.realtimeSinceStartup < deadline) yield return null;
            Time.timeScale = 1;
            Assert.Greater(hurtHostile.Health.Current, hostileBefore, "A warden must heal wounded hostiles.");
            Assert.AreEqual(workerBefore, worker.Health.Current, "A warden must never heal the player.");
            Assert.AreEqual(mineralsBefore, match.Wallet.Minerals, "Warden healing must not bill the player's wallet.");
        }
        // The preview grew from three fixed groups to as many as seven. The replay's overflow detector never sees this string,
        // because no capture lands in the wave-4 break, so the same check it uses is applied directly here.
        [UnityTest] public IEnumerator FinalWavePreviewFitsItsLabel()
        {
            match.settings.waveCompositions = new[] { new WaveComposition(
                new WaveGroup(EntityKind.Enemy,10), new WaveGroup(EntityKind.Runner,10), new WaveGroup(EntityKind.Brute,4),
                new WaveGroup(EntityKind.Lancer,5), new WaveGroup(EntityKind.Breaker,4), new WaveGroup(EntityKind.Warden,2),
                new WaveGroup(EntityKind.Juggernaut,1)) };
            yield return null; yield return null;
            var objective = Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None).FirstOrDefault(t => t.text.StartsWith("Next wave:"));
            Assert.IsNotNull(objective, "The next-wave preview must be on screen during preparation.");
            StringAssert.Contains("1 juggernaut (Heavy)", objective.text);
            StringAssert.Contains("10 standard (Medium)", objective.text);
            Assert.LessOrEqual(objective.preferredHeight, objective.rectTransform.rect.height + 2, "The final-wave preview overflows its label: " + objective.text);
        }
        // A warden healing a juggernaut is the one way this roster could stall a match, since HostileCount is what lets a wave end.
        // Driven by spawning the group directly: WaveState is built in Start, so retuning waveCount afterwards would not take.
        [UnityTest] public IEnumerator WardenHealingCannotOutpaceADefendedOutpost()
        {
            match.settings.turretDamage = 100; match.settings.turretRange = 100; match.settings.attackInterval = .05f;
            // Spawned rather than built: placement clearance around the nearby deposit rejects this point, and the
            // five-wave test stands its turret up the same way for the same reason.
            Assert.IsNotNull(match.Spawn(EntityKind.Turret, new Vector3(-7,0,-12)));
            var boss = match.Spawn(EntityKind.Juggernaut, new Vector3(0,0,30));
            var escortA = match.Spawn(EntityKind.Warden, new Vector3(2,0,31));
            var escortB = match.Spawn(EntityKind.Warden, new Vector3(-2,0,31));
            Assert.AreEqual(3, match.HostileCount);
            Assert.Greater(boss.Health.Maximum, match.settings.Health(EntityKind.Brute), "The boss must outweigh a brute.");
            // Modest acceleration on purpose: the scheduled waves must not start and add hostiles while this group is being killed.
            Time.timeScale = 5;
            float deadline = Time.realtimeSinceStartup + 20;
            bool Dead(StrategyEntity e) => e == null || !e.Alive;
            while (!(Dead(boss) && Dead(escortA) && Dead(escortB)) && Time.realtimeSinceStartup < deadline) yield return null;
            Time.timeScale = 1;
            Assert.IsTrue(Dead(boss) && Dead(escortA) && Dead(escortB), "A boss escorted by wardens must still be killable, or its wave never ends.");
        }
        [UnityTest] public IEnumerator MixedQueueSpawnsTheRightKindsAndReservesTheirSupply()
        {
            match.Wallet.Deposit(5000);
            Assert.IsTrue(match.Build(EntityKind.Barracks, new Vector3(-9,0,-11)));
            var barracks = match.Entities.First(e => e.kind == EntityKind.Barracks);
            var soldier = match.settings.Profile(EntityKind.Soldier);
            var defender = match.settings.Profile(EntityKind.Defender);
            soldier.trainSeconds = .1f; defender.trainSeconds = .1f;
            match.RecountSupply();
            int before = match.Supply.Used;
            Assert.IsTrue(match.Train(barracks, EntityKind.Soldier));
            Assert.IsTrue(match.Train(barracks, EntityKind.Defender));
            Assert.IsTrue(match.Train(barracks, EntityKind.Defender));
            CollectionAssert.AreEqual(new[] { EntityKind.Soldier, EntityKind.Defender, EntityKind.Defender }, barracks.Production.Queued);
            match.RecountSupply();
            // A mixed queue must reserve what it will actually cost, not three of the cheapest.
            Assert.AreEqual(before + soldier.supply + defender.supply * 2, match.Supply.Used);
            Assert.AreNotEqual(before + soldier.supply * 3, match.Supply.Used);
            // A ranger post cannot build barracks units, and a barracks cannot build rangers.
            Assert.IsFalse(match.Train(barracks, EntityKind.Ranger), "A producer must refuse a unit outside its roster.");
            Assert.IsFalse(match.Train(barracks, EntityKind.Worker));
            Time.timeScale = 10;
            float deadline = Time.realtimeSinceStartup + 15;
            while (barracks.Production.Count > 0 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual(0, barracks.Production.Count, "The queue must drain.");
            Assert.AreEqual(1, match.Entities.Count(e => e.kind == EntityKind.Soldier));
            Assert.AreEqual(2, match.Entities.Count(e => e.kind == EntityKind.Defender));
        }
        [UnityTest] public IEnumerator MedicHealsWoundedAlliesAndEngineerRepairsBuildings()
        {
            match.Wallet.Deposit(5000);
            match.settings.medicHealPerSecond = 400; match.settings.engineerRepairPerSecond = 400;
            var soldier = match.Spawn(EntityKind.Soldier, new Vector3(-4,0,-18));
            soldier.Damage(60);
            float wounded = soldier.Health.Current;
            Assert.Less(wounded, soldier.Health.Maximum);
            match.Spawn(EntityKind.Medic, new Vector3(-5,0,-18));
            Time.timeScale = 10;
            float deadline = Time.realtimeSinceStartup + 10;
            while (soldier.Health.Current <= wounded && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.Greater(soldier.Health.Current, wounded, "A medic must mend a wounded ally.");
            // Engineers mend structures instead, and pay minerals for it.
            Assert.IsTrue(match.Build(EntityKind.Turret, new Vector3(6,0,-18)));
            var turret = match.Entities.First(e => e.kind == EntityKind.Turret);
            turret.Damage(200);
            float damaged = turret.Health.Current;
            match.Spawn(EntityKind.Engineer, new Vector3(5,0,-18));
            int purse = match.Wallet.Minerals;
            deadline = Time.realtimeSinceStartup + 10;
            while (turret.Health.Current <= damaged && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.Greater(turret.Health.Current, damaged, "An engineer must repair a damaged structure.");
            Assert.Less(match.Wallet.Minerals, purse, "Repair must be paid for in minerals.");
        }
        [UnityTest] public IEnumerator FiveWavesCombatVictoryAndRestart()
        {
            // Accelerate only timing and strengthen defenders: exercise real navigation,
            // targeting, damage, deaths and all five waves without a long manual wait.
            match.settings.enemyHealth = 10;
            match.settings.turretDamage = 100;
            match.settings.turretRange = 100;
            match.settings.attackInterval = .05f;
            match.Spawn(EntityKind.Turret, new Vector3(-7,0,-12));
            Time.timeScale = 30;
            float deadline = Time.realtimeSinceStartup + 30;
            while (match.Waves.Result == MatchResult.Playing && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual(MatchResult.Victory, match.Waves.Result);
            Assert.AreEqual(5, match.Waves.Wave);
            yield return null;
            var overlay = match.GetComponent<StrategyHud>().canvas.transform.Find("Mission overlay");
            Assert.IsTrue(overlay.Find("Mission grade").gameObject.activeSelf, "Victory shows the mission report.");
            Assert.AreNotEqual("D", overlay.Find("Mission grade").GetComponent<UnityEngine.UI.Text>().text);
            Assert.AreEqual("5 / 5", overlay.Find("Report values").GetComponent<UnityEngine.UI.Text>().text.Split('\n')[1]);
            Assert.Greater(match.Stats.HostilesDefeated, 0); Assert.Greater(match.Stats.Elapsed, 0);
            var copy = match.settings; match.settings = original; Object.Destroy(copy);
            match.Restart(); yield return null; yield return null;
            match = Object.FindFirstObjectByType<StrategyMatch>();
            original = match.settings; match.settings = Object.Instantiate(original);
            Assert.AreEqual(MatchResult.Playing, match.Waves.Result);
            Assert.AreEqual(0, match.Waves.Wave);
            Assert.AreEqual(original.startingMinerals, match.Wallet.Minerals);
            Assert.AreEqual(4, match.Entities.Count);
            Assert.AreEqual(1, Time.timeScale);
            Assert.AreEqual(0, match.Stats.HostilesDefeated, "Restart starts a fresh report.");
            Assert.AreEqual(0, match.Wallet.Spent);
        }
        [UnityTest] public IEnumerator MissionReportCountsTrainingLossesAndKills()
        {
            Assert.IsFalse(match.Practice);
            match.Wallet.Deposit(1000);
            Assert.IsTrue(match.Build(EntityKind.Barracks, new Vector3(-9,0,-11)));
            yield return new WaitForSeconds(.3f);
            var producer = match.Entities.First(e => e.kind == EntityKind.Barracks);
            match.settings.Profile(EntityKind.Soldier).trainSeconds = .1f;
            Assert.IsTrue(match.Train(producer, EntityKind.Soldier));
            float deadline = Time.realtimeSinceStartup + 5;
            while (match.Stats.UnitsTrained < 1 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual(1, match.Stats.UnitsTrained);
            Assert.AreEqual(1, match.Stats.StructuresBuilt);
            Assert.AreEqual(match.settings.barracksCost + match.settings.Profile(EntityKind.Soldier).cost, match.Wallet.Spent);
            match.Spawn(EntityKind.Runner, new Vector3(20,0,20)).Damage(99999);
            match.Entities.First(e => e.kind == EntityKind.Soldier).Damage(99999);
            match.Spawn(EntityKind.Turret, new Vector3(20,0,-20)).Damage(99999);
            Assert.AreEqual(1, match.Stats.HostilesDefeated); Assert.AreEqual(1, match.Stats.UnitsLost); Assert.AreEqual(1, match.Stats.StructuresLost);
            Assert.AreEqual(1, match.Stats.UnitsTrained, "Scripted spawns are not training.");
            yield return new WaitForSeconds(.2f);
            float elapsed = match.Stats.Elapsed; Assert.Greater(elapsed, 0);
            var overlay = match.GetComponent<StrategyHud>().canvas.transform.Find("Mission overlay");
            match.SetPaused(true); yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(elapsed, match.Stats.Elapsed, "The mission clock stops while paused.");
            Assert.IsFalse(overlay.Find("Mission grade").gameObject.activeSelf, "Pausing does not show the report.");
            match.SetPaused(false);
            match.Headquarters.Damage(99999);
            yield return null; yield return null;
            Assert.AreEqual(MatchResult.Defeat, match.Waves.Result);
            Assert.IsTrue(overlay.Find("Mission grade").gameObject.activeSelf);
            Assert.AreEqual("D", overlay.Find("Mission grade").GetComponent<UnityEngine.UI.Text>().text);
            var values = overlay.Find("Report values").GetComponent<UnityEngine.UI.Text>().text.Split('\n');
            Assert.AreEqual("0 / " + match.Waves.Total, values[1]);
            Assert.AreEqual("0 / " + match.settings.headquartersHealth, values[2]);
            Assert.AreEqual((match.settings.barracksCost + match.settings.Profile(EntityKind.Soldier).cost).ToString(), values[4]);
            Assert.AreEqual("1 / 1", values[5], "Units trained / lost");
            Assert.AreEqual("1 / 2", values[6], "The fallen headquarters counts as a lost structure.");
            Assert.AreEqual("1", values[7]);
            var names = overlay.Find("Report names").GetComponent<UnityEngine.UI.Text>();
            Assert.AreEqual(values.Length, names.text.Split('\n').Length);
            Assert.LessOrEqual(names.preferredHeight, names.rectTransform.rect.height + 2, "The report overflows its label.");
        }
        [UnityTest] public IEnumerator AttackAlertsAreThrottledAndSpaceJumpsCamera()
        {
            Assert.IsFalse(match.Practice); Assert.IsFalse(match.HasAlert);
            var camera = match.GetComponent<StrategyCommander>().CameraController;
            var worker = match.Entities.First(e => e.kind == EntityKind.Worker);
            var hostile = match.Spawn(EntityKind.Enemy, new Vector3(25,0,25));
            hostile.Damage(1);
            Assert.IsFalse(match.HasAlert, "Hostiles taking damage never raise an alert.");
            hostile.Damage(99999);
            worker.Damage(1);
            Assert.IsTrue(match.HasAlert); StringAssert.Contains("under attack", match.Notice);
            float first = match.LastAlertTime;
            var tower = match.Spawn(EntityKind.Turret, new Vector3(-25,0,-25));
            tower.Damage(1);
            Assert.AreEqual(first, match.LastAlertTime, "Alerts keep a minimum gap, even for a distant fight.");
            Time.timeScale = 4;
            yield return new WaitForSeconds(StrategyMatch.AlertMinimumGap + .1f);
            worker.Damage(1);
            Assert.AreEqual(first, match.LastAlertTime, "Repeated hits in the same place are throttled.");
            tower.Damage(1);
            Assert.Less(Vector3.Distance(match.LastAlertPosition, tower.transform.position), .1f, "A fight elsewhere raises its own alert.");
            float second = match.LastAlertTime;
            yield return new WaitForSeconds(StrategyMatch.AlertMinimumGap + .1f);
            Time.timeScale = 1;
            worker.Damage(1);
            Assert.AreEqual(second, match.LastAlertTime, "Two fights far apart do not alternate alerts on every hit.");
            var restore = UseSyntheticInput(out _, out var keyboard);
            try
            {
                camera.ResetToHeadquarters();
                Press(keyboard, Key.Space, camera, "LateUpdate");
                Assert.Less(Vector2.Distance(XZ(camera.Focus), XZ(tower.transform.position)), .1f, "Space jumps to the latest alert.");
            }
            finally { restore(); }
        }
        [UnityTest] public IEnumerator IdleWorkerFinderCyclesAndSkipsMiners()
        {
            var commander = match.GetComponent<StrategyCommander>();
            var workers = match.Entities.Where(e => e.kind == EntityKind.Worker).ToArray();
            Assert.AreEqual(3, match.IdleWorkerCount);
            var visited = new System.Collections.Generic.HashSet<StrategyEntity>();
            for (int i = 0; i < 3; i++) { Assert.IsTrue(commander.SelectNextIdleWorker()); Assert.AreEqual(1, commander.Selection.Count); visited.Add(commander.Selection[0]); }
            Assert.AreEqual(3, visited.Count, "Three presses visit three different workers.");
            Assert.IsTrue(commander.SelectNextIdleWorker()); Assert.AreSame(workers[0], commander.Selection[0], "The cycle wraps.");
            Assert.Less(Vector2.Distance(XZ(commander.CameraController.Focus), XZ(workers[0].transform.position)), .1f);
            workers[1].Gather(Object.FindFirstObjectByType<MineralDeposit>());
            Assert.IsTrue(workers[2].Move(new Vector3(-8,0,-5)));
            Assert.AreEqual(1, match.IdleWorkerCount, "Mining and walking workers are busy.");
            for (int i = 0; i < 3; i++) { Assert.IsTrue(commander.SelectNextIdleWorker()); Assert.AreSame(workers[0], commander.Selection[0]); }
            yield return null;
            var label = match.GetComponent<StrategyHud>().canvas.transform.Find("Idle workers").GetComponentInChildren<UnityEngine.UI.Text>();
            StringAssert.Contains("IDLE WORKERS 1", label.text);
            match.SetPaused(true); commander.ClearSelection();
            Assert.IsFalse(commander.SelectNextIdleWorker()); Assert.IsEmpty(commander.Selection);
            match.SetPaused(false);
            var restore = UseSyntheticInput(out _, out var keyboard);
            try
            {
                Press(keyboard, Key.I, commander);
                Assert.AreEqual(1, commander.Selection.Count); Assert.IsTrue(commander.Selection[0].IsIdleWorker);
            }
            finally { restore(); }
        }
        [UnityTest] public IEnumerator MinimapShowsEntitiesAndDrivesCameraAndOrders()
        {
            var commander = match.GetComponent<StrategyCommander>();
            var camera = commander.CameraController;
            var minimap = match.GetComponent<StrategyHud>().Minimap;
            Assert.AreSame(minimap, commander.Minimap);
            int deposits = Object.FindObjectsByType<MineralDeposit>(FindObjectsSortMode.None).Length;
            yield return null;
            Assert.AreEqual(match.Entities.Count + deposits, minimap.BlipCount);
            Assert.IsTrue(match.TrySpawnUnit(EntityKind.Soldier, new Vector3(-6,0,-5), out var soldier));
            var hostile = match.Spawn(EntityKind.Enemy, new Vector3(20,0,20));
            yield return null;
            Assert.AreEqual(match.Entities.Count + deposits, minimap.BlipCount);
            hostile.Damage(99999); yield return null;
            Assert.AreEqual(match.Entities.Count + deposits, minimap.BlipCount, "Dead entities leave the minimap.");
            var restore = UseSyntheticInput(out var mouse, out _);
            try
            {
                // Pressing the minimap moves the camera without starting a selection box.
                var look = new Vector3(20,0,15);
                SendPointer(commander, mouse, new MouseState { position = minimap.ScreenPoint(look), buttons = 1 });
                Assert.IsFalse(commander.Dragging);
                Assert.Less(Vector2.Distance(XZ(camera.Focus), XZ(look)), .5f);
                SendPointer(commander, mouse, new MouseState { position = minimap.ScreenPoint(new Vector3(-20,0,-20)), buttons = 1 });
                Assert.Less(Vector2.Distance(XZ(camera.Focus), new Vector2(-20,-20)), .5f, "Dragging across the minimap tracks the pointer.");
                SendPointer(commander, mouse, new MouseState { position = new Vector2(Screen.width - 5, Screen.height / 2f), buttons = 1 });
                Assert.Greater(camera.Focus.x, 20, "Dragging past the edge holds the camera on the border.");
                SendPointer(commander, mouse, new MouseState { position = new Vector2(Screen.width - 5, Screen.height / 2f) });
                Assert.IsFalse(commander.Dragging); Assert.IsEmpty(commander.Selection);
                // Right-clicking the minimap orders the selection.
                commander.Select(soldier); commander.ClosePopup();
                var goal = new Vector3(-8,0,-5);
                SendPointer(commander, mouse, new MouseState { position = minimap.ScreenPoint(goal), buttons = 2 });
                SendPointer(commander, mouse, new MouseState { position = minimap.ScreenPoint(goal) });
                Assert.Less(Vector3.Distance(soldier.OrderDestination, goal), .5f);
                // Paused matches ignore the minimap entirely.
                var focus = camera.Focus; var before = soldier.OrderDestination;
                match.SetPaused(true);
                SendPointer(commander, mouse, new MouseState { position = minimap.ScreenPoint(new Vector3(5,0,-5)), buttons = 2 });
                SendPointer(commander, mouse, new MouseState { position = minimap.ScreenPoint(new Vector3(5,0,-5)), buttons = 1 });
                SendPointer(commander, mouse, new MouseState { position = minimap.ScreenPoint(new Vector3(5,0,-5)) });
                Assert.AreEqual(before, soldier.OrderDestination); Assert.AreEqual(focus, camera.Focus);
                match.SetPaused(false);
                // A lone producer takes a rally point from the minimap.
                match.Wallet.Deposit(1000); Assert.IsTrue(match.Build(EntityKind.Barracks, new Vector3(-9,0,-11)));
                yield return new WaitForSeconds(.3f);
                var producer = match.Entities.First(e => e.kind == EntityKind.Barracks);
                commander.ClearSelection(); commander.Select(producer); commander.ClosePopup();
                var rally = new Vector3(0,0,-5);
                SendPointer(commander, mouse, new MouseState { position = minimap.ScreenPoint(rally), buttons = 2 });
                SendPointer(commander, mouse, new MouseState { position = minimap.ScreenPoint(rally) });
                Assert.IsTrue(producer.RallyPoint.HasValue); Assert.Less(Vector3.Distance(producer.RallyPoint.Value, rally), 1);
            }
            finally { restore(); }
        }
        [UnityTest] public IEnumerator MinimapCompletesTargetingButIgnoresPlacement()
        {
            var commander = match.GetComponent<StrategyCommander>();
            var minimap = match.GetComponent<StrategyHud>().Minimap;
            Assert.IsTrue(match.TrySpawnUnit(EntityKind.Soldier, new Vector3(-6,0,-5), out var soldier));
            yield return null;
            var restore = UseSyntheticInput(out var mouse, out _);
            try
            {
                commander.Select(soldier); commander.BeginAttackMove(); Assert.IsTrue(commander.TargetingAttackMove);
                var goal = new Vector3(-4,0,-6);
                SendPointer(commander, mouse, new MouseState { position = minimap.ScreenPoint(goal), buttons = 1 });
                SendPointer(commander, mouse, new MouseState { position = minimap.ScreenPoint(goal) });
                Assert.IsFalse(commander.Targeting);
                Assert.AreEqual(UnitOrder.AttackMove, soldier.Order); Assert.Less(Vector3.Distance(soldier.OrderDestination, goal), .5f);
                var worker = match.Entities.First(e => e.kind == EntityKind.Worker);
                commander.ClearSelection(); commander.Select(worker); commander.BeginOrder(UnitOrder.Gather);
                SendPointer(commander, mouse, new MouseState { position = minimap.ScreenPoint(new Vector3(0,0,10)), buttons = 1 });
                SendPointer(commander, mouse, new MouseState { position = minimap.ScreenPoint(new Vector3(0,0,10)) });
                Assert.AreEqual(UnitOrder.Gather, commander.TargetingOrder, "Open ground is not a gather target.");
                var deposit = Object.FindObjectsByType<MineralDeposit>(FindObjectsSortMode.None).OrderBy(d => d.transform.position.x).First();
                var near = deposit.transform.position + new Vector3(1,0,1);
                SendPointer(commander, mouse, new MouseState { position = minimap.ScreenPoint(near), buttons = 1 });
                SendPointer(commander, mouse, new MouseState { position = minimap.ScreenPoint(near) });
                Assert.IsFalse(commander.Targeting); Assert.AreSame(deposit, worker.MiningTarget);
                commander.BeginPlacement(EntityKind.Turret);
                var focus = commander.CameraController.Focus;
                int turrets = match.Entities.Count(e => e.kind == EntityKind.Turret);
                SendPointer(commander, mouse, new MouseState { position = minimap.ScreenPoint(new Vector3(20,0,15)), buttons = 1 });
                SendPointer(commander, mouse, new MouseState { position = minimap.ScreenPoint(new Vector3(20,0,15)) });
                Assert.AreEqual(EntityKind.Turret, commander.Placement, "Placement needs precise ground, so the minimap ignores it.");
                Assert.AreEqual(focus, commander.CameraController.Focus);
                Assert.AreEqual(turrets, match.Entities.Count(e => e.kind == EntityKind.Turret));
                commander.CancelPlacement();
            }
            finally { restore(); }
        }
        static Vector2 XZ(Vector3 value) => new(value.x, value.z);
        // Synthetic devices for pointer and keyboard tests; the returned action restores the settings and removes the devices.
        internal static System.Action UseSyntheticInput(out Mouse mouse, out Keyboard keyboard)
        {
            var background = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            var editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            var addedMouse = InputSystem.AddDevice<Mouse>(); var addedKeyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.EnableDevice(addedMouse); InputSystem.EnableDevice(addedKeyboard);
            InputSystem.QueueStateEvent(addedKeyboard, new KeyboardState()); InputSystem.Update();
            mouse = addedMouse; keyboard = addedKeyboard;
            return () =>
            {
                InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
                InputSystem.settings.editorInputBehaviorInPlayMode = editorBehavior;
#endif
                InputSystem.RemoveDevice(addedMouse); InputSystem.RemoveDevice(addedKeyboard);
            };
        }
        // Presses and releases one key, running the named callback in between so wasPressedThisFrame is observed.
        internal static void Press(Keyboard keyboard, Key key, Component target, string callback = "Update")
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); InputSystem.Update();
            target.SendMessage(callback);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
        }
        [UnityTest] public IEnumerator HeadquartersDeathEndsMatchAndMenuLoads()
        {
            var worker=match.Entities.First(e=>e.kind==EntityKind.Worker);
            var deposit=Object.FindFirstObjectByType<MineralDeposit>();
            match.Headquarters.Damage(99999);
            yield return null; yield return null;
            Assert.AreEqual(MatchResult.Defeat, match.Waves.Result);
            Assert.AreEqual(0, Time.timeScale);
            Assert.IsFalse(match.Running);
            Assert.IsFalse(worker.Move(Vector3.zero)); worker.Gather(deposit); Assert.IsNull(worker.MiningTarget);
            Assert.IsFalse(Object.FindFirstObjectByType<StrategyCommander>().IssueAttackMove(Vector3.zero));
            match.MainMenu(); yield return null;
            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
            Assert.AreEqual(1,Time.timeScale);
        }
        [UnityTest] public IEnumerator MovementRoutesAroundBuildingAndRejectsOffMap()
        {
            match.Wallet.Deposit(1000);
            Assert.IsTrue(match.Build(EntityKind.Barracks, new Vector3(-9,0,-11)));
            yield return new WaitForSeconds(.3f);
            Assert.IsTrue(match.TrySpawnUnit(EntityKind.Soldier, new Vector3(-15,0,-11), out var soldier));
            Assert.IsFalse(soldier.Move(new Vector3(200,0,200)));
            Vector3 goal = new(-5,0,-11);
            Assert.IsTrue(soldier.Move(goal));
            float deadline = Time.realtimeSinceStartup + 7;
            while (Vector3.Distance(soldier.transform.position,goal) > 1 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.Less(Vector3.Distance(soldier.transform.position,goal),1);
        }
        [UnityTest] public IEnumerator PointerSelectionCommandsAndUIIsolation()
        {
            var background = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            var editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            var mouse = InputSystem.AddDevice<Mouse>();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.EnableDevice(mouse); InputSystem.EnableDevice(keyboard);
            try
            {
                var commander = Object.FindFirstObjectByType<StrategyCommander>();
                var worker = match.Entities.First(e => e.kind == EntityKind.Worker);
                yield return null;
                Vector2 point = commander.view.WorldToScreenPoint(worker.transform.position + Vector3.up);
                Assert.Greater(point.y,158, "Worker must be outside HUD; screen " + Screen.width + "x" + Screen.height + ", projected " + point);
                Physics.SyncTransforms();
                Assert.IsTrue(Physics.Raycast(commander.view.ScreenPointToRay(point),out var pick,300));
                Assert.AreEqual(worker,pick.collider.GetComponentInParent<StrategyEntity>(),"Ray hit " + pick.collider.name);
                SendPointer(commander,mouse,new MouseState { position = point, buttons = 1 });
                yield return null;
                Assert.IsTrue(commander.Dragging, "Mouse " + mouse.leftButton.isPressed + ", current " + (Mouse.current == mouse) + ", running " + match.Running + ", pointer " + commander.Pointer);
                SendPointer(commander,mouse,new MouseState { position = point });
                yield return null;
                Assert.Contains(worker,commander.Selection);
                Vector3 goal = new(-8,0,-5);
                point = commander.view.WorldToScreenPoint(goal);
                Assert.Greater(point.y,158);
                Assert.Less(point.y,Screen.height-76);
                SendPointer(commander,mouse,new MouseState { position = point, buttons = 2 });
                yield return null;
                SendPointer(commander,mouse,new MouseState { position = point });
                yield return null;
                Assert.Less(Vector3.Distance(worker.Agent.destination,goal),1);
                point = new Vector2(40, Screen.height - 30);
                SendPointer(commander,mouse,new MouseState { position = point, buttons = 1 });
                yield return null;
                SendPointer(commander,mouse,new MouseState { position = point });
                yield return null;
                Assert.Contains(worker,commander.Selection);
                Vector3 before = worker.Agent.destination;
                match.SetPaused(true);
                point = commander.view.WorldToScreenPoint(new Vector3(10,0,-3));
                SendPointer(commander,mouse,new MouseState { position = point, buttons = 2 });
                yield return null;
                Assert.AreEqual(before,worker.Agent.destination);
                match.SetPaused(false);
            }
            finally
            {
                InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
                InputSystem.settings.editorInputBehaviorInPlayMode = editorBehavior;
#endif
                InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(keyboard);
            }
        }
        [UnityTest] public IEnumerator AttackMoveResumesAndExplicitAttackKeepsTarget()
        {
            match.settings.Profile(EntityKind.Soldier).damage=1000; match.settings.Profile(EntityKind.Soldier).range=4;
            Assert.IsTrue(match.TrySpawnUnit(EntityKind.Soldier,new Vector3(-10,0,0),out var soldier));
            var origin=soldier.transform.position;
            var target=match.Spawn(EntityKind.Enemy,origin+Vector3.forward*2);
            var closer=match.Spawn(EntityKind.Runner,origin+Vector3.right);
            soldier.Attack(target); Assert.AreEqual(target,soldier.AttackTarget);
            yield return null;
            Assert.IsFalse(target.Alive,"Explicit attack must retain the requested target.");
            closer.Damage(99999);
            var goal=origin+Vector3.forward*10;
            var blocker=match.Spawn(EntityKind.Brute,origin+Vector3.forward*3);
            Assert.IsTrue(soldier.AttackMove(goal));
            float deadline=Time.realtimeSinceStartup+8;
            while(Vector3.Distance(soldier.transform.position,goal)>1 && Time.realtimeSinceStartup<deadline) yield return null;
            Assert.IsFalse(blocker.Alive); Assert.Less(Vector3.Distance(soldier.transform.position,goal),1);
        }
        [UnityTest] public IEnumerator RallyGroupsAndPausedCommands()
        {
            match.Wallet.Deposit(1000); Assert.IsTrue(match.Build(EntityKind.Barracks,new Vector3(-9,0,-11)));
            yield return new WaitForSeconds(.3f);
            var producer=match.Entities.First(e=>e.kind==EntityKind.Barracks);
            var goal=new Vector3(-5,0,0); Assert.IsTrue(producer.SetRallyPoint(goal));
            Assert.IsFalse(producer.SetRallyPoint(new Vector3(200,0,200))); Assert.Less(Vector3.Distance(goal,producer.RallyPoint.Value),.1f); goal=producer.RallyPoint.Value;
            match.settings.Profile(EntityKind.Soldier).trainSeconds=.1f; Assert.IsTrue(match.Train(producer)); yield return new WaitForSeconds(.3f);
            var soldier=match.Entities.First(e=>e.kind==EntityKind.Soldier);
            Assert.AreEqual(UnitOrder.AttackMove,soldier.Order); Assert.AreEqual(goal,soldier.OrderDestination);
            var commander=Object.FindFirstObjectByType<StrategyCommander>(); commander.Select(soldier); commander.StoreGroup(1); commander.ClearSelection(); commander.RecallGroup(1);
            Assert.Contains(soldier,commander.Selection);
            match.SetPaused(true); Assert.IsFalse(soldier.Move(Vector3.zero)); Assert.IsFalse(soldier.AttackMove(Vector3.zero));
            Assert.IsFalse(producer.SetRallyPoint(Vector3.zero)); Assert.IsFalse(commander.IssueAttackMove(Vector3.zero)); Assert.AreEqual(goal,soldier.OrderDestination);
            match.SetPaused(false); soldier.Damage(99999); commander.ClearSelection(); commander.RecallGroup(1); Assert.IsEmpty(commander.Selection);
        }
        [UnityTest] public IEnumerator PendingWaveRetainsVariantsWhenNavigationIsUnavailable()
        {
            // Temporarily remove the baked surface: every spawn must wait, then recover.
            var surface=Object.FindFirstObjectByType<Unity.AI.Navigation.NavMeshSurface>();
            match.settings.waveCompositions=new[]{new WaveComposition(0,2,1)};
            match.Waves.Tick(1000,0,true); // Activate a wave, then feed its pending spawn queue.
            var field=typeof(StrategyMatch).GetField("pendingEnemies",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            var pending=(System.Collections.Generic.Queue<EntityKind>)field.GetValue(match);
            foreach(var kind in match.settings.Composition(1).Enemies()) pending.Enqueue(kind);
            surface.RemoveData();
            try
            {
                yield return null; yield return null;
                Assert.AreEqual(3,match.HostileCount); Assert.AreEqual(3,pending.Count);
                Assert.AreEqual(MatchResult.Playing,match.Waves.Result);
            }
            finally { surface.AddData(); }
            yield return null; yield return null;
            Assert.AreEqual(0,pending.Count);
            Assert.AreEqual(2,match.Entities.Count(e=>e.kind==EntityKind.Runner));
            Assert.AreEqual(1,match.Entities.Count(e=>e.kind==EntityKind.Brute));
        }
        [UnityTest] public IEnumerator TargetingCancellationAndHudClicksPreserveOrders()
        {
            var mouse=InputSystem.AddDevice<Mouse>(); var keyboard=InputSystem.AddDevice<Keyboard>();
            var background=InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            var editorBehavior=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.EnableDevice(mouse); InputSystem.EnableDevice(keyboard);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); InputSystem.Update();
            try
            {
                var commander=Object.FindFirstObjectByType<StrategyCommander>();
                Assert.IsTrue(match.TrySpawnUnit(EntityKind.Soldier,new Vector3(-6,0,-5),out var soldier));
                commander.ClearSelection(); commander.Select(soldier);
                var goal=new Vector3(5,0,-5); Assert.IsTrue(soldier.Move(goal));
                commander.BeginAttackMove(); Assert.IsTrue(commander.TargetingAttackMove);
                var point=new Vector2(40,Screen.height-30);
                SendPointer(commander,mouse,new MouseState{position=point,buttons=1});
                SendPointer(commander,mouse,new MouseState{position=point});
                Assert.IsTrue(commander.TargetingAttackMove); Assert.AreEqual(goal,soldier.OrderDestination);
                yield return null;
                point=commander.view.WorldToScreenPoint(new Vector3(0,0,-5));
                SendPointer(commander,mouse,new MouseState{position=point,buttons=2});
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point}); InputSystem.Update();
                Assert.IsFalse(commander.TargetingAttackMove); Assert.AreEqual(goal,soldier.OrderDestination);
                yield return null;
                commander.BeginAttackMove();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape)); InputSystem.Update(); commander.SendMessage("Update");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); InputSystem.Update();
                Assert.IsFalse(commander.TargetingAttackMove); Assert.IsFalse(match.Paused); Assert.IsFalse(commander.Dragging);
                Assert.Contains(soldier,commander.Selection); Assert.AreEqual(goal,soldier.OrderDestination);
                commander.BeginOrder(UnitOrder.Move);
                point=new Vector2(40,Screen.height-30);
                SendPointer(commander,mouse,new MouseState{position=point,buttons=1});
                SendPointer(commander,mouse,new MouseState{position=point});
                Assert.AreEqual(UnitOrder.Move,commander.TargetingOrder); Assert.AreEqual(goal,soldier.OrderDestination);
                point=commander.view.WorldToScreenPoint(new Vector3(0,0,-5));
                SendPointer(commander,mouse,new MouseState{position=point,buttons=1});
                SendPointer(commander,mouse,new MouseState{position=point});
                Assert.IsFalse(commander.Targeting); Assert.Less(Vector3.Distance(new Vector3(0,0,-5),soldier.OrderDestination),.5f);
                commander.ClearSelection(); var worker=match.Entities.First(e=>e.kind==EntityKind.Worker); commander.Select(worker);
                commander.BeginOrder(UnitOrder.Gather);
                SendPointer(commander,mouse,new MouseState{position=point,buttons=1});
                SendPointer(commander,mouse,new MouseState{position=point});
                Assert.AreEqual(UnitOrder.Gather,commander.TargetingOrder,"Invalid gather target must keep targeting active.");
                var deposit=Object.FindObjectsByType<MineralDeposit>(FindObjectsSortMode.None).OrderBy(d=>d.transform.position.x).First();
                point=commander.view.WorldToScreenPoint(deposit.transform.position+Vector3.up);
                SendPointer(commander,mouse,new MouseState{position=point,buttons=1});
                SendPointer(commander,mouse,new MouseState{position=point});
                Assert.IsFalse(commander.Targeting); Assert.AreSame(deposit,worker.MiningTarget);
                match.SetPaused(true); commander.BeginOrder(UnitOrder.Move); Assert.IsFalse(commander.Targeting); match.SetPaused(false);
                yield return null;
            }
            finally
            {
#if UNITY_EDITOR
                InputSystem.settings.editorInputBehaviorInPlayMode=editorBehavior;
#endif
                InputSystem.settings.backgroundBehavior=background; InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(keyboard);
            }
        }
        static void SendPointer(StrategyCommander commander, Mouse mouse, MouseState state)
        {
            // Pump synthetic events explicitly; the batch editor does not reliably
            // advance device events before UnityTest coroutine continuations.
            InputSystem.QueueStateEvent(mouse,state);
            InputSystem.Update();
            Assert.AreEqual(state.position,mouse.position.ReadValue(),"Synthetic pointer state was not applied.");
            commander.SendMessage("Update");
        }
    }
}
