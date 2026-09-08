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
            match.settings.soldierTraining = .1f;
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
            var copy = match.settings; match.settings = original; Object.Destroy(copy);
            match.Restart(); yield return null; yield return null;
            match = Object.FindFirstObjectByType<StrategyMatch>();
            original = match.settings; match.settings = Object.Instantiate(original);
            Assert.AreEqual(MatchResult.Playing, match.Waves.Result);
            Assert.AreEqual(0, match.Waves.Wave);
            Assert.AreEqual(original.startingMinerals, match.Wallet.Minerals);
            Assert.AreEqual(4, match.Entities.Count);
            Assert.AreEqual(1, Time.timeScale);
        }
        [UnityTest] public IEnumerator HeadquartersDeathEndsMatchAndMenuLoads()
        {
            match.Headquarters.Damage(99999);
            yield return null; yield return null;
            Assert.AreEqual(MatchResult.Defeat, match.Waves.Result);
            Assert.AreEqual(0, Time.timeScale);
            Assert.IsFalse(match.Running);
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
