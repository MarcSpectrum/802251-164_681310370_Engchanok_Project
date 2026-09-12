#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Engchanok.StrategyGame
{
    // Opt-in standalone regression replay. Never runs during normal play.
    public sealed class DevelopmentSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!Environment.GetCommandLineArgs().Contains("--outpost-smoke")) return;
            if (FindFirstObjectByType<DevelopmentSmoke>() != null) return;
            Application.runInBackground = true;
            var obj = new GameObject("Development smoke replay");
            DontDestroyOnLoad(obj); obj.AddComponent<DevelopmentSmoke>();
        }
        IEnumerator Start()
        {
            if (Environment.GetCommandLineArgs().Contains("--outpost-camera")) { yield return CameraReplay(); yield break; }
            bool normal = Environment.GetCommandLineArgs().Contains("--outpost-normal-speed");
            bool research = Environment.GetCommandLineArgs().Contains("--outpost-research");
            string directory = Path.Combine(Application.dataPath, "..", research ? "SmokeResearch" : normal ? "SmokeNormal" : "Smoke");
            Directory.CreateDirectory(directory);
            yield return new WaitForSecondsRealtime(1);
            yield return CaptureLayouts(directory, "menu");
            yield return new WaitForSecondsRealtime(.5f);
            StrategySession.PracticeRequested = true;
            SceneManager.LoadScene("Survival");
            yield return null; yield return null;
            var practice = FindFirstObjectByType<StrategyMatch>();
            var practiceCommander = FindFirstObjectByType<StrategyCommander>();
            yield return CaptureLayouts(directory, "tutorial-select");
            practiceCommander.Select(practice.Entities.First(e=>e.kind==EntityKind.Worker));
            yield return null; yield return CaptureLayouts(directory,"tutorial-gather");
            practice.Deliver(20); practiceCommander.InspectObject(practice.Headquarters.transform); yield return null; yield return CaptureLayouts(directory,"tutorial-build");
            practice.Build(EntityKind.Barracks,new Vector3(-9,0,-11)); yield return null;
            practiceCommander.ClearSelection(); practiceCommander.Select(practice.Entities.First(e=>e.kind==EntityKind.Barracks));
            yield return CaptureLayouts(directory,"tutorial-train");
            practice.Spawn(EntityKind.Soldier,new Vector3(-5,0,-12)); yield return null;
            practiceCommander.ClearSelection(); practiceCommander.Select(practice.Entities.First(e=>e.kind==EntityKind.Soldier));
            yield return CaptureLayouts(directory,"tutorial-order");
            practiceCommander.IssueAttackMove(new Vector3(0,0,-2)); yield return null;
            practiceCommander.InspectObject(practice.Headquarters.transform); yield return null;
            practice.GetComponent<StrategyHud>().canvas.GetComponentInChildren<UnityEngine.UI.ScrollRect>().verticalNormalizedPosition=0;
            yield return CaptureLayouts(directory,"tutorial-research");
            practice.StartResearch(UpgradeKind.Mining); yield return null;
            yield return CaptureLayouts(directory,"research-progress");
            practice.Research.Tick(100); yield return null; yield return CaptureLayouts(directory,"tutorial-complete");
            StrategySession.PracticeRequested = false;
            SceneManager.LoadScene("Survival");
            yield return null; yield return null;
            var match = FindFirstObjectByType<StrategyMatch>();
            var deposits = FindObjectsByType<MineralDeposit>(FindObjectsSortMode.None);
            int workerIndex = 0;
            foreach (var worker in match.Entities.Where(e => e.kind == EntityKind.Worker))
                worker.Gather(deposits[workerIndex++ % deposits.Length]);
            match.Build(EntityKind.Barracks, new Vector3(-9,0,-11));
            match.Build(EntityKind.Turret, new Vector3(7,0,-11));
            var commander = FindFirstObjectByType<StrategyCommander>(); commander.Select(match.Headquarters);
            match.SetPaused(true); yield return CaptureLayouts(directory, "pause"); match.SetPaused(false);
            yield return CaptureLayouts(directory, "hud");
            commander.InspectObject(match.Headquarters.transform); yield return CaptureLayouts(directory,"build");
            commander.BeginPlacement(EntityKind.Turret); yield return CaptureLayouts(directory,"placement"); commander.CancelPlacement();
            commander.InspectObject(match.Headquarters.transform); yield return CaptureLayouts(directory,"research"); commander.ClosePopup();
            Time.timeScale = normal ? 1 : 10;
            float start = Time.realtimeSinceStartup;
            bool captured = false;
            var rallied = new HashSet<StrategyEntity>();
            while (match.Running && Time.realtimeSinceStartup - start < (normal ? 800 : 100))
            {
                var barracks = match.Entities.FirstOrDefault(e => e.kind == EntityKind.Barracks);
                if (barracks != null && !barracks.RallyPoint.HasValue) barracks.SetRallyPoint(new Vector3(0,0,-2));
                foreach (var soldier in match.Entities.Where(e => e.kind == EntityKind.Soldier))
                    if (rallied.Add(soldier)) soldier.AttackMove(new Vector3(-3 + rallied.Count % 3 * 3,0,-2));
                if (research && match.Waves.Wave >= 1 && !match.Research.Active.HasValue)
                    foreach(UpgradeKind upgrade in Enum.GetValues(typeof(UpgradeKind)))
                        if(match.CanResearch(upgrade,out _)) { match.StartResearch(upgrade); break; }
                bool savingForResearch=research && match.Waves.Wave>=1 && !match.Research.Active.HasValue && Enum.GetValues(typeof(UpgradeKind)).Cast<UpgradeKind>().Any(u=>!match.Research.Completed(u));
                if (!savingForResearch && match.Wallet.Minerals >= match.settings.turretCost)
                {
                    bool built = false;
                    for (int z = 5; z >= -24 && !built; z -= 4)
                        for (int x = -18; x <= 18 && !built; x += 4)
                            if (match.CanPlace(EntityKind.Turret, new Vector3(x,0,z), out _)) built = match.Build(EntityKind.Turret,new Vector3(x,0,z));
                }
                if (!savingForResearch && barracks != null && barracks.Production.Count < 2 && match.Wallet.Minerals >= match.settings.soldierCost && match.Entities.Count(e => e.kind == EntityKind.Soldier) < 8) match.Train(barracks);
                if (!captured && match.Waves.Wave >= 2)
                {
                    captured = true;
                    Capture(Path.Combine(directory, "survival.png"));
                    yield return CaptureLayouts(directory,"combat");
                }
                yield return new WaitForSecondsRealtime(.3f);
            }
            yield return null;
            Capture(Path.Combine(directory, "result.png"));
            string result = match.Waves.Result.ToString();
            File.WriteAllText(Path.Combine(directory, "result.txt"), "Result: " + result + "\nWave: " + match.Waves.Wave + "\nMinerals: " + match.Wallet.Minerals + "\nEntities: " + match.Entities.Count + "\nHQ health: " + (match.Headquarters!=null?match.Headquarters.Health.Current:0) + "\nResearch completed: " + string.Join(", ",Enum.GetValues(typeof(UpgradeKind)).Cast<UpgradeKind>().Where(u=>match.Research.Completed(u))));
            yield return new WaitForSecondsRealtime(.5f);
            yield return CaptureLayouts(directory,"victory");
            bool allResearch=Enum.GetValues(typeof(UpgradeKind)).Cast<UpgradeKind>().All(u=>match.Research.Completed(u));
            // Exercise the losing overlay independently after recording the real replay result.
            SceneManager.LoadScene("Survival"); yield return null; yield return null;
            var losingMatch=FindFirstObjectByType<StrategyMatch>(); losingMatch.Headquarters.Damage(100000);
            yield return null; yield return null;
            yield return CaptureLayouts(directory,"defeat");
            Application.Quit(result == "Victory" && (!research || allResearch) ? 0 : 1);
        }
        IEnumerator CameraReplay()
        {
            string directory = Path.Combine(Application.dataPath, "..", "SmokeCamera");
            Directory.CreateDirectory(directory);
            StrategySession.PracticeRequested = false;
            SceneManager.LoadScene("Survival"); yield return null; yield return null;
            var match = FindFirstObjectByType<StrategyMatch>();
            var commander = match.GetComponent<StrategyCommander>();
            var worker = match.Entities.First(e=>e.kind==EntityKind.Worker);
            commander.Select(worker);
            yield return CaptureLayouts(directory,"tactical");
            foreach (var target in new[] { worker.transform, match.Headquarters.transform, FindFirstObjectByType<MineralDeposit>().transform })
            {
                commander.InspectObject(target); yield return new WaitForSecondsRealtime(.5f);
                yield return CaptureLayouts(directory,"inspect-"+target.name);
                commander.ClosePopup(); yield return new WaitForSecondsRealtime(.5f);
            }
            commander.InspectObject(match.Headquarters.transform); yield return null;
            match.GetComponent<StrategyHud>().canvas.GetComponentInChildren<UnityEngine.UI.ScrollRect>().verticalNormalizedPosition=0;
            yield return CaptureLayouts(directory,"hq-research");
            match.Wallet.Deposit(1000); match.StartResearch(UpgradeKind.Mining);
            yield return CaptureLayouts(directory,"hq-research-progress");
            var soldier=match.Spawn(EntityKind.Soldier,new Vector3(-6,0,-12));
            var turret=match.Spawn(EntityKind.Turret,new Vector3(7,0,-11));
            var barracks=match.Spawn(EntityKind.Barracks,new Vector3(-9,0,-11));
            var enemy=match.Spawn(EntityKind.Brute,new Vector3(15,0,10));
            foreach(var entity in new[]{soldier,turret,barracks,enemy})
            {
                commander.InspectObject(entity.transform); yield return CaptureLayouts(directory,"inspect-"+entity.kind);
            }
            commander.InspectObject(worker.transform); commander.Select(soldier);
            yield return CaptureLayouts(directory,"group");
            commander.InspectObject(barracks.transform); commander.BeginRallyPoint();
            yield return CaptureLayouts(directory,"rally-targeting"); commander.CancelInteractions();
            match.SetPaused(true); yield return CaptureLayouts(directory,"pause"); match.SetPaused(false);
            commander.InspectObject(worker.transform);
            yield return CaptureLayouts(directory,"returned");
            File.WriteAllText(Path.Combine(directory,"result.txt"), "Camera replay complete; running="+match.Running+"; selection="+commander.Selection.Count);
            Application.Quit(match.Running && commander.Selection.Contains(worker)?0:1);
        }
        static IEnumerator CaptureLayouts(string directory, string stage)
        {
            foreach (var size in new[] { new Vector2Int(1280,720), new Vector2Int(1920,1080), new Vector2Int(2560,1080) })
            {
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);
                yield return new WaitForSecondsRealtime(.5f);
                Capture(Path.Combine(directory,stage+"-"+size.x+".png"));
            }
            Screen.SetResolution(1280,720,FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(.3f);
        }
        static void Capture(string path)
        {
            var camera=Camera.main;
            var canvas=FindObjectsByType<Canvas>(FindObjectsSortMode.None).FirstOrDefault(c=>c.isActiveAndEnabled && c.isRootCanvas);
            var target=new RenderTexture(Screen.width,Screen.height,24);
            var previous=RenderTexture.active; var previousTarget=camera.targetTexture;
            var mode=canvas.renderMode;
            try
            {
                canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1;
                camera.targetTexture=target; Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active=target;
                foreach(var text in canvas.GetComponentsInChildren<UnityEngine.UI.Text>())
                    if(text.isActiveAndEnabled && text.preferredHeight > text.rectTransform.rect.height + 2)
                        File.AppendAllText(Path.Combine(Path.GetDirectoryName(path),"layout-warnings.txt"),Path.GetFileName(path)+": "+text.text.Replace("\n"," / ")+"\n");
                var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0); pixels.Apply();
                File.WriteAllBytes(path,pixels.EncodeToPNG()); Destroy(pixels);
            }
            finally { camera.targetTexture=previousTarget; RenderTexture.active=previous; canvas.renderMode=mode; target.Release(); Destroy(target); }
        }
    }
}
#endif
