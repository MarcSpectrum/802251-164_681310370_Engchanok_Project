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
        Text headquartersStatus;
        Image headquartersProgress, trainingProgress;
        readonly Image[] researchProgress = new Image[3];
        Text resources, wave, notice, selection, queue, resultTitle, resultBody, muteLabel;
        Button train, attack, barracks, turret, resume, move, gather, tutorialNext;
        Text objective, tutorialText;
        Image tutorialPanel;
        readonly GameObject[] pages = new GameObject[3];
        readonly Button[] tabs = new Button[3], researchButtons = new Button[3];
        int lastTutorialStep = -1;
        LineRenderer tutorialRing;
        public void ShowPage(int page) { for (int i=0;i<3;i++) { pages[i].SetActive(i==page); tabs[i].GetComponentInChildren<Text>().text = new[]{"Orders","Build","Research"}[i]+(i==page?" / open":""); tabs[i].image.color = i==page ? new Color(.12f,.42f,.46f) : new Color(.09f,.22f,.29f); } }
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
            var bottom=StrategyUI.Panel(canvas.transform,"Commands",Vector2.zero,new Vector2(1,.25f),StrategyUI.Ink);
            notice=StrategyUI.Label(bottom.transform,"",new Vector2(.27f,.75f),Vector2.one,22,StrategyUI.Accent);
            selection=StrategyUI.Label(bottom.transform,"",new Vector2(0,.42f),new Vector2(.27f,1),26);
            queue=StrategyUI.Label(bottom.transform,"",new Vector2(0,.08f),new Vector2(.27f,.44f),20);
            trainingProgress=StrategyUI.Progress(bottom.transform,"Training progress",new Vector2(.012f,.035f),new Vector2(.255f,.06f),StrategyUI.Accent);
            StrategyUI.Rule(bottom.transform,new Vector2(0,.99f),Vector2.one);
            for (int i=0;i<3;i++)
            {
                int page=i;
                tabs[i]=StrategyUI.Button(bottom.transform,new[]{"Orders","Build","Research"}[i],new Vector2(.28f+i*.24f,.52f),new Vector2(.52f+i*.24f,.75f),()=>ShowPage(page));
                pages[i]=StrategyUI.Panel(bottom.transform,"Page "+i,new Vector2(.28f,0),new Vector2(1,.52f),StrategyUI.Ink).gameObject;
            }
            train=StrategyUI.Button(pages[0].transform,"Train",Vector2.zero,new Vector2(.25f,1),()=> { if(commander.Selection.Count==1) match.Train(commander.Selection[0]); });
            move=StrategyUI.Button(pages[0].transform,"Move",new Vector2(.25f,0),new Vector2(.5f,1),()=>commander.BeginOrder(UnitOrder.Move));
            gather=StrategyUI.Button(pages[0].transform,"Gather",new Vector2(.5f,0),new Vector2(.75f,1),()=>commander.BeginOrder(UnitOrder.Gather));
            attack=StrategyUI.Button(pages[0].transform,"Attack-move [F]",new Vector2(.75f,0),Vector2.one,()=>commander.BeginAttackMove());
            barracks=StrategyUI.Button(pages[1].transform,"Barracks",Vector2.zero,new Vector2(.5f,1),()=>commander.BeginPlacement(EntityKind.Barracks));
            turret=StrategyUI.Button(pages[1].transform,"Turret",new Vector2(.5f,0),Vector2.one,()=>commander.BeginPlacement(EntityKind.Turret));
            for(int i=0;i<3;i++)
            {
                var upgrade=(UpgradeKind)i;
                researchButtons[i]=StrategyUI.Button(pages[2].transform,StrategySettings.ResearchName(upgrade),new Vector2(i/3f,0),new Vector2((i+1)/3f,1),()=>match.StartResearch(upgrade));
                researchButtons[i].GetComponentInChildren<Text>().fontSize=20;
                researchProgress[i]=StrategyUI.Progress(researchButtons[i].transform,"Research progress",new Vector2(.04f,.025f),new Vector2(.96f,.06f),StrategyUI.Accent);
            }
            objective=StrategyUI.Label(canvas.transform,"Protect headquarters. Defeat five waves.",new Vector2(.01f,.83f),new Vector2(.65f,.895f),23,StrategyUI.Accent);
            tutorialPanel=StrategyUI.Panel(canvas.transform,"Guided practice",new Vector2(.65f,.51f),new Vector2(.99f,.83f),StrategyUI.Ink);
            tutorialText=StrategyUI.Label(tutorialPanel.transform,"",new Vector2(0,.27f),Vector2.one,23);
            tutorialNext=StrategyUI.Button(tutorialPanel.transform,"Skip tutorial / start fresh mission",Vector2.zero,new Vector2(1,.27f),()=>match.FinishPractice());
            tutorialPanel.gameObject.SetActive(false); ShowPage(0);
            help=StrategyUI.Panel(canvas.transform,"Controls",new Vector2(.25f,.30f),new Vector2(.75f,.82f),StrategyUI.Ink);
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
            resources.text=$"MINERALS\n<b>{match.Wallet.Minerals}</b>";
            headquartersStatus.text=$"HQ INTEGRITY\n{(hq!=null?Mathf.CeilToInt(hq.Health.Current):0)} / {match.settings.headquartersHealth}";
            StrategyUI.SetProgress(headquartersProgress,hq!=null?hq.Health.Current/hq.Health.Maximum:0);
            wave.text=match.Practice?"PRACTICE / NO ENEMIES\nLearn at your own pace":match.Waves.Result!=MatchResult.Playing?$"WAVE {match.Waves.Wave} / {match.Waves.Total}\nMISSION COMPLETE":match.Waves.Active?$"WAVE {match.Waves.Wave} / {match.Waves.Total}\nHOSTILES  {match.HostileCount}":$"PREPARE / WAVE {match.Waves.Wave+1}\nINCOMING IN {Mathf.CeilToInt(match.Waves.Countdown)}s";
            var selected=commander.Selection.Count==1?commander.Selection[0]:null;
            if(selected!=null && !selected.Alive) selected=null;
            selection.text=selected!=null?$"{selected.kind.ToString().ToUpper()}  /  {Mathf.CeilToInt(selected.Health.Current)} HP":$"{commander.Selection.Count} UNITS SELECTED";
            queue.text=selected!=null && selected.Production.Count>0?$"Training: {selected.Production.Count} queued - {Mathf.CeilToInt(selected.Production.Remaining)}s":selected!=null && selected.kind==EntityKind.Barracks?"Right-click ground to set rally":selected!=null && selected.kind==EntityKind.Headquarters?"Train workers in Orders / Improve forces in Research":selected!=null && selected.kind==EntityKind.Worker?(selected.MiningTarget!=null?"Gathering and delivering":"Choose Gather to start mining")+"\nCarrying "+selected.Cargo+" / "+match.WorkerCapacity:selected!=null && selected.kind==EntityKind.Soldier?"Use Attack-move to engage along a route":"Click a unit or building for its actions";
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
            ActionLabel(barracks,"Build barracks / "+match.settings.barracksCost+" minerals","Trains soldiers"+(match.Wallet.Minerals<match.settings.barracksCost?" / Need more minerals":" / Instant"));
            ActionLabel(turret,"Build turret / "+match.settings.turretCost+" minerals","Automatic defense"+(match.Wallet.Minerals<match.settings.turretCost?" / Need more minerals":" / Instant"));
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
                if(step!=lastTutorialStep) { ShowPage(step==2?1:step==5?2:0); lastTutorialStep=step; }
                string[] instructions={"1 / 6  SELECT A WORKER\nClick an orange worker near headquarters.","2 / 6  GATHER MINERALS\nClick Gather, then a teal deposit. Wait for a worker to bring minerals home.","3 / 6  BUILD A BARRACKS\nOpen Build. Place barracks inside the ring, away from the approach lanes.","4 / 6  TRAIN A SOLDIER\nSelect your barracks, open Orders, then Train Soldier. Wait for training.","5 / 6  COMMAND YOUR SOLDIER\nSelect a soldier. Click Attack-move, then choose ground ahead of headquarters.","6 / 6  INVEST IN RESEARCH\nChoose a research project. Wait for completion; its benefit applies to current and future units.","READY TO DEPLOY\nPractice complete! Your mission starts fresh with normal resources and no upgrades."};
                if(tutorialRing==null) tutorialRing=StrategyFeedback.Ring(transform,match.beamMaterial,2,StrategyUI.Accent);
                var target=step==0?match.Entities.Find(e=>e.kind==EntityKind.Worker):step==3?match.Entities.Find(e=>e.kind==EntityKind.Barracks):step==4?match.Entities.Find(e=>e.kind==EntityKind.Soldier):null;
                var deposit=step==1?System.Array.Find(Object.FindObjectsByType<MineralDeposit>(FindObjectsSortMode.None),d=>d.transform.position.x<0 && d.transform.position.z<-12):null;
                tutorialRing.gameObject.SetActive(match.Running && step<5);
                tutorialRing.transform.position=target!=null?target.transform.position:deposit!=null?deposit.transform.position:step==2?new Vector3(-9,0,-11):new Vector3(0,0,-2);
                tutorialText.text=instructions[Mathf.Min(step,6)]+(step<6?"\nPractice progress resets when you deploy.":"");
                ActionLabel(tutorialNext,step>=6?"Start fresh mission":"Skip tutorial","Normal resources / five waves");
                for(int i=0;i<3;i++) Highlight(researchButtons[i],step==5 && !match.Research.Active.HasValue && !match.Research.Completed((UpgradeKind)i));
                Highlight(gather,step==1); Highlight(barracks,step==2); Highlight(train,step==3); Highlight(attack,step==4);
            }
            notice.text=commander.TargetingOrder.HasValue?(commander.TargetingOrder==UnitOrder.Gather?"GATHER / Click a teal deposit. Esc or right-click cancels.":"MOVE / Click ground. Esc or right-click cancels."):commander.TargetingAttackMove?"ATTACK-MOVE / Click terrain. Esc or right-click to cancel.":commander.Placement.HasValue?$"PLACE {commander.Placement} / {(commander.PlacementValid?"Click to build":commander.PlacementReason)}":!string.IsNullOrEmpty(match.Notice)?match.Notice:producer && selected.Production.Count>=5?"Production queue full (5).":producer && match.Wallet.Minerals<match.settings.Cost(kind)?"More minerals needed to train. Select workers and right-click a deposit.":"Hold the perimeter. Mine, build and command your defenses.";
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
                bar.gameObject.SetActive(match.Running && point.z>0 && point.y>Screen.height*.25f && point.y<Screen.height*.895f && (e.Selected || e.Health.Current<e.Health.Maximum));
                var r=bar.rectTransform; r.anchoredPosition=new Vector2(point.x,point.y)/canvas.scaleFactor; r.sizeDelta=new Vector2(64,7);
                ((RectTransform)r.GetChild(0)).anchorMax=new Vector2(e.Health.Current/e.Health.Maximum,1);
            }
        }
    }
}
