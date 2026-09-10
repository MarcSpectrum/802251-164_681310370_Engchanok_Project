using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Engchanok.StrategyGame.Tests
{
    public sealed class CameraTests
    {
        StrategyMatch match;
        StrategyCommander commander;
        StrategyCameraController camera;
        [UnitySetUp] public IEnumerator Setup()
        {
            StrategySession.PracticeRequested = false;
            SceneManager.LoadScene("Survival");
            yield return null; yield return null;
            match = Object.FindFirstObjectByType<StrategyMatch>();
            commander = match.GetComponent<StrategyCommander>(); camera = commander.CameraController;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            SceneManager.LoadScene("MainMenu"); yield return null;
        }
        [UnityTest] public IEnumerator InspectionFreezesSimulationAndRestoresSelectionAndPose()
        {
            var worker = match.Entities.First(e => e.kind == EntityKind.Worker);
            var deposit = Object.FindFirstObjectByType<MineralDeposit>();
            var soldier = match.Spawn(EntityKind.Soldier,new Vector3(5,0,-8));
            var enemy = match.Spawn(EntityKind.Enemy,new Vector3(6,0,-8));
            soldier.Attack(enemy);
            float enemyHealth = enemy.Health.Current;
            worker.Gather(deposit); commander.Select(worker);
            Assert.IsTrue(match.Train(match.Headquarters));
            Assert.IsTrue(match.StartResearch(UpgradeKind.Mining));
            commander.BeginOrder(UnitOrder.Move);
            Vector3 position = camera.view.transform.position, workerPosition = worker.transform.position;
            Quaternion rotation = camera.view.transform.rotation;
            float countdown = match.Waves.Countdown, research = match.Research.Remaining, training = match.Headquarters.Production.Remaining;
            int stock = deposit.Stock.Remaining;
            Assert.IsTrue(camera.BeginInspection(worker.transform));
            Assert.IsFalse(commander.Targeting); Assert.IsFalse(match.Running); Assert.IsFalse(match.Paused);
            Assert.IsTrue(match.InspectionPaused); Assert.AreEqual(0, Time.timeScale);
            Assert.IsFalse(worker.Move(Vector3.zero)); Assert.IsFalse(match.Train(match.Headquarters));
            yield return new WaitForSecondsRealtime(.5f);
            Assert.AreEqual(StrategyCameraState.Inspecting, camera.State);
            Assert.AreEqual(countdown, match.Waves.Countdown); Assert.AreEqual(research, match.Research.Remaining);
            Assert.AreEqual(training, match.Headquarters.Production.Remaining); Assert.AreEqual(stock, deposit.Stock.Remaining);
            Assert.AreEqual(workerPosition, worker.transform.position); Assert.AreEqual(enemyHealth, enemy.Health.Current);
            Assert.IsFalse(match.GetComponent<StrategyHud>().canvas.transform.Find("Mission overlay").gameObject.activeSelf);
            Assert.IsFalse(match.GetComponent<StrategyHud>().canvas.transform.Find("Commands").gameObject.activeSelf);
            foreach (Transform child in match.GetComponent<StrategyHud>().canvas.transform)
                if (child.name != "Inspection controls") Assert.IsFalse(child.gameObject.activeSelf,"Inspection leaked HUD: "+child.name);
            camera.EndInspection(); Assert.IsTrue(match.InspectionPaused);
            yield return new WaitForSecondsRealtime(.4f);
            Assert.IsTrue(match.Running); Assert.AreEqual(1, Time.timeScale);
            Assert.Less(Vector3.Distance(position, camera.view.transform.position), .001f);
            Assert.Less(Quaternion.Angle(rotation, camera.view.transform.rotation), .001f);
            CollectionAssert.AreEqual(new[] { worker }, commander.Selection);
        }
        [UnityTest] public IEnumerator TargetsFrameAcrossAspectRatiosAndPauseOwnershipIsPreserved()
        {
            var enemy = match.Spawn(EntityKind.Brute, new Vector3(0,0,15));
            var targets = new[] { match.Entities.First(e => e.kind == EntityKind.Worker).transform, match.Headquarters.transform, enemy.transform, Object.FindFirstObjectByType<MineralDeposit>().transform };
            foreach (float aspect in new[] { 1280f/720, 1920f/1080, 2560f/1080 })
            foreach (var target in targets)
            {
                camera.view.aspect = aspect;
                Assert.IsTrue(camera.BeginInspection(target));
                yield return new WaitForSecondsRealtime(.4f);
                Bounds bounds = StrategyCameraController.VisualBounds(target);
                for (int i=0; i<8; i++)
                {
                    Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    Vector3 point = camera.view.WorldToViewportPoint(corner);
                    Assert.That(point.x, Is.InRange(0f,1f)); Assert.That(point.y, Is.InRange(0f,1f)); Assert.Greater(point.z,camera.view.nearClipPlane);
                }
                camera.EndInspection(); yield return new WaitForSecondsRealtime(.4f);
            }
            camera.view.ResetAspect();
            match.SetPaused(true); Assert.IsFalse(camera.BeginInspection(targets[0]));
            match.SetPaused(false); Assert.IsTrue(camera.BeginInspection(targets[0]));
            match.SetPaused(true); camera.EndInspection(); yield return new WaitForSecondsRealtime(.4f);
            Assert.IsTrue(match.Paused); Assert.IsFalse(match.InspectionPaused); Assert.AreEqual(0,Time.timeScale);
        }
        [UnityTest] public IEnumerator PickingCancellationLostTargetAndRestartAreSafe()
        {
            var worker = match.Entities.First(e => e.kind == EntityKind.Worker);
            commander.Select(worker); commander.BeginPlacement(EntityKind.Turret);
            camera.BeginPicking(); Assert.IsTrue(camera.Picking); Assert.IsNull(commander.Placement); Assert.IsTrue(match.Running);
            Assert.IsFalse(camera.BeginInspection(new GameObject("Invalid target").transform)); Assert.IsTrue(camera.Picking);
            camera.CancelPicking(); Assert.AreEqual(StrategyCameraState.Tactical,camera.State);
            Assert.IsTrue(camera.BeginInspection(worker.transform)); Object.Destroy(worker.gameObject);
            yield return new WaitForSecondsRealtime(.5f);
            Assert.IsFalse(camera.Inspecting); Assert.IsTrue(match.Running);
            camera.BeginInspection(match.Headquarters.transform); match.Restart(); yield return null; yield return null;
            Assert.IsFalse(Object.FindFirstObjectByType<StrategyMatch>().InspectionPaused); Assert.AreEqual(1,Time.timeScale);
        }
        [UnityTest] public IEnumerator PointerPickingOrbitAndUIIsolation()
        {
            var mouse = InputSystem.AddDevice<Mouse>(); var keyboard = InputSystem.AddDevice<Keyboard>();
            var background = InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
            var editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.EnableDevice(mouse); InputSystem.EnableDevice(keyboard);
            try
            {
                var worker = match.Entities.First(e=>e.kind==EntityKind.Worker); commander.Select(worker);
                Vector3 destination = worker.OrderDestination;
                yield return null;
                camera.BeginPicking();
                Send(mouse,new MouseState { position = new Vector2(Screen.width*.95f,Screen.height*.95f),buttons=1 });
                Assert.IsTrue(camera.Picking); Assert.IsFalse(match.InspectionPaused);
                Send(mouse,new MouseState());
                Vector2 point = camera.view.WorldToScreenPoint(worker.transform.position+Vector3.up);
                Physics.SyncTransforms();
                Assert.IsTrue(Physics.Raycast(camera.view.ScreenPointToRay(point),out var hit,300));
                Assert.AreSame(worker,hit.collider.GetComponentInParent<StrategyEntity>(),"Target ray hit "+hit.collider.name);
                Send(mouse,new MouseState { position=point,buttons=1 });
                Assert.IsTrue(camera.Inspecting,"State="+camera.State+", UI="+commander.PointerOverUI+", pointer="+commander.Pointer+", expected="+point+", pressed="+mouse.leftButton.wasPressedThisFrame);
                Send(mouse,new MouseState { position=point });
                yield return new WaitForSecondsRealtime(.4f);
                Quaternion before = camera.view.transform.rotation;
                Vector2 ui = new Vector2(Screen.width*.4f,Screen.height*.06f);
                Send(mouse,new MouseState { position=ui,buttons=1,delta=new Vector2(80,20),scroll=new Vector2(0,120) }); camera.SendMessage("LateUpdate");
                Assert.Less(Quaternion.Angle(before,camera.view.transform.rotation),.001f);
                Send(mouse,new MouseState { position=ui }); camera.SendMessage("LateUpdate");
                point=new Vector2(Screen.width*.5f,Screen.height*.5f);
                Send(mouse,new MouseState { position=point,buttons=1,delta=new Vector2(80,20) }); camera.SendMessage("LateUpdate");
                Assert.Greater(Quaternion.Angle(before,camera.view.transform.rotation),1);
                Send(mouse,new MouseState { position=point,buttons=2 });
                Assert.AreEqual(destination,worker.OrderDestination); CollectionAssert.AreEqual(new[]{worker},commander.Selection);
                Send(mouse,new MouseState { position=point });
            }
            finally
            {
                InputSystem.settings.backgroundBehavior=background;
#if UNITY_EDITOR
                InputSystem.settings.editorInputBehaviorInPlayMode=editorBehavior;
#endif
                InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(keyboard);
            }
        }
        [UnityTest] public IEnumerator FinishedMatchAndDisabledCameraReleaseInspectionSafely()
        {
            camera.BeginInspection(match.Headquarters.transform);
            camera.enabled = false;
            Assert.IsFalse(match.InspectionPaused); Assert.IsTrue(match.Running);
            camera.enabled = true;
            camera.BeginInspection(match.Headquarters.transform);
            match.Waves.Tick(0,0,false);
            yield return null; yield return null;
            Assert.IsFalse(camera.Inspecting); Assert.IsFalse(match.InspectionPaused);
            Assert.IsFalse(match.Running); Assert.AreEqual(0,Time.timeScale);
            Assert.IsFalse(camera.BeginInspection(match.Headquarters.transform));
        }
        static void Send(Mouse mouse, MouseState state)
        {
            InputSystem.QueueStateEvent(mouse,state); InputSystem.Update();
            Object.FindFirstObjectByType<StrategyCommander>().SendMessage("Update");
        }
        [UnityTest] public IEnumerator FocusAndHeadquartersResetStayBounded()
        {
            var start = camera.view.transform.position;
            camera.FocusSelection(); yield return new WaitForSecondsRealtime(.2f); Assert.AreEqual(start,camera.view.transform.position);
            foreach(var e in match.Entities.Where(e=>e.kind==EntityKind.Worker)) commander.Select(e);
            camera.FocusSelection(); yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(camera.view.transform.position.y,Is.InRange(18f,60f));
            camera.ResetToHeadquarters(); yield return new WaitForSecondsRealtime(1.5f);
            Assert.Less(Vector3.Distance(start,camera.view.transform.position),.01f);
        }
    }
}
