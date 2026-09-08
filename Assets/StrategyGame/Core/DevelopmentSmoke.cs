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
            string directory = Path.Combine(Application.dataPath, "..", "Smoke");
            Directory.CreateDirectory(directory);
            yield return new WaitForSecondsRealtime(1);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "menu.png"));
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
            Time.timeScale = 10;
            float start = Time.realtimeSinceStartup;
            bool captured = false;
            var rallied = new HashSet<StrategyEntity>();
            while (match.Running && Time.realtimeSinceStartup - start < 80)
            {
                var barracks = match.Entities.FirstOrDefault(e => e.kind == EntityKind.Barracks);
                foreach (var soldier in match.Entities.Where(e => e.kind == EntityKind.Soldier))
                    if (rallied.Add(soldier)) soldier.Move(new Vector3(-3 + rallied.Count % 3 * 3,0,-2));
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
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, "survival.png"));
                }
                yield return new WaitForSecondsRealtime(.3f);
            }
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "result.png"));
            string result = match.Waves.Result.ToString();
            File.WriteAllText(Path.Combine(directory, "result.txt"), "Result: " + result + "\nWave: " + match.Waves.Wave + "\nMinerals: " + match.Wallet.Minerals + "\nEntities: " + match.Entities.Count);
            yield return new WaitForSecondsRealtime(.5f);
            Application.Quit(result == "Victory" ? 0 : 1);
        }
    }
}
#endif
