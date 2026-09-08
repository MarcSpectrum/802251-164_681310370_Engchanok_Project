using UnityEngine;
using UnityEngine.SceneManagement;
namespace Engchanok.StrategyGame
{
    public sealed class StrategyMenu : MonoBehaviour
    {
        void Start() { Time.timeScale = 1; Cursor.visible = true; Cursor.lockState = CursorLockMode.None; }
        void OnGUI()
        {
            float x = Screen.width * .12f, y = Screen.height * .26f;
            var title = new GUIStyle(GUI.skin.label) { fontSize = 44, fontStyle = FontStyle.Bold };
            var body = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
            GUI.Label(new Rect(x, y, 700, 70), "OUTPOST", title);
            GUI.Label(new Rect(x + 3, y + 70, 600, 35), "A SCI-FI STRATEGY SURVIVAL PROTOTYPE", body);
            GUI.Label(new Rect(x + 3, y + 125, 570, 105), "Mine minerals. Build barracks and turrets. Command your soldiers. Protect headquarters through five enemy waves.", body);
            if (GUI.Button(new Rect(x, y + 245, 280, 52), "DEPLOY TO OUTPOST", new GUIStyle(GUI.skin.button) { fontSize = 20 })) SceneManager.LoadScene("Survival");
            if (GUI.Button(new Rect(x, y + 312, 280, 38), "Quit")) Application.Quit();
        }
    }
}
