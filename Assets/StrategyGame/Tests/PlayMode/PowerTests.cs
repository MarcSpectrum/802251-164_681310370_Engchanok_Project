using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Engchanok.StrategyGame.Tests
{
    public sealed class PowerTests
    {
        StrategyMatch match;
        StrategySettings original;
        static readonly Vector3 Target = new(0, 0, 12);
        [UnitySetUp] public IEnumerator Setup()
        {
            StrategySession.PracticeRequested = false;
            SceneManager.LoadScene("Survival");
            yield return null; yield return null;
            match = Object.FindFirstObjectByType<StrategyMatch>();
            original = match.settings;
            match.settings = Object.Instantiate(original);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (match != null) { var copy = match.settings; match.settings = original; Object.Destroy(copy); }
            Time.timeScale = 1; SceneManager.LoadScene("MainMenu"); yield return null;
        }
        // Hostiles that hold still and cannot hurt anything, so only the power under test changes their health.
        void Harmless() { match.settings.enemySpeed = .01f; match.settings.enemyDamage = 0; }
        static bool Dead(StrategyEntity e) => e == null || !e.Alive;
        [UnityTest] public IEnumerator AirstrikeDamagesHostilesInTheBlastOnly()
        {
            Harmless();
            // The soldier sits under the first bomb and ten units from the hostile, outside both weapon ranges.
            var soldier = match.Spawn(EntityKind.Soldier, Target + new Vector3(0, 0, -8));
            var inside = match.Spawn(EntityKind.Enemy, Target + new Vector3(0, 0, 2));
            var outside = match.Spawn(EntityKind.Enemy, Target + new Vector3(20, 0, 0));
            var power = match.settings.PowerOf(PowerKind.Airstrike);
            int minerals = match.Wallet.Minerals;
            yield return null;
            Assert.IsTrue(match.UsePower(PowerKind.Airstrike, Target));
            Assert.AreEqual(minerals - power.cost, match.Wallet.Minerals);
            Assert.AreEqual(power.cooldown, match.Powers.Remaining(PowerKind.Airstrike), .1f);
            var powers = StrategyPowers.For(match);
            Assert.AreEqual(1, powers.ActiveStrikes); Assert.AreEqual(1, powers.ActiveJets, "An airstrike flies a jet.");
            yield return new WaitForSeconds(power.delay + .6f);
            Assert.IsTrue(Dead(inside), "Two bombs land on a standard hostile beside the target.");
            Assert.AreEqual(outside.Health.Maximum, outside.Health.Current, "A hostile outside the blast is untouched.");
            Assert.AreEqual(soldier.Health.Maximum, soldier.Health.Current, "Commander strikes never hurt friendly units.");
            Assert.AreEqual(match.settings.headquartersHealth, match.Headquarters.Health.Current);
            float deadline = Time.time + 3;
            while (powers.ActiveStrikes > 0 && Time.time < deadline) yield return null;
            Assert.AreEqual(0, powers.ActiveStrikes, "The strike finishes once the jet leaves.");
            Assert.IsNull(GameObject.Find("Strike jet"), "The jet is removed after its run.");
            Assert.AreEqual(1, match.Stats.HostilesDefeated, "Power kills count in the mission report.");
        }
        [UnityTest] public IEnumerator PowersRespectCostCooldownPauseAndMatchEnd()
        {
            Harmless();
            Assert.IsTrue(match.Wallet.TrySpend(match.Wallet.Minerals - 50));
            Assert.IsFalse(match.UsePower(PowerKind.Airstrike, Target), "An unaffordable power is refused.");
            Assert.AreEqual(50, match.Wallet.Minerals); Assert.IsTrue(match.Powers.Ready(PowerKind.Airstrike));
            match.Wallet.Deposit(1000);
            Assert.IsFalse(match.UsePower(PowerKind.Barrage, new Vector3(100, 0, 0)), "A point off the battlefield is refused.");
            Assert.AreEqual(1050, match.Wallet.Minerals); Assert.IsTrue(match.Powers.Ready(PowerKind.Barrage));
            var hostile = match.Spawn(EntityKind.Enemy, Target);
            yield return null;
            Assert.IsTrue(match.UsePower(PowerKind.Airstrike, Target));
            int afterStrike = match.Wallet.Minerals;
            Assert.IsFalse(match.UsePower(PowerKind.Airstrike, Target), "A recharging power is refused.");
            Assert.AreEqual(afterStrike, match.Wallet.Minerals);
            match.SetPaused(true);
            float remaining = match.Powers.Remaining(PowerKind.Airstrike);
            var jet = GameObject.Find("Strike jet").transform; var jetPosition = jet.position;
            Assert.IsFalse(match.UsePower(PowerKind.CryoField, Target), "A paused mission rejects powers.");
            yield return new WaitForSecondsRealtime(2.5f);
            Assert.AreEqual(hostile.Health.Maximum, hostile.Health.Current, "Pause holds a pending strike.");
            Assert.AreEqual(remaining, match.Powers.Remaining(PowerKind.Airstrike), "Pause freezes the recharge.");
            Assert.AreEqual(jetPosition, jet.position, "Pause freezes the jet.");
            match.SetPaused(false);
            yield return new WaitForSeconds(2.5f);
            Assert.IsTrue(Dead(hostile), "The strike lands once the mission resumes.");
            Assert.Less(match.Powers.Remaining(PowerKind.Airstrike), remaining);
            Assert.IsTrue(match.UsePower(PowerKind.CryoField, Target));
            Assert.AreEqual(1, StrategyPowers.For(match).ActiveFields);
            match.Headquarters.Damage(100000);
            yield return null; yield return null;
            Assert.AreEqual(MatchResult.Defeat, match.Waves.Result);
            Assert.AreEqual(0, StrategyPowers.For(match).ActiveFields, "A finished mission clears every power.");
            Assert.IsFalse(Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None).Any(l => l.name.EndsWith(" zone")), "Zone outlines leave with the mission.");
            Assert.IsFalse(match.UsePower(PowerKind.RepairField, Target), "A finished mission rejects powers.");
        }
        [UnityTest] public IEnumerator CryoFieldSlowsHostilesThenExpires()
        {
            match.settings.enemyDamage = 0;
            var cryo = match.settings.PowerOf(PowerKind.CryoField); cryo.duration = 1.5f;
            var runner = match.Spawn(EntityKind.Runner, Target);
            var distant = match.Spawn(EntityKind.Runner, Target + new Vector3(25, 0, 0));
            float speed = match.settings.Speed(EntityKind.Runner);
            yield return null;
            Assert.AreEqual(speed, runner.Agent.speed, 1e-3);
            Assert.IsTrue(match.UsePower(PowerKind.CryoField, runner.transform.position));
            yield return new WaitForSeconds(cryo.delay + .2f);
            Assert.IsTrue(runner.Chilled, "A hostile inside the field is chilled.");
            Assert.AreEqual(cryo.amount, runner.Pace, 1e-3);
            Assert.AreEqual(speed * cryo.amount, runner.Agent.speed, 1e-3, "Chill slows movement.");
            Assert.IsFalse(distant.Chilled, "A hostile outside the field keeps its pace.");
            yield return new WaitForSeconds(cryo.duration + .6f);
            Assert.AreEqual(0, StrategyPowers.For(match).ActiveFields);
            Assert.IsFalse(runner.Chilled, "Chill wears off after the field expires.");
            Assert.AreEqual(speed, runner.Agent.speed, 1e-3, "Speed recovers.");
        }
        [UnityTest] public IEnumerator RepairFieldMendsFriendliesOnly()
        {
            Harmless(); match.settings.turretDamage = 0; match.settings.Profile(EntityKind.Soldier).damage = 0;
            var center = new Vector3(6, 0, 0);
            var turret = match.Spawn(EntityKind.Turret, center);
            var soldier = match.Spawn(EntityKind.Soldier, center + new Vector3(2, 0, 3));
            var hostile = match.Spawn(EntityKind.Enemy, center + new Vector3(-3, 0, -1));
            yield return null;
            turret.Health.Damage(200); soldier.Health.Damage(60); hostile.Health.Damage(40);
            float turretBefore = turret.Health.Current, soldierBefore = soldier.Health.Current, hostileBefore = hostile.Health.Current;
            Assert.IsTrue(match.UsePower(PowerKind.RepairField, center));
            yield return new WaitForSeconds(match.settings.PowerOf(PowerKind.RepairField).delay + 1.5f);
            Assert.Greater(turret.Health.Current, turretBefore, "The field repairs structures.");
            Assert.Greater(soldier.Health.Current, soldierBefore, "The field heals units.");
            Assert.AreEqual(hostileBefore, hostile.Health.Current, "The field never heals hostiles.");
        }
        [UnityTest] public IEnumerator PowerHotkeyTargetsAndCancels()
        {
            var commander = match.GetComponent<StrategyCommander>();
            var hud = match.GetComponent<StrategyHud>();
            var restore = SurvivalTests.UseSyntheticInput(out var mouse, out var keyboard);
            try
            {
                commander.InspectObject(match.Headquarters.transform); yield return null;
                Assert.IsTrue(hud.PopupVisible);
                StringAssert.StartsWith("[F1] AIRSTRIKE", hud.PowerButton(PowerKind.Airstrike).GetComponentInChildren<Text>().text);
                int minerals = match.Wallet.Minerals;
                SurvivalTests.Press(keyboard, Key.F1, commander);
                Assert.AreEqual(PowerKind.Airstrike, commander.TargetingPower); Assert.IsTrue(commander.Targeting);
                yield return null;
                Assert.IsFalse(hud.PopupVisible, "Aiming a power hides the popup like any other targeting.");
                SurvivalTests.Press(keyboard, Key.Escape, commander);
                Assert.IsFalse(commander.Targeting); Assert.AreEqual(minerals, match.Wallet.Minerals, "Cancelling costs nothing.");
                Assert.IsFalse(match.Paused, "Escape cancels aiming before it pauses.");
                SurvivalTests.Press(keyboard, Key.F3, commander); Assert.AreEqual(PowerKind.CryoField, commander.TargetingPower);
                SurvivalTests.Press(keyboard, Key.F1, commander); Assert.AreEqual(PowerKind.Airstrike, commander.TargetingPower, "Another power key switches the aim.");
                SurvivalTests.Press(keyboard, Key.F1, commander); Assert.IsNull(commander.TargetingPower, "The same key puts the power away.");
                // Aim, then call it in from the minimap: powers complete targeting through the same path as orders.
                var minimap = hud.Minimap;
                SurvivalTests.Press(keyboard, Key.F1, commander);
                SurvivalTests.SendPointer(commander, mouse, new MouseState { position = minimap.ScreenPoint(Target), buttons = 1 });
                SurvivalTests.SendPointer(commander, mouse, new MouseState { position = minimap.ScreenPoint(Target) });
                Assert.IsFalse(commander.Targeting);
                Assert.AreEqual(minerals - match.settings.PowerOf(PowerKind.Airstrike).cost, match.Wallet.Minerals);
                Assert.AreEqual(1, StrategyPowers.For(match).ActiveStrikes);
                SurvivalTests.Press(keyboard, Key.F1, commander);
                Assert.IsNull(commander.TargetingPower, "A recharging power cannot be aimed.");
                StringAssert.Contains("Recharging", match.Notice);
                yield return null;
                var label = hud.PowerButton(PowerKind.Airstrike).GetComponentInChildren<Text>().text;
                StringAssert.Contains("Recharging", label); Assert.IsFalse(hud.PowerButton(PowerKind.Airstrike).interactable);
                match.SetPaused(true);
                SurvivalTests.Press(keyboard, Key.F2, commander);
                Assert.IsNull(commander.TargetingPower, "Paused missions ignore power keys.");
                match.SetPaused(false);
            }
            finally { restore(); }
        }
        [UnityTest] public IEnumerator IdleWardenMarchesOnHeadquarters()
        {
            // The soft-lock: a warden with nobody to mend used to stand where it spawned, holding its wave open forever.
            var warden = match.Spawn(EntityKind.Warden, new Vector3(0, 0, 26));
            yield return null;
            float start = Vector3.Distance(warden.transform.position, match.Headquarters.transform.position);
            Time.timeScale = 5;
            float deadline = Time.realtimeSinceStartup + 8;
            while (Vector3.Distance(warden.transform.position, match.Headquarters.transform.position) > start - 10 && Time.realtimeSinceStartup < deadline) yield return null;
            Time.timeScale = 1;
            Assert.Less(Vector3.Distance(warden.transform.position, match.Headquarters.transform.position), start - 10, "An idle warden must march on headquarters.");
            Assert.AreEqual(match.settings.headquartersHealth, match.Headquarters.Health.Current, "A warden is unarmed.");
            Assert.IsNull(warden.AttackTarget);
        }
    }
}
