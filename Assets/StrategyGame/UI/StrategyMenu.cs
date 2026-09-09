using UnityEngine;
using UnityEngine.SceneManagement;
namespace Engchanok.StrategyGame
{
    public static class StrategySession
    {
        public const string TutorialKey = "Outpost.TutorialComplete";
        public static bool PracticeRequested;
        public static void Play(bool tutorial = false)
        {
            PracticeRequested = tutorial || PlayerPrefs.GetInt(TutorialKey, 0) == 0;
            SceneManager.LoadScene("Survival");
        }
    }
    public sealed class StrategyMenu : MonoBehaviour
    {
        public Canvas canvas;
        public void BuildUI()
        {
            if(canvas!=null) return;
            canvas=StrategyUI.Canvas(transform);
            var bg=StrategyUI.Panel(canvas.transform,"Menu",Vector2.zero,Vector2.one,StrategyUI.Ink);
            StrategyUI.Label(bg.transform,"O U T P O S T",new Vector2(.12f,.65f),new Vector2(.85f,.83f),76,StrategyUI.Accent);
            StrategyUI.Label(bg.transform,"STRATEGY / SURVIVAL",new Vector2(.12f,.57f),new Vector2(.8f,.65f),28);
            StrategyUI.Label(bg.transform,"Protect headquarters. Defeat all five waves.\n\nGather minerals, build defenses, and train soldiers.\nInvest in research to strengthen your economy and army.",new Vector2(.12f,.32f),new Vector2(.8f,.55f),28);
            StrategyUI.Button(bg.transform,"Play",new Vector2(.12f,.2f),new Vector2(.43f,.29f),()=>StrategySession.Play());
            StrategyUI.Button(bg.transform,"Learn to Play",new Vector2(.45f,.2f),new Vector2(.76f,.29f),()=>StrategySession.Play(true));
            StrategyUI.Button(bg.transform,"Quit",new Vector2(.12f,.1f),new Vector2(.43f,.18f),()=>Application.Quit());
            StrategyUI.Label(bg.transform,"01  MINE     /     02  FORTIFY     /     03  SURVIVE",new Vector2(.53f,.1f),new Vector2(.95f,.2f),22,StrategyUI.Accent);
        }
        void Awake() { if(canvas!=null) { canvas.gameObject.SetActive(false); Destroy(canvas.gameObject); } canvas=null; BuildUI(); StrategyUI.EnsureEventSystem(); }
        void Start() { Time.timeScale=1; Cursor.visible=true; Cursor.lockState=CursorLockMode.None; }
    }
}
