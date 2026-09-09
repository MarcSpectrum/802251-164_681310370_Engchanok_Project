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
            bool normal = Environment.GetCommandLineArgs().Contains("--outpost-normal-speed");
            string directory = Path.Combine(Application.dataPath, "..", normal ? "SmokeNormal" : "Smoke");
            Directory.CreateDirectory(directory);
            yield return new WaitForSecondsRealtime(1);
            yield return CaptureLayouts(directory, "menu");
            yield return new WaitForSecondsRealtime(.5f);
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
                if (match.Wallet.Minerals >= match.settings.turretCost)
                {
                    bool built = false;
                    for (int z = 5; z >= -24 && !built; z -= 4)
                        for (int x = -18; x <= 18 && !built; x += 4)
                            if (match.CanPlace(EntityKind.Turret, new Vector3(x,0,z), out _)) built = match.Build(EntityKind.Turret,new Vector3(x,0,z));
                }
                if (barracks != null && barracks.Production.Count < 2 && match.Wallet.Minerals >= match.settings.soldierCost && match.Entities.Count(e => e.kind == EntityKind.Soldier) < 8) match.Train(barracks);
                if (!captured && match.Waves.Wave >= 2)
                {
                    captured = true;
                    Capture(Path.Combine(directory, "survival.png"));
                }
                yield return new WaitForSecondsRealtime(.3f);
            }
            yield return null;
            Capture(Path.Combine(directory, "result.png"));
            string result = match.Waves.Result.ToString();
            File.WriteAllText(Path.Combine(directory, "result.txt"), "Result: " + result + "\nWave: " + match.Waves.Wave + "\nMinerals: " + match.Wallet.Minerals + "\nEntities: " + match.Entities.Count);
            yield return new WaitForSecondsRealtime(.5f);
            Application.Quit(result == "Victory" ? 0 : 1);
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
                var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0); pixels.Apply();
                File.WriteAllBytes(path,pixels.EncodeToPNG()); Destroy(pixels);
            }
            finally { camera.targetTexture=previousTarget; RenderTexture.active=previous; canvas.renderMode=mode; target.Release(); Destroy(target); }
        }
    }
}
#endif
