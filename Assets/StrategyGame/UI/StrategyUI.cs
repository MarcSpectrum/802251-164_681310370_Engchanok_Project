using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
namespace Engchanok.StrategyGame
{
    public static class StrategyUI
    {
        public static readonly Color Paper = new(1f, .965f, .84f, 1f);
        public static readonly Color TextInk = new(.055f, .24f, .30f);
        public static readonly Color ButtonFill = new(.73f, .83f, .63f);
        public static readonly Color SelectedFill = new(.55f, .73f, .48f);
        public static readonly Color Warning = new(.64f, .29f, .17f);
        public static readonly Color Accent = new(.19f, .40f, .31f);
        public static Canvas Canvas(Transform parent)
        {
            var obj = new GameObject("Outpost UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            obj.transform.SetParent(parent, false);
            var canvas = obj.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = obj.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
            return canvas;
        }
        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null) new GameObject("UI input", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }
        public static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false);
            r.anchorMin = min; r.anchorMax = max; r.offsetMin = offsetMin; r.offsetMax = offsetMax; return r;
        }
        public static Image Panel(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var r = Rect(parent,name,min,max,Vector2.zero,Vector2.zero); var i=r.gameObject.AddComponent<ForestPanel>(); i.color=color;
            i.stitched=color==Paper && name!="Action viewport";
            if(i.stitched)
            {
                var shadow=r.gameObject.AddComponent<Shadow>(); shadow.effectColor=new Color(.09f,.19f,.12f,.16f); shadow.effectDistance=new Vector2(0,-4);
            }
            return i;
        }
        public static Text Label(Transform parent, string value, Vector2 min, Vector2 max, int size=24, Color? color=null)
        {
            var r=Rect(parent,value,min,max,new Vector2(16,6),new Vector2(-16,-6)); var t=r.gameObject.AddComponent<Text>();
            t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.text=value; t.fontSize=size; t.color=color ?? TextInk;
            t.supportRichText=true;
            t.alignment=TextAnchor.MiddleLeft; t.raycastTarget=false; return t;
        }
        public static Image Progress(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var track=Panel(parent,name,min,max,new Color(.83f,.85f,.70f)); track.raycastTarget=false;
            var fill=Panel(track.transform,"Progress fill",Vector2.zero,Vector2.one,color); fill.raycastTarget=false;
            return fill;
        }
        public static void SetProgress(Image fill, float value)
        {
            fill.rectTransform.anchorMax=new Vector2(Mathf.Clamp01(value),1);
        }
        public static void Rule(Transform parent, Vector2 min, Vector2 max)
        {
            var line=Panel(parent,"Accent rule",min,max,new Color(.50f,.66f,.44f,.45f)); line.raycastTarget=false;
        }
        public static Button Button(Transform parent,string title,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action)
        {
            var r=Rect(parent,title,min,max,new Vector2(6,6),new Vector2(-6,-6));
            var image=r.gameObject.AddComponent<ForestPanel>(); image.color=ButtonFill; image.cornerRadius=28;
            var edge=Panel(r,"Action edge",new Vector2(.1f,.04f),new Vector2(.9f,.065f),new Color(.4f,.57f,.35f,.3f)); edge.raycastTarget=false;
            var b=r.gameObject.AddComponent<Button>(); b.targetGraphic=image; b.onClick.AddListener(action);
            // A clicked button must not stay selected: the default UI Submit/Navigate bindings (Enter, WASD) would re-trigger or wander from it.
            b.onClick.AddListener(()=>{ if(EventSystem.current!=null) EventSystem.current.SetSelectedGameObject(null); });
            var colors=b.colors; colors.highlightedColor=new Color(.88f,1f,.83f); colors.disabledColor=new Color(.92f,.92f,.88f,1); colors.pressedColor=new Color(.75f,.88f,.68f); colors.selectedColor=colors.normalColor; colors.fadeDuration=.1f; b.colors=colors;
            var label=Label(r,title,Vector2.zero,Vector2.one,22); label.alignment=TextAnchor.MiddleCenter; label.fontStyle=FontStyle.Bold; return b;
        }
    }
}
