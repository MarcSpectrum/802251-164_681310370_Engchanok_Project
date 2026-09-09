using UnityEngine;
using UnityEngine.SceneManagement;
namespace Engchanok.StrategyGame
{
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
            StrategyUI.Label(bg.transform,"Mine minerals. Command your soldiers.\nBuild a perimeter and hold against five escalating waves.\n\nRunners close fast. Brutes hit hard. Make every order count.",new Vector2(.12f,.32f),new Vector2(.8f,.55f),28);
            StrategyUI.Button(bg.transform,"DEPLOY TO OUTPOST",new Vector2(.12f,.2f),new Vector2(.43f,.29f),()=>SceneManager.LoadScene("Survival"));
            StrategyUI.Button(bg.transform,"Quit",new Vector2(.12f,.1f),new Vector2(.43f,.18f),()=>Application.Quit());
            StrategyUI.Label(bg.transform,"01  MINE     /     02  FORTIFY     /     03  SURVIVE",new Vector2(.53f,.1f),new Vector2(.95f,.2f),22,StrategyUI.Accent);
        }
        void Awake() { if(canvas!=null) { canvas.gameObject.SetActive(false); Destroy(canvas.gameObject); } canvas=null; BuildUI(); StrategyUI.EnsureEventSystem(); }
        void Start() { Time.timeScale=1; Cursor.visible=true; Cursor.lockState=CursorLockMode.None; }
    }
}
