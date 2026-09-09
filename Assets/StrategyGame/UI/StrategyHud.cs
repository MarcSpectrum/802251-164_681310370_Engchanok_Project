using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Engchanok.StrategyGame
{
    public sealed class StrategyHud : MonoBehaviour
    {
        public StrategyMatch match;
        public StrategyCommander commander;
        public Canvas canvas;
        Text resources, wave, notice, selection, queue, resultTitle, resultBody, muteLabel;
        Button train, attack, barracks, turret, resume;
        Image overlay, help, drag;
        readonly Dictionary<StrategyEntity, Image> healthBars = new();
        public void BuildUI()
        {
            if (canvas != null) return;
            canvas=StrategyUI.Canvas(transform);
            var top=StrategyUI.Panel(canvas.transform,"Status",new Vector2(0,.895f),Vector2.one,StrategyUI.Ink);
            StrategyUI.Label(top.transform,"OUTPOST  /  SURVIVAL",new Vector2(0,0),new Vector2(.3f,1),30,StrategyUI.Accent);
            resources=StrategyUI.Label(top.transform,"",new Vector2(.3f,0),new Vector2(.54f,1));
            wave=StrategyUI.Label(top.transform,"",new Vector2(.54f,0),new Vector2(.81f,1));
            StrategyUI.Button(top.transform,"?",new Vector2(.81f,.15f),new Vector2(.86f,.85f),()=>help.gameObject.SetActive(!help.gameObject.activeSelf));
            var mute=StrategyUI.Button(top.transform,"Sound",new Vector2(.86f,.15f),new Vector2(.93f,.85f),()=> { StrategyFeedback.Muted=!StrategyFeedback.Muted; AudioListener.volume=StrategyFeedback.Muted?0:1; });
            muteLabel=mute.GetComponentInChildren<Text>();
            StrategyUI.Button(top.transform,"Pause",new Vector2(.93f,.15f),new Vector2(1,.85f),()=> { if(match.Waves.Result==MatchResult.Playing) match.SetPaused(!match.Paused); });
            var bottom=StrategyUI.Panel(canvas.transform,"Commands",Vector2.zero,new Vector2(1,.215f),StrategyUI.Ink);
            notice=StrategyUI.Label(bottom.transform,"",new Vector2(0,.7f),Vector2.one,23,StrategyUI.Accent);
            selection=StrategyUI.Label(bottom.transform,"",new Vector2(0,.28f),new Vector2(.3f,.7f),26);
            queue=StrategyUI.Label(bottom.transform,"",Vector2.zero,new Vector2(.3f,.28f),19);
            train=StrategyUI.Button(bottom.transform,"Train",new Vector2(.3f,.18f),new Vector2(.47f,.65f),()=> { if(commander.Selection.Count==1) match.Train(commander.Selection[0]); });
            attack=StrategyUI.Button(bottom.transform,"Attack-move [F]",new Vector2(.47f,.18f),new Vector2(.64f,.65f),()=>commander.BeginAttackMove());
            barracks=StrategyUI.Button(bottom.transform,"Barracks",new Vector2(.64f,.18f),new Vector2(.82f,.65f),()=>commander.BeginPlacement(EntityKind.Barracks));
            turret=StrategyUI.Button(bottom.transform,"Turret",new Vector2(.82f,.18f),new Vector2(1,.65f),()=>commander.BeginPlacement(EntityKind.Turret));
            help=StrategyUI.Panel(canvas.transform,"Controls",new Vector2(.27f,.32f),new Vector2(.73f,.8f),StrategyUI.Ink);
            StrategyUI.Label(help.transform,"FIELD MANUAL\n\nLeft-click / drag: select    Shift: add selection\nRight-click: move, attack or gather\nF then click: attack-move soldiers\nSelect barracks + right-click: set rally point\nCtrl+1-9: store group    1-9: recall group\nWASD / arrows: camera    Wheel: zoom\nEsc / right-click: cancel targeting\nEsc: pause / resume",new Vector2(0,.15f),Vector2.one,24);
            StrategyUI.Button(help.transform,"Close",Vector2.zero,new Vector2(1,.15f),()=>help.gameObject.SetActive(false)); help.gameObject.SetActive(false);
            drag=StrategyUI.Panel(canvas.transform,"Selection box",Vector2.zero,Vector2.zero,new Color(.1f,.9f,1,.2f)); drag.raycastTarget=false;
            overlay=StrategyUI.Panel(canvas.transform,"Mission overlay",Vector2.zero,Vector2.one,new Color(.01f,.025f,.045f,.95f));
            resultTitle=StrategyUI.Label(overlay.transform,"",new Vector2(.3f,.62f),new Vector2(.7f,.74f),44,StrategyUI.Accent);
            resultBody=StrategyUI.Label(overlay.transform,"",new Vector2(.3f,.52f),new Vector2(.7f,.62f),24);
            resume=StrategyUI.Button(overlay.transform,"Resume",new Vector2(.3f,.42f),new Vector2(.7f,.5f),()=>match.SetPaused(false));
            StrategyUI.Button(overlay.transform,"Restart mission",new Vector2(.3f,.32f),new Vector2(.7f,.4f),()=>match.Restart());
            StrategyUI.Button(overlay.transform,"Main menu",new Vector2(.3f,.22f),new Vector2(.7f,.3f),()=>match.MainMenu()); overlay.gameObject.SetActive(false);
        }
        void Awake()
        {
            // Generated canvases are recreated so event listeners always bind to this runtime instance.
            if(canvas!=null) { canvas.gameObject.SetActive(false); Destroy(canvas.gameObject); }
            canvas=null; BuildUI(); StrategyUI.EnsureEventSystem();
        }
        void Update()
        {
            if(match.Waves==null) return;
            var hq=match.Headquarters;
            resources.text=$"MINERALS  {match.Wallet.Minerals}\nHQ  {(hq!=null?Mathf.CeilToInt(hq.Health.Current):0)} / {match.settings.headquartersHealth}";
            wave.text=match.Waves.Result!=MatchResult.Playing?$"WAVE {match.Waves.Wave} / {match.Waves.Total}\nMISSION COMPLETE":match.Waves.Active?$"WAVE {match.Waves.Wave} / {match.Waves.Total}\nHOSTILES  {match.HostileCount}":$"PREPARE / WAVE {match.Waves.Wave+1}\nINCOMING IN {Mathf.CeilToInt(match.Waves.Countdown)}s";
            var selected=commander.Selection.Count==1?commander.Selection[0]:null;
            if(selected!=null && !selected.Alive) selected=null;
            selection.text=selected!=null?$"{selected.kind.ToString().ToUpper()}  /  {Mathf.CeilToInt(selected.Health.Current)} HP":$"{commander.Selection.Count} UNITS SELECTED";
            queue.text=selected!=null && selected.Production.Count>0?$"Training: {selected.Production.Count} queued - {Mathf.CeilToInt(selected.Production.Remaining)}s":selected!=null && selected.kind==EntityKind.Barracks?"Right-click ground to set rally":"Ctrl+1-9 store  /  1-9 recall";
            bool producer=selected!=null && (selected.kind==EntityKind.Headquarters || selected.kind==EntityKind.Barracks);
            var kind=selected!=null && selected.kind==EntityKind.Barracks?EntityKind.Soldier:EntityKind.Worker;
            train.GetComponentInChildren<Text>().text=producer?$"Train {kind}\n{match.settings.Cost(kind)} minerals":"Select HQ / barracks\nto train units";
            bool ready=match.Running && !commander.Placement.HasValue && !commander.TargetingAttackMove;
            train.interactable=ready && producer && selected.Production.Count<5 && match.Wallet.Minerals>=match.settings.Cost(kind);
            attack.interactable=ready && commander.Selection.Exists(e=>e!=null && e.Alive && e.kind==EntityKind.Soldier);
            barracks.GetComponentInChildren<Text>().text=$"Build barracks\n{match.settings.barracksCost} minerals";
            turret.GetComponentInChildren<Text>().text=$"Build turret\n{match.settings.turretCost} minerals";
            barracks.interactable=ready && match.Wallet.Minerals>=match.settings.barracksCost;
            turret.interactable=ready && match.Wallet.Minerals>=match.settings.turretCost;
            notice.text=commander.TargetingAttackMove?"ATTACK-MOVE / Click terrain. Esc or right-click to cancel.":commander.Placement.HasValue?$"PLACE {commander.Placement} / {(commander.PlacementValid?"Click to build":commander.PlacementReason)}":!string.IsNullOrEmpty(match.Notice)?match.Notice:producer && selected.Production.Count>=5?"Production queue full (5).":producer && match.Wallet.Minerals<match.settings.Cost(kind)?"More minerals needed to train. Select workers and right-click a deposit.":"Hold the perimeter. Mine, build and command your defenses.";
            muteLabel.text=StrategyFeedback.Muted?"Muted":"Sound";
            overlay.gameObject.SetActive(!match.Running); resume.gameObject.SetActive(match.Paused);
            resultTitle.text=match.Paused?"MISSION PAUSED":match.Waves.Result==MatchResult.Victory?"OUTPOST SECURED":"OUTPOST LOST";
            resultBody.text=match.Paused?"Orders are on hold.":match.Waves.Result==MatchResult.Victory?$"All {match.Waves.Total} waves defeated.":"Headquarters has been destroyed.";
            drag.gameObject.SetActive(commander.Dragging && match.Running);
            if(commander.Dragging)
            {
                var r=drag.rectTransform; r.anchorMin=Vector2.zero; r.anchorMax=Vector2.zero;
                r.offsetMin=Vector2.Min(commander.Pointer,commander.DragStart)/canvas.scaleFactor;
                r.offsetMax=Vector2.Max(commander.Pointer,commander.DragStart)/canvas.scaleFactor;
            }
            UpdateHealthBars();
        }
        void UpdateHealthBars()
        {
            var dead=new List<StrategyEntity>();
            foreach(var pair in healthBars) if(pair.Key==null || !pair.Key.Alive) { Destroy(pair.Value.gameObject); dead.Add(pair.Key); }
            foreach(var e in dead) healthBars.Remove(e);
            foreach(var e in match.Entities)
            {
                if(e==null || !e.Alive) continue;
                if(!healthBars.TryGetValue(e,out var bar))
                {
                    bar=StrategyUI.Panel(canvas.transform,"Health",Vector2.zero,Vector2.zero,Color.black); bar.raycastTarget=false; bar.transform.SetAsFirstSibling();
                    var fill=StrategyUI.Panel(bar.transform,"Fill",Vector2.zero,Vector2.one,e.IsEnemy?new Color(1,.3f,.3f):StrategyUI.Accent); fill.raycastTarget=false;
                    healthBars[e]=bar;
                }
                Vector3 point=commander.view.WorldToScreenPoint(e.transform.position+Vector3.up*(e.IsUnit?2.3f:4));
                bar.gameObject.SetActive(match.Running && point.z>0 && point.y>Screen.height*.215f && point.y<Screen.height*.895f && (e.Selected || e.Health.Current<e.Health.Maximum));
                var r=bar.rectTransform; r.anchoredPosition=new Vector2(point.x,point.y)/canvas.scaleFactor; r.sizeDelta=new Vector2(64,7);
                ((RectTransform)r.GetChild(0)).anchorMax=new Vector2(e.Health.Current/e.Health.Maximum,1);
            }
        }
    }
}
