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
            var title=StrategyUI.Label(canvas.transform,"OUTPOST",new Vector2(.065f,.66f),new Vector2(.47f,.88f),108,StrategyUI.Paper);
            title.fontStyle=FontStyle.BoldAndItalic;
            title.rectTransform.localRotation=Quaternion.Euler(0,0,5);
            var shadow=title.gameObject.AddComponent<UnityEngine.UI.Shadow>(); shadow.effectColor=new Color(.12f,.28f,.18f,.3f); shadow.effectDistance=new Vector2(2,-5);
            var subtitle=StrategyUI.Label(canvas.transform,"A woodland survival story",new Vector2(.09f,.61f),new Vector2(.46f,.68f),26,StrategyUI.Paper);
            subtitle.fontStyle=FontStyle.Bold;
            var card=StrategyUI.Panel(canvas.transform,"Mission briefing",new Vector2(.10f,.20f),new Vector2(.38f,.55f),StrategyUI.Paper);
            card.rectTransform.localRotation=Quaternion.Euler(0,0,4);
            var play=StrategyUI.Button(card.transform,"Let's play",new Vector2(.09f,.60f),new Vector2(.91f,.84f),()=>StrategySession.Play());
            var learn=StrategyUI.Button(card.transform,"Learn to play",new Vector2(.09f,.36f),new Vector2(.91f,.60f),()=>StrategySession.Play(true));
            var quit=StrategyUI.Button(card.transform,"Exit",new Vector2(.09f,.12f),new Vector2(.91f,.36f),()=>Application.Quit());
            foreach(var button in new[]{play,learn,quit})
            {
                button.GetComponentInChildren<UnityEngine.UI.Text>().fontSize=32;
                button.transform.Find("Action edge").gameObject.SetActive(false);
                if(button!=play) button.image.color=StrategyUI.Paper;
            }
            var hint=StrategyUI.Panel(canvas.transform,"Selection hint",new Vector2(.77f,.055f),new Vector2(.96f,.12f),StrategyUI.ButtonFill);
            hint.raycastTarget=false;
            var hintText=StrategyUI.Label(hint.transform,"Click to select",Vector2.zero,Vector2.one,22);
            hintText.alignment=TextAnchor.MiddleCenter; hintText.fontStyle=FontStyle.Bold;
        }
        void Awake() { if(canvas!=null) { canvas.gameObject.SetActive(false); Destroy(canvas.gameObject); } canvas=null; BuildUI(); StrategyUI.EnsureEventSystem(); }
        void Start() { Time.timeScale=1; Cursor.visible=true; Cursor.lockState=CursorLockMode.None; }
    }
}
