using UnityEngine;
namespace Engchanok.StrategyGame
{
    public sealed class StrategyHud : MonoBehaviour
    {
        public StrategyMatch match;
        public StrategyCommander commander;
        GUIStyle title, text, button, small;
        void Styles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
            text = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            small = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            button = new GUIStyle(GUI.skin.button) { fontSize = 15 };
        }
        static void Panel(Rect rect, Color color) { var old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old; }
        void OnGUI()
        {
            Styles();
            if (match.Waves == null) return;
            float w = Screen.width, h = Screen.height;
            Panel(new Rect(0, 0, w, 76), new Color(.025f, .05f, .09f, .96f));
            GUI.Label(new Rect(22, 10, 250, 34), "OUTPOST / SURVIVAL", title);
            GUI.Label(new Rect(22, 44, 450, 25), "Hold the perimeter. Survive all five waves.", small);
            GUI.Label(new Rect(w * .36f, 14, 230, 25), "MINERALS  " + match.Wallet.Minerals, text);
            GUI.Label(new Rect(w * .36f, 43, 230, 25), "HQ  " + (match.Headquarters != null ? Mathf.CeilToInt(match.Headquarters.Health.Current) : 0), small);
            string wave = match.Waves.Active ? "WAVE " + match.Waves.Wave + " / " + match.Waves.Total : "NEXT WAVE IN " + Mathf.CeilToInt(match.Waves.Countdown) + "s";
            GUI.Label(new Rect(w * .62f, 15, 245, 26), wave, text);
            GUI.Label(new Rect(w * .62f, 44, 245, 25), "HOSTILES  " + match.Entities.FindAll(e => e != null && e.IsEnemy && e.Alive).Count, small);
            if (GUI.Button(new Rect(w - 98, 17, 80, 37), "Pause", button) && match.Waves.Result == MatchResult.Playing) match.SetPaused(!match.Paused);
            Panel(new Rect(0, h - 158, w, 158), new Color(.025f, .05f, .09f, .96f));
            GUI.Label(new Rect(22, h - 149, w - 44, 27), commander.Placement.HasValue ? "PLACE " + commander.Placement + " / " + (commander.PlacementValid ? "Click to build" : commander.PlacementReason) : match.Notice, text);
            var selected = commander.Selection.Count == 1 ? commander.Selection[0] : null;
            GUI.Label(new Rect(22, h - 113, 300, 26), selected != null ? selected.kind + " / " + Mathf.CeilToInt(selected.Health.Current) + " HP" : commander.Selection.Count + " units selected", text);
            GUI.Label(new Rect(22, h - 82, 280, 25), selected != null && selected.Production.Count > 0 ? "Queue: " + selected.Production.Count + " / " + Mathf.CeilToInt(selected.Production.Remaining) + "s" : "LMB select / RMB command", small);
            GUI.enabled = match.Running && !commander.Placement.HasValue;
            if (selected != null && (selected.kind == EntityKind.Headquarters || selected.kind == EntityKind.Barracks))
            {
                var kind = selected.kind == EntityKind.Headquarters ? EntityKind.Worker : EntityKind.Soldier;
                if (GUI.Button(new Rect(w * .32f, h - 111, 190, 45), "Train " + kind + " / " + match.settings.Cost(kind), button)) match.Train(selected);
            }
            if (GUI.Button(new Rect(w * .55f, h - 111, 190, 45), "Barracks / " + match.settings.barracksCost, button)) commander.BeginPlacement(EntityKind.Barracks);
            if (GUI.Button(new Rect(w * .77f, h - 111, 170, 45), "Turret / " + match.settings.turretCost, button)) commander.BeginPlacement(EntityKind.Turret);
            GUI.enabled = true;
            GUI.Label(new Rect(22, h - 37, w - 44, 24), "WASD / arrows: pan    Wheel: zoom    Shift: add selection    Drag: select units    Esc: cancel / pause", small);
            if (commander.Dragging)
            {
                Vector2 p = commander.Pointer, s = commander.DragStart;
                Panel(Rect.MinMaxRect(Mathf.Min(p.x, s.x), h - Mathf.Max(p.y, s.y), Mathf.Max(p.x, s.x), h - Mathf.Min(p.y, s.y)), new Color(.15f, .8f, 1, .22f));
            }
            foreach (var e in match.Entities)
            {
                if (e == null || !e.Alive) continue;
                Vector3 p = commander.view.WorldToScreenPoint(e.transform.position + Vector3.up * (e.IsUnit ? 2.2f : 4));
                if (p.z <= 0 || p.y < 165 || p.y > h - 82) continue;
                if (e.Selected || e.Health.Current < e.Health.Maximum)
                {
                    Panel(new Rect(p.x - 23, h - p.y, 46, 5), Color.black);
                    Panel(new Rect(p.x - 22, h - p.y + 1, 44 * e.Health.Current / e.Health.Maximum, 3), e.IsEnemy ? Color.red : Color.cyan);
                    if (e.Selected) GUI.Label(new Rect(p.x - 35, h - p.y - 22, 100, 22), e.kind.ToString(), small);
                }
            }
            if (!match.Running)
            {
                Panel(new Rect(0, 0, w, h), new Color(0, .015f, .035f, .82f));
                float cx = w / 2 - 170, cy = h / 2 - 130;
                GUI.Label(new Rect(cx, cy, 400, 45), match.Paused ? "MISSION PAUSED" : match.Waves.Result == MatchResult.Victory ? "OUTPOST SECURED" : "OUTPOST LOST", title);
                GUI.Label(new Rect(cx, cy + 46, 390, 40), match.Paused ? "Orders are on hold." : match.Waves.Result == MatchResult.Victory ? "All five waves defeated." : "Headquarters has been destroyed.", text);
                if (match.Paused && GUI.Button(new Rect(cx, cy + 95, 340, 38), "Resume", button)) match.SetPaused(false);
                if (GUI.Button(new Rect(cx, cy + 140, 340, 38), "Restart mission", button)) match.Restart();
                if (GUI.Button(new Rect(cx, cy + 185, 340, 38), "Main menu", button)) match.MainMenu();
            }
        }
    }
}
