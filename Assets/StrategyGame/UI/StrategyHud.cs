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
        RectTransform popup, actionContent;
        ScrollRect actionScroll;
        Button rally;
        string layoutKey;
        Transform positionedTarget;
        Vector2 previousScreenSize;
        readonly List<Button> visibleActions = new();
        public bool PopupVisible => popup != null && popup.gameObject.activeSelf;
        Text headquartersStatus;
        Image headquartersProgress, trainingProgress;
        readonly Image[] researchProgress = new Image[3];
        Text resources, wave, notice, selection, queue, resultTitle, resultBody, muteLabel;
        Button train, attack, barracks, turret, resume, move, gather, tutorialNext;
        Text objective, tutorialText;
        Image tutorialPanel;
        readonly Button[] researchButtons = new Button[3];
        LineRenderer tutorialRing;
        static void ActionLabel(Button button, string title, string detail) { button.GetComponentInChildren<Text>().text = title + "\n" + detail; }
        static void Highlight(Button button, bool on) { button.image.color = on ? new Color(.18f,.48f,.45f) : new Color(.09f,.22f,.29f); }
        Image overlay, help, drag;
        readonly Dictionary<StrategyEntity, Image> healthBars = new();
        public void BuildUI()
        {
            if (canvas != null) return;
            canvas=StrategyUI.Canvas(transform);
            var top=StrategyUI.Panel(canvas.transform,"Status",new Vector2(0,.895f),Vector2.one,StrategyUI.Ink);
            StrategyUI.Label(top.transform,"OUTPOST  /  SURVIVAL",new Vector2(0,0),new Vector2(.2f,1),26,StrategyUI.Accent);
            resources=StrategyUI.Label(top.transform,"",new Vector2(.2f,0),new Vector2(.35f,1),24,new Color(1,.75f,.35f));
            headquartersStatus=StrategyUI.Label(top.transform,"",new Vector2(.35f,.15f),new Vector2(.55f,1),22);
            headquartersProgress=StrategyUI.Progress(top.transform,"HQ integrity",new Vector2(.36f,.12f),new Vector2(.54f,.19f),StrategyUI.Accent);
            StrategyUI.Rule(top.transform,Vector2.zero,new Vector2(1,.018f));
            wave=StrategyUI.Label(top.transform,"",new Vector2(.55f,0),new Vector2(.81f,1));
            StrategyUI.Button(top.transform,"?",new Vector2(.81f,.15f),new Vector2(.86f,.85f),()=>help.gameObject.SetActive(!help.gameObject.activeSelf));
            var mute=StrategyUI.Button(top.transform,"Sound",new Vector2(.86f,.15f),new Vector2(.93f,.85f),()=> { StrategyFeedback.Muted=!StrategyFeedback.Muted; AudioListener.volume=StrategyFeedback.Muted?0:1; });
            muteLabel=mute.GetComponentInChildren<Text>();
            StrategyUI.Button(top.transform,"Pause",new Vector2(.93f,.15f),new Vector2(1,.85f),()=> { if(match.Waves.Result==MatchResult.Playing) match.SetPaused(!match.Paused); });
            notice=StrategyUI.Label(canvas.transform,"",new Vector2(.01f,.015f),new Vector2(.99f,.085f),22,StrategyUI.Accent);
            popup=StrategyUI.Panel(canvas.transform,"Object popup",Vector2.zero,Vector2.zero,StrategyUI.Ink).rectTransform;
            popup.pivot=new Vector2(0,1); popup.sizeDelta=new Vector2(440,500);
            selection=StrategyUI.Label(popup,"",new Vector2(0,1),Vector2.one,23,StrategyUI.Accent);
            SetRow(selection.rectTransform,0,54,354); selection.rectTransform.anchoredPosition=new Vector2(12,0);
            var close=StrategyUI.Button(popup,"X",Vector2.zero,Vector2.one,()=>commander.ClosePopup());
            SetRow((RectTransform)close.transform,4,44,48); ((RectTransform)close.transform).anchoredPosition=new Vector2(384,-4);
            queue=StrategyUI.Label(popup,"",Vector2.zero,Vector2.one,19);
            SetRow(queue.rectTransform,54,88,416); queue.rectTransform.anchoredPosition=new Vector2(12,-54);
            trainingProgress=StrategyUI.Progress(popup,"Training progress",Vector2.zero,Vector2.one,StrategyUI.Accent);
            SetRow((RectTransform)trainingProgress.transform.parent,143,5,416); ((RectTransform)trainingProgress.transform.parent).anchoredPosition=new Vector2(12,-143);
            var viewport=StrategyUI.Panel(popup,"Action viewport",Vector2.zero,Vector2.one,StrategyUI.Ink).rectTransform;
            viewport.offsetMin=new Vector2(8,8); viewport.offsetMax=new Vector2(-24,-158);
            viewport.gameObject.AddComponent<RectMask2D>();
            actionScroll=viewport.gameObject.AddComponent<ScrollRect>();
            actionScroll.viewport=viewport; actionScroll.horizontal=false; actionScroll.movementType=ScrollRect.MovementType.Clamped; actionScroll.scrollSensitivity=30;
            actionContent=StrategyUI.Rect(viewport,"Actions",new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero);
            actionContent.pivot=new Vector2(.5f,1); actionScroll.content=actionContent;
            var scrollTrack=StrategyUI.Panel(popup,"Action scrollbar",new Vector2(1,0),Vector2.one,new Color(.06f,.13f,.18f)).rectTransform;
            scrollTrack.offsetMin=new Vector2(-18,8); scrollTrack.offsetMax=new Vector2(-8,-158);
            var thumb=StrategyUI.Panel(scrollTrack,"Thumb",Vector2.zero,Vector2.one,StrategyUI.Accent);
            var scrollbar=scrollTrack.gameObject.AddComponent<Scrollbar>(); scrollbar.handleRect=thumb.rectTransform;
            scrollbar.targetGraphic=thumb; scrollbar.direction=Scrollbar.Direction.BottomToTop;
            actionScroll.verticalScrollbar=scrollbar; actionScroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            train=PopupAction("Train",()=> { if(commander.Selection.Count==1) match.Train(commander.Selection[0]); });
            move=PopupAction("Move",()=>commander.BeginOrder(UnitOrder.Move));
            gather=PopupAction("Gather",()=>commander.BeginOrder(UnitOrder.Gather));
            attack=PopupAction("Attack-move [F]",()=>commander.BeginAttackMove());
            rally=PopupAction("Set rally point",()=>commander.BeginRallyPoint());
            barracks=PopupAction("Build barracks",()=>commander.BeginPlacement(EntityKind.Barracks));
            turret=PopupAction("Build turret",()=>commander.BeginPlacement(EntityKind.Turret));
            for(int i=0;i<3;i++)
            {
                var upgrade=(UpgradeKind)i;
                researchButtons[i]=PopupAction(StrategySettings.ResearchName(upgrade),()=>match.StartResearch(upgrade));
                researchButtons[i].GetComponentInChildren<Text>().fontSize=19;
                researchProgress[i]=StrategyUI.Progress(researchButtons[i].transform,"Research progress",new Vector2(.04f,.025f),new Vector2(.96f,.06f),StrategyUI.Accent);
            }
            popup.gameObject.SetActive(false);
            objective=StrategyUI.Label(canvas.transform,"Protect headquarters. Defeat five waves.",new Vector2(.01f,.83f),new Vector2(.65f,.895f),23,StrategyUI.Accent);
            tutorialPanel=StrategyUI.Panel(canvas.transform,"Guided practice",new Vector2(.65f,.51f),new Vector2(.99f,.83f),StrategyUI.Ink);
            tutorialText=StrategyUI.Label(tutorialPanel.transform,"",new Vector2(0,.27f),Vector2.one,23);
            tutorialNext=StrategyUI.Button(tutorialPanel.transform,"Skip tutorial / start fresh mission",Vector2.zero,new Vector2(1,.27f),()=>match.FinishPractice());
            tutorialPanel.gameObject.SetActive(false);
            help=StrategyUI.Panel(canvas.transform,"Controls",new Vector2(.25f,.30f),new Vector2(.75f,.82f),StrategyUI.Ink);
            StrategyUI.Label(help.transform,"FIELD MANUAL\n\nLeft-click / drag: select    Shift: add selection\nRight-click: move, attack or gather\nF then click: attack-move soldiers\nSelect barracks + right-click: set rally point\nCtrl+1-9: store group    1-9: recall group\nWASD / arrows: camera    Wheel: zoom\nClick objects: inspect / actions    C: focus    Home: HQ\nEsc / right-click: cancel targeting\nEsc: pause / resume",new Vector2(0,.15f),Vector2.one,20);
            StrategyUI.Button(help.transform,"Close",Vector2.zero,new Vector2(1,.15f),()=>help.gameObject.SetActive(false)); help.gameObject.SetActive(false);
            drag=StrategyUI.Panel(canvas.transform,"Selection box",Vector2.zero,Vector2.zero,new Color(.1f,.9f,1,.2f)); drag.raycastTarget=false;
            overlay=StrategyUI.Panel(canvas.transform,"Mission overlay",Vector2.zero,Vector2.one,new Color(.01f,.025f,.045f,.95f));
            resultTitle=StrategyUI.Label(overlay.transform,"",new Vector2(.3f,.62f),new Vector2(.7f,.74f),44,StrategyUI.Accent);
            resultBody=StrategyUI.Label(overlay.transform,"",new Vector2(.3f,.52f),new Vector2(.7f,.62f),24);
            resume=StrategyUI.Button(overlay.transform,"Resume",new Vector2(.3f,.42f),new Vector2(.7f,.5f),()=>match.SetPaused(false));
            StrategyUI.Button(overlay.transform,"Restart mission",new Vector2(.3f,.32f),new Vector2(.7f,.4f),()=>match.Restart());
            StrategyUI.Button(overlay.transform,"Main menu",new Vector2(.3f,.22f),new Vector2(.7f,.3f),()=>match.MainMenu()); overlay.gameObject.SetActive(false);
        }
        static void SetRow(RectTransform rect, float top, float height, float width)
        {
            rect.anchorMin=rect.anchorMax=new Vector2(0,1); rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(0,-top); rect.sizeDelta=new Vector2(width,height);
        }
        Button PopupAction(string name, UnityEngine.Events.UnityAction action) => StrategyUI.Button(actionContent,name,Vector2.zero,Vector2.one,action);
        void UpdatePopup(StrategyEntity selected)
        {
            bool friendly=selected!=null && !selected.IsEnemy;
            bool hq=friendly && selected.kind==EntityKind.Headquarters;
            bool producer=friendly && (hq || selected.kind==EntityKind.Barracks);
            bool units=commander.Selection.Exists(e=>e!=null && e.Alive && e.IsUnit);
            bool workers=commander.Selection.Exists(e=>e!=null && e.Alive && e.kind==EntityKind.Worker);
            bool soldiers=commander.Selection.Exists(e=>e!=null && e.Alive && e.kind==EntityKind.Soldier);
            visibleActions.Clear();
            void Show(Button button, bool show) { button.gameObject.SetActive(show); if(show) visibleActions.Add(button); }
            Show(train,producer); Show(move,units); Show(gather,workers); Show(attack,soldiers);
            Show(rally,friendly && selected.kind==EntityKind.Barracks);
            Show(barracks,hq); Show(turret,hq);
            foreach(var button in researchButtons) Show(button,hq);
            rally.interactable=match.Running && !commander.Targeting && !commander.Placement.HasValue;
            ActionLabel(rally,"Set rally point","Choose ground for new soldiers");
            float y=0; string key="";
            foreach(var button in visibleActions)
            {
                float row=System.Array.IndexOf(researchButtons,button)>=0?140:86;
                SetRow((RectTransform)button.transform,y,row-6,408); y+=row; key+=button.name+";";
            }
            actionContent.sizeDelta=new Vector2(0,y);
            if(key!=layoutKey) { actionScroll.verticalNormalizedPosition=1; layoutKey=key; }
            float available=Screen.height/canvas.scaleFactor*.72f;
            popup.sizeDelta=new Vector2(440,Mathf.Min(158+y+8,Mathf.Min(650,available)));
            bool active=match.Running && commander.PopupOpen && commander.InspectedObject!=null && !commander.Targeting && !commander.Placement.HasValue;
            popup.gameObject.SetActive(active);
            if(!active) return;
            Vector3 anchor=commander.InspectedObject.position+Vector3.up*2;
            if(commander.Selection.Count>1)
            {
                anchor=Vector3.zero; foreach(var entity in commander.Selection) anchor+=entity.transform.position;
                anchor=anchor/commander.Selection.Count+Vector3.up*2;
            }
            Vector3 point=commander.view.WorldToScreenPoint(anchor);
            if(point.z<=0) { popup.gameObject.SetActive(false); return; }
            Vector2 size=new Vector2(Screen.width,Screen.height)/canvas.scaleFactor;
            bool sameContext=positionedTarget==commander.InspectedObject && previousScreenSize==size;
            positionedTarget=commander.InspectedObject; previousScreenSize=size;
            if(sameContext && RectTransformUtility.RectangleContainsScreenPoint(popup,commander.Pointer)) return;
            float x=point.x/canvas.scaleFactor+26;
            if(x+popup.sizeDelta.x>size.x-12) x=point.x/canvas.scaleFactor-popup.sizeDelta.x-26;
            if(match.Practice && x+popup.sizeDelta.x>size.x*.65f) x=Mathf.Min(x,size.x*.65f-popup.sizeDelta.x-16);
            popup.anchoredPosition=new Vector2(Mathf.Clamp(x,12,Mathf.Max(12,size.x-popup.sizeDelta.x-12)),
                Mathf.Clamp(point.y/canvas.scaleFactor+90,popup.sizeDelta.y+size.y*.09f,size.y*.82f));
        }
        void Awake()
        {
            // Generated canvases are recreated so event listeners always bind to this runtime instance.
            if(canvas!=null) { canvas.gameObject.SetActive(false); Destroy(canvas.gameObject); }
            canvas=null; BuildUI(); StrategyUI.EnsureEventSystem();
            commander.TargetingStarted+=HideForTargeting;
        }
        void HideForTargeting() { popup.gameObject.SetActive(false); }
        void OnDestroy() { if(commander!=null) commander.TargetingStarted-=HideForTargeting; }
        void Update()
        {
            if(match.Waves==null) return;
            var hq=match.Headquarters;
            resources.text=$"MINERALS\n<b>{match.Wallet.Minerals}</b>";
            headquartersStatus.text=$"HQ INTEGRITY\n{(hq!=null?Mathf.CeilToInt(hq.Health.Current):0)} / {match.settings.headquartersHealth}";
            StrategyUI.SetProgress(headquartersProgress,hq!=null?hq.Health.Current/hq.Health.Maximum:0);
            wave.text=match.Practice?"PRACTICE / NO ENEMIES\nLearn at your own pace":match.Waves.Result!=MatchResult.Playing?$"WAVE {match.Waves.Wave} / {match.Waves.Total}\nMISSION COMPLETE":match.Waves.Active?$"WAVE {match.Waves.Wave} / {match.Waves.Total}\nHOSTILES  {match.HostileCount}":$"PREPARE / WAVE {match.Waves.Wave+1}\nINCOMING IN {Mathf.CeilToInt(match.Waves.Countdown)}s";
            var selected=commander.Selection.Count==1?commander.Selection[0]:commander.Selection.Count==0 && commander.InspectedObject!=null?commander.InspectedObject.GetComponent<StrategyEntity>():null;
            if(selected!=null && !selected.Alive) selected=null;
            selection.text=selected!=null?$"{selected.kind.ToString().ToUpper()}  /  {Mathf.CeilToInt(selected.Health.Current)} HP":$"{commander.Selection.Count} UNITS SELECTED";
            queue.text=selected!=null && selected.Production.Count>0?$"Training: {selected.Production.Count} queued - {Mathf.CeilToInt(selected.Production.Remaining)}s":selected!=null && selected.kind==EntityKind.Barracks?"Right-click ground to set rally":selected!=null && selected.kind==EntityKind.Headquarters?"Train workers / Build / Research":selected!=null && selected.kind==EntityKind.Worker?(selected.MiningTarget!=null?"Gathering and delivering":"Choose Gather to start mining")+"\nCarrying "+selected.Cargo+" / "+match.WorkerCapacity:selected!=null && selected.kind==EntityKind.Soldier?"Use Attack-move to engage along a route":"Click an object for details and actions";
            bool producer=selected!=null && (selected.kind==EntityKind.Headquarters || selected.kind==EntityKind.Barracks);
            var kind=selected!=null && selected.kind==EntityKind.Barracks?EntityKind.Soldier:EntityKind.Worker;
            train.GetComponentInChildren<Text>().text=producer?$"Train {kind}\n{match.settings.Cost(kind)} minerals":"Select HQ / barracks\nto train units";
            trainingProgress.transform.parent.gameObject.SetActive(producer && selected.Production.Count>0);
            if(producer) StrategyUI.SetProgress(trainingProgress,1-selected.Production.Remaining/Mathf.Max(.1f,kind==EntityKind.Worker?match.settings.workerTraining:match.settings.soldierTraining));
            bool ready=match.Running && !commander.Placement.HasValue && !commander.Targeting;
            train.interactable=ready && producer && selected.Production.Count<5 && match.Wallet.Minerals>=match.settings.Cost(kind);
            attack.interactable=ready && commander.Selection.Exists(e=>e!=null && e.Alive && e.kind==EntityKind.Soldier);
            barracks.GetComponentInChildren<Text>().text=$"Build barracks\n{match.settings.barracksCost} minerals";
            turret.GetComponentInChildren<Text>().text=$"Build turret\n{match.settings.turretCost} minerals";
            barracks.interactable=ready && match.Wallet.Minerals>=match.settings.barracksCost;
            turret.interactable=ready && match.Wallet.Minerals>=match.settings.turretCost;
            move.interactable=ready && commander.Selection.Exists(e=>e!=null && e.Alive && e.IsUnit);
            gather.interactable=ready && commander.Selection.Exists(e=>e!=null && e.Alive && e.kind==EntityKind.Worker);
            ActionLabel(move,"Move",move.interactable?"Choose destination":"Select units");
            ActionLabel(gather,"Gather",gather.interactable?"Choose minerals":"Select workers");
            ActionLabel(attack,"Attack-move [F]",attack.interactable?"Choose destination":"Select soldiers");
            if(producer) ActionLabel(train,"Train "+kind,selected.Production.Count>=5?"Queue full (5)":match.Wallet.Minerals<match.settings.Cost(kind)?"Need "+(match.settings.Cost(kind)-match.Wallet.Minerals)+" minerals":match.settings.Cost(kind)+" minerals / "+(kind==EntityKind.Worker?match.settings.workerTraining:match.settings.soldierTraining)+"s");
            ActionLabel(barracks,"Build barracks / "+match.settings.barracksCost+" minerals",match.Wallet.Minerals<match.settings.barracksCost?"Need more minerals":"Trains soldiers / Instant");
            ActionLabel(turret,"Build turret / "+match.settings.turretCost+" minerals",match.Wallet.Minerals<match.settings.turretCost?"Need more minerals":"Automatic defense / Instant");
            for(int i=0;i<3;i++)
            {
                var upgrade=(UpgradeKind)i;
                bool eligible=match.CanResearch(upgrade,out var reason);
                researchButtons[i].interactable=ready && eligible;
                float bonus=i==0?match.settings.miningResearchBonus:i==1?match.settings.soldierResearchBonus:match.settings.turretResearchBonus;
                StrategyUI.SetProgress(researchProgress[i],match.Research.Completed(upgrade)?1:match.Research.Active==upgrade?1-match.Research.Remaining/Mathf.Max(.1f,match.settings.ResearchSeconds(upgrade)):0);
                researchButtons[i].image.color=match.Research.Completed(upgrade)?new Color(.12f,.35f,.3f):match.Research.Active==upgrade?new Color(.14f,.32f,.4f):new Color(.09f,.22f,.29f);
                string benefit="+"+Mathf.RoundToInt(bonus*100)+"% "+(i==0?"carrying capacity":"damage");
                string status=match.Research.Active==upgrade?"Researching / "+Mathf.CeilToInt(match.Research.Remaining)+"s left":match.Research.Completed(upgrade)?"Completed":eligible?"Ready to research":reason.Replace("Research already in progress","Another project active");
                ActionLabel(researchButtons[i],StrategySettings.ResearchName(upgrade),benefit+"\n"+match.settings.ResearchCost(upgrade)+" minerals / "+match.settings.ResearchSeconds(upgrade)+"s\n"+status);
            }
            if(match.Research.Active.HasValue && (selected==null || selected.kind!=EntityKind.Worker)) queue.text+="\n"+StrategySettings.ResearchName(match.Research.Active.Value)+": "+Mathf.CeilToInt(match.Research.Remaining)+"s left";
            var next=match.settings.Composition(Mathf.Min(match.Waves.Wave+1,match.Waves.Total));
            objective.gameObject.SetActive(match.Waves.Result==MatchResult.Playing);
            objective.text=match.Practice?"GUIDED PRACTICE / No enemy waves":match.Waves.Active?"Protect headquarters / Defeat the remaining enemies":"Next wave: "+next.standard+" standard / "+next.runners+" runners / "+next.brutes+" brutes";
            tutorialPanel.gameObject.SetActive(match.Practice && match.Running);
            if(match.Practice)
            {
                int step=match.TutorialStep;
                string[] instructions={"1 / 6  SELECT A WORKER\nClick an orange worker near headquarters.","2 / 6  GATHER MINERALS\nClick Gather, then a teal deposit. Wait for a worker to bring minerals home.","3 / 6  BUILD A BARRACKS\nClick HQ, then Build barracks. Place it inside the ring, away from the approach lanes.","4 / 6  TRAIN A SOLDIER\nSelect your barracks, then Train Soldier. Wait for training.","5 / 6  COMMAND YOUR SOLDIER\nSelect a soldier. Click Attack-move, then choose ground ahead of headquarters.","6 / 6  INVEST IN RESEARCH\nClick HQ, scroll down, and choose research. Wait for completion; its benefit applies to current and future units.","READY TO DEPLOY\nPractice complete! Your mission starts fresh with normal resources and no upgrades."};
                if(tutorialRing==null) tutorialRing=StrategyFeedback.Ring(transform,match.beamMaterial,2,StrategyUI.Accent);
                var target=step==0?match.Entities.Find(e=>e.kind==EntityKind.Worker):step==3?match.Entities.Find(e=>e.kind==EntityKind.Barracks):step==4?match.Entities.Find(e=>e.kind==EntityKind.Soldier):step==2 || step==5?match.Headquarters:null;
                var deposit=step==1?System.Array.Find(Object.FindObjectsByType<MineralDeposit>(FindObjectsSortMode.None),d=>d.transform.position.x<0 && d.transform.position.z<-12):null;
                tutorialRing.gameObject.SetActive(match.Running && step<6);
                tutorialRing.transform.position=target!=null?target.transform.position:deposit!=null?deposit.transform.position:step==2?new Vector3(-9,0,-11):new Vector3(0,0,-2);
                tutorialText.text=instructions[Mathf.Min(step,6)]+(step<6?"\nPractice progress resets when you deploy.":"");
                ActionLabel(tutorialNext,step>=6?"Start fresh mission":"Skip tutorial","Normal resources / five waves");
                for(int i=0;i<3;i++) Highlight(researchButtons[i],step==5 && !match.Research.Active.HasValue && !match.Research.Completed((UpgradeKind)i));
                Highlight(gather,step==1); Highlight(barracks,step==2); Highlight(train,step==3); Highlight(attack,step==4);
            }
            notice.text=commander.TargetingRally?"RALLY / Click reachable ground. Esc or right-click cancels.":commander.TargetingOrder.HasValue?(commander.TargetingOrder==UnitOrder.Gather?"GATHER / Click a teal deposit. Esc or right-click cancels.":"MOVE / Click ground. Esc or right-click cancels."):commander.TargetingAttackMove?"ATTACK-MOVE / Click terrain. Esc or right-click to cancel.":commander.Placement.HasValue?$"PLACE {commander.Placement} / {(commander.PlacementValid?"Click to build":commander.PlacementReason)}":!string.IsNullOrEmpty(match.Notice)?match.Notice:producer && selected.Production.Count>=5?"Production queue full (5).":producer && match.Wallet.Minerals<match.settings.Cost(kind)?"More minerals needed to train. Select workers and right-click a deposit.":"Hold the perimeter. Mine, build and command your defenses.";
            muteLabel.text=StrategyFeedback.Muted?"Muted":"Sound";
            overlay.gameObject.SetActive(match.Paused || match.Waves.Result!=MatchResult.Playing); resume.gameObject.SetActive(match.Paused);
            resultTitle.text=match.Paused?"MISSION PAUSED":match.Waves.Result==MatchResult.Victory?"OUTPOST SECURED":"OUTPOST LOST";
            resultBody.text=match.Paused?"Orders are on hold.":match.Waves.Result==MatchResult.Victory?$"All {match.Waves.Total} waves defeated.":"Headquarters has been destroyed.";
            drag.gameObject.SetActive(commander.Dragging && match.Running);
            if(commander.Dragging)
            {
                var r=drag.rectTransform; r.anchorMin=Vector2.zero; r.anchorMax=Vector2.zero;
                r.offsetMin=Vector2.Min(commander.Pointer,commander.DragStart)/canvas.scaleFactor;
                r.offsetMax=Vector2.Max(commander.Pointer,commander.DragStart)/canvas.scaleFactor;
            }
            if(commander.Selection.Count>1)
            {
                int workers=commander.Selection.FindAll(e=>e!=null && e.kind==EntityKind.Worker).Count;
                int soldiers=commander.Selection.FindAll(e=>e!=null && e.kind==EntityKind.Soldier).Count;
                queue.text=workers+" workers / "+soldiers+" soldiers\nCommands affect eligible units.";
            }
            else if(selected!=null)
            {
                if(selected.IsEnemy) queue.text="Hostile / "+selected.kind+"\nAttacks the outpost and nearby defenders.";
                else if(selected.kind==EntityKind.Turret) queue.text="Automatic defense\nDamage: "+match.CombatDamage(selected.kind);
                else if(selected.IsUnit) queue.text="Order: "+selected.Order+(selected.kind==EntityKind.Worker?"\nCarrying "+selected.Cargo+" / "+match.WorkerCapacity:"\nDamage: "+match.CombatDamage(selected.kind));
            }
            else if(commander.InspectedObject!=null && commander.InspectedObject.TryGetComponent<MineralDeposit>(out var mineral))
            {
                selection.text="MINERAL DEPOSIT";
                queue.text=(mineral.Stock!=null?mineral.Stock.Remaining:0)+" minerals remaining\nAssign workers with Gather or right-click.";
            }
            UpdateHealthBars();
        }
        void LateUpdate()
        {
            if(match.Waves==null) return;
            var selected=commander.Selection.Count==1?commander.Selection[0]:commander.Selection.Count==0 && commander.InspectedObject!=null?commander.InspectedObject.GetComponent<StrategyEntity>():null;
            if(selected!=null && !selected.Alive) selected=null;
            UpdatePopup(selected);
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
                bar.gameObject.SetActive(match.Running && point.z>0 && point.y>Screen.height*.085f && point.y<Screen.height*.895f && (e.Selected || e.Health.Current<e.Health.Maximum));
                var r=bar.rectTransform; r.anchoredPosition=new Vector2(point.x,point.y)/canvas.scaleFactor; r.sizeDelta=new Vector2(64,7);
                ((RectTransform)r.GetChild(0)).anchorMax=new Vector2(e.Health.Current/e.Health.Maximum,1);
            }
        }
    }
}
