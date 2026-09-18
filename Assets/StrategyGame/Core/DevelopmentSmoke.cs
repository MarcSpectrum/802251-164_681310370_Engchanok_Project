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
            // Captures here run at normal speed and eat into the preparation phase, so new ones belong in the fresh
            // mission at the end; adding them here shifts the replay's whole timeline.
            Time.timeScale = normal ? 1 : 10;
            float start = Time.realtimeSinceStartup;
            int startFrame = Time.frameCount;
            bool captured = false;
            var rallied = new HashSet<StrategyEntity>();
            while (match.Running && Time.realtimeSinceStartup - start < (normal ? 800 : 100))
            {
                foreach (var producer in match.Entities.Where(e => e != null && e.Alive && match.settings.IsProducer(e.kind) && e.kind != EntityKind.Headquarters))
                    if (!producer.RallyPoint.HasValue) producer.SetRallyPoint(new Vector3(0,0,-2));
                foreach (var troop in match.Entities.Where(e => e != null && e.Alive && e.IsUnit && !e.IsEnemy && e.kind != EntityKind.Worker))
                    if (rallied.Add(troop)) troop.AttackMove(new Vector3(-3 + rallied.Count % 3 * 3,0,-2));
                if (research && match.Waves.Wave >= 1 && !match.Research.Active.HasValue)
                    foreach(UpgradeKind upgrade in Enum.GetValues(typeof(UpgradeKind)))
                        if(match.CanResearch(upgrade,out _)) { match.StartResearch(upgrade); break; }
                bool savingForResearch=research && match.Waves.Wave>=1 && !match.Research.Active.HasValue && Enum.GetValues(typeof(UpgradeKind)).Cast<UpgradeKind>().Any(u=>!match.Research.Completed(u));
                // Supply comes before more defences, otherwise the replay stalls with minerals it cannot convert into an army.
                if (!savingForResearch && match.Supply.Free < 4 && match.Wallet.Minerals >= match.settings.relayCost)
                {
                    bool built = false;
                    for (int z = 5; z >= -24 && !built; z -= 4)
                        for (int x = -18; x <= 18 && !built; x += 4)
                            if (match.CanPlace(EntityKind.SupplyRelay, new Vector3(x,0,z), out _)) built = match.Build(EntityKind.SupplyRelay,new Vector3(x,0,z));
                }
                // Stand up the whole production line before stacking turrets, so the replay exercises every unit kind.
                foreach (var structure in new[] { EntityKind.RangerPost, EntityKind.SupportBay, EntityKind.Turret })
                {
                    if (savingForResearch || match.Wallet.Minerals < match.settings.Cost(structure)) continue;
                    if (structure != EntityKind.Turret && match.Entities.Any(e => e != null && e.Alive && e.kind == structure)) continue;
                    bool built = false;
                    for (int z = 5; z >= -24 && !built; z -= 4)
                        for (int x = -18; x <= 18 && !built; x += 4)
                            if (match.CanPlace(structure, new Vector3(x,0,z), out _)) built = match.Build(structure,new Vector3(x,0,z));
                    if (built) break;
                }
                if (!savingForResearch)
                    foreach (var unit in new[] { EntityKind.Soldier, EntityKind.Defender, EntityKind.Ranger, EntityKind.Medic, EntityKind.Engineer })
                    {
                        var profile = match.settings.Profile(unit);
                        // The hostile roster hits harder than the replay's old four-of-each army could hold, and defenders
                        // are the screen that keeps rangers and support alive, so the front line is deepened first.
                        int want = unit == EntityKind.Medic || unit == EntityKind.Engineer ? 2 : unit == EntityKind.Defender ? 6 : 5;
                        if (profile == null || match.Wallet.Minerals < profile.cost || match.Entities.Count(e => e != null && e.Alive && e.kind == unit) >= want) continue;
                        var source = match.Entities.FirstOrDefault(e => e != null && e.Alive && e.kind == profile.producer && e.Production.Count < 2);
                        if (source != null && match.Train(source, unit)) break;
                    }
                if (!captured && match.Waves.Wave >= 2)
                {
                    captured = true;
                    Capture(Path.Combine(directory, "survival.png"));
                    yield return CaptureLayouts(directory,"combat");
                }
                yield return new WaitForSecondsRealtime(.3f);
            }
            // Frames per real second across the loop, captures included. At 10x speed a lower rate means coarser simulation steps.
            float replayFps = (Time.frameCount - startFrame) / Mathf.Max(.01f, Time.realtimeSinceStartup - start);
            yield return null;
            Capture(Path.Combine(directory, "result.png"));
            string result = match.Waves.Result.ToString();
            File.WriteAllText(Path.Combine(directory, "result.txt"), "Result: " + result + "\nWave: " + match.Waves.Wave + "\nMinerals: " + match.Wallet.Minerals + "\nEntities: " + match.Entities.Count + "\nHQ health: " + (match.Headquarters!=null?match.Headquarters.Health.Current:0) + "\nResearch completed: " + string.Join(", ",Enum.GetValues(typeof(UpgradeKind)).Cast<UpgradeKind>().Where(u=>match.Research.Completed(u)))
                + "\nGrade: " + MatchStats.Grade(match.Waves.Result, match.Headquarters!=null?match.Headquarters.Health.Current/match.settings.headquartersHealth:0, match.Stats.UnitsLost, match.Stats.UnitsTrained)
                + "\nMission time: " + Mathf.RoundToInt(match.Stats.Elapsed) + "s\nMinerals mined / spent: " + match.DeliveredMinerals + " / " + match.Wallet.Spent
                + "\nUnits trained / lost: " + match.Stats.UnitsTrained + " / " + match.Stats.UnitsLost + "\nStructures built / lost: " + match.Stats.StructuresBuilt + " / " + match.Stats.StructuresLost
                + "\nHostiles defeated: " + match.Stats.HostilesDefeated + "\nReplay FPS: " + Mathf.RoundToInt(replayFps));
            yield return new WaitForSecondsRealtime(.5f);
            yield return CaptureLayouts(directory,"victory");
            bool allResearch=Enum.GetValues(typeof(UpgradeKind)).Cast<UpgradeKind>().All(u=>match.Research.Completed(u));
            // Exercise the losing overlay independently after recording the real replay result.
            SceneManager.LoadScene("Survival"); yield return null; yield return null;
            var losingMatch=FindFirstObjectByType<StrategyMatch>();
            // A one-point scratch raises the attack alert, so the minimap ring and notice are captured.
            losingMatch.Entities.First(e => e.kind == EntityKind.Worker).Damage(1); yield return null;
            yield return CaptureLayouts(directory,"alert");
            // The field manual is only visible on demand, so capture it explicitly for the overflow check.
            var manual = losingMatch.GetComponent<StrategyHud>().canvas.transform.Find("Controls").gameObject;
            manual.SetActive(true); yield return CaptureLayouts(directory,"manual"); manual.SetActive(false);
            // Commander powers: the aiming notice and preview, then the bar recharging with the jet mid-run.
            var losingCommander = losingMatch.GetComponent<StrategyCommander>();
            losingMatch.Deliver(400);
            losingCommander.BeginPower(PowerKind.Barrage); yield return CaptureLayouts(directory,"power-aim"); losingCommander.CancelInteractions();
            losingMatch.UsePower(PowerKind.Airstrike, new Vector3(0,0,4)); yield return CaptureLayouts(directory,"powers");
            losingMatch.Headquarters.Damage(100000);
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
                if(entity==null) continue;
                commander.InspectObject(entity.transform); yield return CaptureLayouts(directory,"inspect-"+entity.kind);
            }
            // One capture per hostile archetype, so the new popup lines are reviewed at every supported resolution.
            // Each is spawned immediately before its own capture and removed after: standing inside the outpost, a hostile
            // does not survive the three resolution passes of the capture ahead of it.
            foreach(var kind in new[]{EntityKind.Lancer,EntityKind.Breaker,EntityKind.Warden,EntityKind.Juggernaut})
            {
                var hostile=match.Spawn(kind,new Vector3(18,0,26));
                if(hostile==null) continue;
                commander.InspectObject(hostile.transform);
                yield return CaptureLayouts(directory,"inspect-"+kind);
                if(hostile!=null) Destroy(hostile.gameObject);
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
