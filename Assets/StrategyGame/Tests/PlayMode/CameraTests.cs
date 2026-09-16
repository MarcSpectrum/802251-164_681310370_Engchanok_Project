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
    public sealed class CameraTests
    {
        StrategyMatch match;
        StrategyCommander commander;
        StrategyCameraController camera;
        StrategyHud hud;
        Transform Popup => hud.canvas.transform.Find("Object popup");
        Button Action(string name) => Popup.Find("Action viewport/Actions/"+name).GetComponent<Button>();
        string[] Actions => Popup.Find("Action viewport/Actions").GetComponentsInChildren<Button>().Select(b=>b.name).ToArray();
        [UnitySetUp] public IEnumerator Setup()
        {
            StrategySession.PracticeRequested=false; SceneManager.LoadScene("Survival");
            yield return null; yield return null;
            match=Object.FindFirstObjectByType<StrategyMatch>(); commander=match.GetComponent<StrategyCommander>();
            camera=commander.CameraController; hud=match.GetComponent<StrategyHud>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            SceneManager.LoadScene("MainMenu"); yield return null;
        }
        [UnityTest] public IEnumerator InspectionKeepsCameraAndSimulationLive()
        {
            var worker=match.Entities.First(e=>e.kind==EntityKind.Worker);
            worker.Gather(Object.FindFirstObjectByType<MineralDeposit>());
            Assert.IsTrue(match.Train(match.Headquarters)); Assert.IsTrue(match.StartResearch(UpgradeKind.Mining));
            var position=camera.view.transform.position; var rotation=camera.view.transform.rotation;
            float countdown=match.Waves.Countdown, research=match.Research.Remaining, training=match.Headquarters.Production.Remaining;
            Assert.IsTrue(commander.InspectObject(worker.transform));
            yield return new WaitForSecondsRealtime(.5f);
            Assert.IsTrue(hud.PopupVisible); Assert.IsTrue(match.Running); Assert.AreEqual(1,Time.timeScale);
            Assert.Less(match.Waves.Countdown,countdown); Assert.Less(match.Research.Remaining,research);
            Assert.Less(match.Headquarters.Production.Remaining,training);
            Assert.Less(Vector3.Distance(position,camera.view.transform.position),.001f);
            Assert.Less(Quaternion.Angle(rotation,camera.view.transform.rotation),.001f);
            CollectionAssert.AreEqual(new[]{worker},commander.Selection);
            Assert.IsTrue(hud.canvas.transform.Find("Status").gameObject.activeSelf);
        }
        [UnityTest] public IEnumerator EveryObjectShowsOnlyRelevantActionsAndLiveDetails()
        {
            commander.InspectObject(match.Headquarters.transform); yield return null;
            CollectionAssert.AreEquivalent(new[]{"Train Worker","Build barracks","Build ranger post","Build support bay","Build turret","Build supply relay","Improved Mining","Soldier Weapons","Turret Weapons"},Actions);
            match.Wallet.TrySpend(match.Wallet.Minerals); yield return null;
            Assert.IsFalse(Action("Train Worker").interactable);
            StringAssert.Contains("Need",Action("Train Worker").GetComponentInChildren<Text>().text);
            match.Wallet.Deposit(1000); Action("Train Worker").onClick.Invoke(); yield return null;
            Assert.AreEqual(1,match.Headquarters.Production.Count);
            Assert.IsTrue(Popup.Find("Training progress").gameObject.activeSelf);
            Action("Improved Mining").onClick.Invoke(); yield return null;
            StringAssert.Contains("Researching",Action("Improved Mining").GetComponentInChildren<Text>().text);
            foreach(var kind in new[]{EntityKind.Worker,EntityKind.Soldier,EntityKind.Barracks,EntityKind.RangerPost,EntityKind.SupportBay,EntityKind.Turret,EntityKind.Enemy,EntityKind.Lancer,EntityKind.Juggernaut})
            {
                var entity=kind==EntityKind.Worker?match.Entities.First(e=>e.kind==kind):match.Spawn(kind,new Vector3(10,0,-10));
                commander.InspectObject(entity.transform); yield return null;
                string[] expected=kind==EntityKind.Worker?new[]{"Move","Gather"}:kind==EntityKind.Soldier?new[]{"Move","Attack-move [F]"}:kind==EntityKind.Barracks?new[]{"Train Soldier","Train Defender","Set rally point"}:kind==EntityKind.RangerPost?new[]{"Train Ranger","Set rally point"}:kind==EntityKind.SupportBay?new[]{"Train Medic","Train Engineer","Set rally point"}:new string[0];
                CollectionAssert.AreEquivalent(expected,Actions,kind.ToString());
                if(entity.IsEnemy) Assert.IsEmpty(commander.Selection);
                entity.Damage(10); yield return null;
                Assert.IsTrue(Popup.GetComponentsInChildren<Text>().Any(t=>t.text.Contains(Mathf.CeilToInt(entity.Health.Current)+" HP")));
                if(kind!=EntityKind.Worker) Object.Destroy(entity.gameObject);
            }
            var deposit=Object.FindFirstObjectByType<MineralDeposit>();
            commander.InspectObject(deposit.transform); deposit.Extract(17); yield return null;
            Assert.IsEmpty(Actions); Assert.IsEmpty(commander.Selection);
            Assert.IsTrue(Popup.GetComponentsInChildren<Text>().Any(t=>t.text.Contains(deposit.Stock.Remaining+" minerals remaining")));
        }
        [UnityTest] public IEnumerator GroupsCloseTargetLossAndPauseRemainSafe()
        {
            var worker=match.Entities.First(e=>e.kind==EntityKind.Worker);
            var soldier=match.Spawn(EntityKind.Soldier,new Vector3(5,0,-10));
            commander.Select(worker); commander.Select(soldier); yield return null;
            CollectionAssert.AreEquivalent(new[]{"Move","Gather","Attack-move [F]"},Actions);
            commander.StoreGroup(1); commander.ClosePopup(); yield return null;
            Assert.IsFalse(hud.PopupVisible); Assert.AreEqual(2,commander.Selection.Count);
            commander.RecallGroup(1); yield return null; Assert.IsTrue(hud.PopupVisible);
            commander.BeginOrder(UnitOrder.Gather); yield return null; Assert.IsFalse(hud.PopupVisible);
            commander.CancelInteractions(); yield return null; Assert.IsTrue(hud.PopupVisible);
            match.SetPaused(true); yield return null; Assert.IsFalse(hud.PopupVisible);
            Assert.IsFalse(commander.InspectObject(worker.transform)); Assert.IsFalse(match.Train(match.Headquarters));
            commander.BeginAttackMove(); Assert.IsFalse(commander.Targeting);
            match.SetPaused(false); commander.InspectObject(soldier.transform); soldier.Damage(99999);
            yield return null; yield return null; Assert.IsFalse(hud.PopupVisible);
            commander.InspectObject(worker.transform); match.Waves.Tick(0,0,false);
            yield return null; Assert.IsFalse(hud.PopupVisible); Assert.IsFalse(worker.Move(Vector3.zero));
            match.Restart(); yield return null; yield return null;
            Assert.IsFalse(Object.FindFirstObjectByType<StrategyHud>().PopupVisible); Assert.AreEqual(1,Time.timeScale);
        }
        [UnityTest] public IEnumerator PointerSelectionPopupIsolationAndRallyTargeting()
        {
            var mouse=InputSystem.AddDevice<Mouse>(); var keyboard=InputSystem.AddDevice<Keyboard>();
            var background=InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
            var editorBehavior=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.EnableDevice(mouse); InputSystem.EnableDevice(keyboard);
            try
            {
                var worker=match.Entities.First(e=>e.kind==EntityKind.Worker);
                Vector2 point=camera.view.WorldToScreenPoint(worker.transform.position+Vector3.up);
                Physics.SyncTransforms(); Send(mouse,new MouseState{position=point,buttons=1}); Send(mouse,new MouseState{position=point});
                yield return null; Assert.IsTrue(hud.PopupVisible); Assert.Contains(worker,commander.Selection);
                var destination=worker.OrderDestination;
                point=RectTransformUtility.WorldToScreenPoint(null,Popup.TransformPoint(new Vector3(50,-35,0)));
                Send(mouse,new MouseState{position=point,buttons=2}); Send(mouse,new MouseState{position=point});
                Assert.AreEqual(destination,worker.OrderDestination); Assert.Contains(worker,commander.Selection);
                commander.BeginOrder(UnitOrder.Move); yield return null;
                point=camera.view.WorldToScreenPoint(new Vector3(-7,0,-5));
                Send(mouse,new MouseState{position=point,buttons=1}); Send(mouse,new MouseState{position=point});
                Assert.IsFalse(commander.Targeting); Assert.Less(Vector3.Distance(worker.OrderDestination,new Vector3(-7,0,-5)),1);
                match.Wallet.Deposit(1000); Assert.IsTrue(match.Build(EntityKind.Barracks,new Vector3(-9,0,-11)));
                var producer=match.Entities.First(e=>e.kind==EntityKind.Barracks);
                commander.InspectObject(producer.transform); yield return null;
                Action("Set rally point").onClick.Invoke(); yield return null; Assert.IsTrue(commander.TargetingRally); Assert.IsFalse(hud.PopupVisible);
                point=camera.view.WorldToScreenPoint(new Vector3(0,0,-5));
                Send(mouse,new MouseState{position=point,buttons=1}); Send(mouse,new MouseState{position=point});
                Assert.IsFalse(commander.TargetingRally); Assert.IsTrue(producer.RallyPoint.HasValue);
                Assert.Less(Vector3.Distance(producer.RallyPoint.Value,new Vector3(0,0,-5)),1);
                commander.BeginRallyPoint(); Send(mouse,new MouseState{position=point,buttons=2}); Send(mouse,new MouseState{position=point});
                Assert.IsFalse(commander.Targeting); Assert.IsFalse(match.Paused);
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
        static void Send(Mouse mouse, MouseState state)
        {
            InputSystem.QueueStateEvent(mouse,state); InputSystem.Update();
            Object.FindFirstObjectByType<StrategyCommander>().SendMessage("Update");
        }
        [UnityTest] public IEnumerator PopupClampsToScreenAndResearchScrolls()
        {
            commander.InspectObject(match.Headquarters.transform); yield return null;
            var corners=new Vector3[4]; ((RectTransform)Popup).GetWorldCorners(corners);
            foreach(var corner in corners)
            {
                Assert.That(corner.x,Is.InRange(0f,(float)Screen.width)); Assert.That(corner.y,Is.InRange(0f,Screen.height*.895f));
            }
            var scroll=Popup.GetComponentInChildren<ScrollRect>();
            Assert.Greater(scroll.content.rect.height,scroll.viewport.rect.height);
            scroll.verticalNormalizedPosition=0; yield return null;
            Assert.Less(scroll.verticalNormalizedPosition,.01f);
            commander.InspectObject(match.Entities.First(e=>e.kind==EntityKind.Worker).transform); yield return null;
            Assert.LessOrEqual(scroll.content.rect.height,scroll.viewport.rect.height+1);
        }
        [UnityTest] public IEnumerator FocusAndHeadquartersResetStayBounded()
        {
            var start=camera.view.transform.position;
            camera.FocusSelection(); yield return new WaitForSecondsRealtime(.2f); Assert.AreEqual(start,camera.view.transform.position);
            foreach(var e in match.Entities.Where(e=>e.kind==EntityKind.Worker)) commander.Select(e);
            camera.FocusSelection(); yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(camera.view.transform.position.y,Is.InRange(18f,60f));
            camera.ResetToHeadquarters(); yield return new WaitForSecondsRealtime(1.5f);
            Assert.Less(Vector3.Distance(start,camera.view.transform.position),.01f);
        }
    }
}
