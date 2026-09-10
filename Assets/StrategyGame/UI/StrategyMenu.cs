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
            var bg=StrategyUI.Panel(canvas.transform,"Menu",Vector2.zero,Vector2.one,new Color(.015f,.03f,.05f,.18f));
            var card=StrategyUI.Panel(bg.transform,"Mission briefing",new Vector2(.04f,.08f),new Vector2(.48f,.92f),StrategyUI.Ink);
            StrategyUI.Rule(card.transform,new Vector2(.06f,.9f),new Vector2(.22f,.907f));
            StrategyUI.Label(card.transform,"FRONTIER DEFENSE / SECTOR 07",new Vector2(.04f,.79f),new Vector2(.96f,.89f),21,StrategyUI.Accent);
            StrategyUI.Label(card.transform,"OUTPOST",new Vector2(.035f,.63f),new Vector2(.98f,.8f),70);
            StrategyUI.Label(card.transform,"STRATEGY / SURVIVAL",new Vector2(.04f,.55f),new Vector2(.95f,.64f),24,StrategyUI.Accent);
            StrategyUI.Label(card.transform,"Build your perimeter. Hold the line.\n\nGather minerals, train soldiers, and research\nstronger defenses. Survive five enemy waves.",new Vector2(.04f,.33f),new Vector2(.96f,.55f),23);
            StrategyUI.Button(card.transform,"DEPLOY / Play",new Vector2(.06f,.22f),new Vector2(.94f,.32f),()=>StrategySession.Play());
            StrategyUI.Button(card.transform,"Learn to Play",new Vector2(.06f,.11f),new Vector2(.65f,.21f),()=>StrategySession.Play(true));
            StrategyUI.Button(card.transform,"Quit",new Vector2(.67f,.11f),new Vector2(.94f,.21f),()=>Application.Quit());
            StrategyUI.Label(card.transform,"01 MINE   /   02 FORTIFY   /   03 SURVIVE",new Vector2(.04f,.015f),new Vector2(.96f,.1f),18,StrategyUI.Accent);
            StrategyUI.Label(bg.transform,"FORWARD OPERATING BASE\nSINGLE PLAYER / FIVE WAVES",new Vector2(.64f,.06f),new Vector2(.97f,.17f),22,StrategyUI.Accent);
        }
        void Awake() { if(canvas!=null) { canvas.gameObject.SetActive(false); Destroy(canvas.gameObject); } canvas=null; BuildUI(); StrategyUI.EnsureEventSystem(); }
        void Start() { Time.timeScale=1; Cursor.visible=true; Cursor.lockState=CursorLockMode.None; }
    }
}
