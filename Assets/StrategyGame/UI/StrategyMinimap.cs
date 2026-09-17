using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Engchanok.StrategyGame
{
    // Presentation and coordinate mapping only. StrategyCommander routes every click on it, so input order stays deterministic.
    public sealed class StrategyMinimap : MonoBehaviour
    {
        public const float Size = 240, AlertSeconds = 4;
        static readonly Color Ally = new(.15f, .85f, .85f), Structure = new(.4f, .68f, .8f), Worker = new(1, .72f, .25f);
        static readonly Color Hostile = new(1, .42f, .38f), Mineral = new(.15f, .95f, .8f), Depleted = new(.35f, .4f, .42f);
        // Viewport corners in winding order, so projecting them gives the camera's ground footprint as a polygon.
        static readonly Vector2[] Corners = { new(0, 0), new(1, 0), new(1, 1), new(0, 1) };
        public StrategyMatch match;
        public Camera view;
        RectTransform field, structures, units, hostiles, overlay, alert;
        readonly Dictionary<StrategyEntity, Image> blips = new();
        readonly Dictionary<MineralDeposit, Image> deposits = new();
        // A quadrilateral clipped by the four map edges has at most eight sides.
        readonly Image[] frame = new Image[8], alertEdges = new Image[4];
        readonly List<RaycastResult> hits = new();
        readonly List<Vector2> footprint = new(8);
        public int BlipCount => blips.Count + deposits.Count;
        float HalfSize => match != null && match.settings != null ? match.settings.mapHalfSize : 40;

        // The camera looks along +z, so world +z is up on the map and +x is right.
        public static Vector2 WorldToMap(Vector3 world, float halfSize)
        {
            halfSize = Mathf.Max(.01f, halfSize);
            return new Vector2(Mathf.Clamp01((world.x + halfSize) / (2 * halfSize)), Mathf.Clamp01((world.z + halfSize) / (2 * halfSize)));
        }
        public static Vector3 MapToWorld(Vector2 map, float halfSize) => new((Mathf.Clamp01(map.x) * 2 - 1) * halfSize, 0, (Mathf.Clamp01(map.y) * 2 - 1) * halfSize);
        static Vector2 Unclamped(Vector3 world, float halfSize) => new((world.x + halfSize) / (2 * halfSize), (world.z + halfSize) / (2 * halfSize));
        // Sutherland-Hodgman against the unit square. The tilted camera sees far past the map edge, so clamping each corner
        // independently would bend the footprint's sides; clipping keeps them where the camera actually looks.
        public static void ClipToMap(List<Vector2> polygon)
        {
            var input = new List<Vector2>(polygon.Count + 4);
            for (int side = 0; side < 4 && polygon.Count > 0; side++)
            {
                input.Clear(); input.AddRange(polygon); polygon.Clear();
                for (int i = 0; i < input.Count; i++)
                {
                    Vector2 a = input[i], b = input[(i + 1) % input.Count];
                    bool keepA = Inside(a, side), keepB = Inside(b, side);
                    if (keepA) polygon.Add(a);
                    if (keepA != keepB) polygon.Add(Crossing(a, b, side));
                }
            }
        }
        static bool Inside(Vector2 p, int side) => side switch { 0 => p.x >= 0, 1 => p.x <= 1, 2 => p.y >= 0, _ => p.y <= 1 };
        static Vector2 Crossing(Vector2 a, Vector2 b, int side)
        {
            float t = side switch { 0 => -a.x / (b.x - a.x), 1 => (1 - a.x) / (b.x - a.x), 2 => -a.y / (b.y - a.y), _ => (1 - a.y) / (b.y - a.y) };
            return a + (b - a) * t;
        }

        public static StrategyMinimap Create(Transform canvas, StrategyMatch match, Camera view)
        {
            var panel = StrategyUI.Panel(canvas, "Minimap", new Vector2(0, .085f), new Vector2(0, .085f), StrategyUI.Ink);
            var rect = panel.rectTransform; rect.pivot = Vector2.zero; rect.sizeDelta = Vector2.one * Size; rect.anchoredPosition = new Vector2(16, 8);
            var minimap = panel.gameObject.AddComponent<StrategyMinimap>();
            minimap.match = match; minimap.view = view; minimap.Build();
            return minimap;
        }
        void Build()
        {
            var ground = StrategyUI.Panel(transform, "Field", Vector2.zero, Vector2.one, new Color(.055f, .105f, .14f)); ground.raycastTarget = false;
            field = ground.rectTransform; field.offsetMin = new Vector2(6, 6); field.offsetMax = new Vector2(-6, -6);
            StrategyUI.Rule(transform, new Vector2(0, .985f), Vector2.one);
            structures = Layer("Structures"); units = Layer("Units"); hostiles = Layer("Hostiles"); overlay = Layer("Overlay");
            // Approach lanes and spawn points are fixed, so they are drawn once.
            foreach (var start in StrategyMatch.SpawnPoints)
            {
                Line(structures, start, StrategyMatch.HomePosition, new Color(.15f, .22f, .27f), 7);
                Place(Dot(structures, new Color(Hostile.r, Hostile.g, Hostile.b, .55f), 9).rectTransform, start);
            }
            for (int i = 0; i < frame.Length; i++) { frame[i] = Dot(overlay, new Color(1, 1, 1, .8f), 0); frame[i].name = "Camera frame"; }
            alert = StrategyUI.Rect(overlay, "Attack alert", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            for (int i = 0; i < 4; i++)
            {
                alertEdges[i] = Dot(alert, Hostile, 0);
                var edge = alertEdges[i].rectTransform;
                edge.anchorMin = i switch { 0 => Vector2.zero, 1 => new Vector2(0, 1), 2 => Vector2.zero, _ => new Vector2(1, 0) };
                edge.anchorMax = i switch { 0 => new Vector2(1, 0), 1 => Vector2.one, 2 => new Vector2(0, 1), _ => Vector2.one };
                edge.sizeDelta = i < 2 ? new Vector2(0, 2) : new Vector2(2, 0);
            }
            alert.gameObject.SetActive(false);
        }
        RectTransform Layer(string name) => StrategyUI.Rect(field, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        static Image Dot(RectTransform parent, Color color, float size)
        {
            var dot = StrategyUI.Panel(parent, "Blip", new Vector2(.5f, .5f), new Vector2(.5f, .5f), color);
            dot.raycastTarget = false; dot.rectTransform.sizeDelta = Vector2.one * size; return dot;
        }
        void Place(RectTransform rect, Vector3 world)
        {
            rect.anchorMin = rect.anchorMax = WorldToMap(world, HalfSize); rect.anchoredPosition = Vector2.zero;
        }
        void Line(RectTransform parent, Vector3 from, Vector3 to, Color color, float width)
        {
            var line = Dot(parent, color, 0); line.name = "Approach lane";
            Segment(line.rectTransform, WorldToMap(from, HalfSize), WorldToMap(to, HalfSize), width, 0);
        }
        // A rotated strip between two normalized map points; overlap lengthens it so joined segments leave no gap at the corners.
        void Segment(RectTransform rect, Vector2 a, Vector2 b, float width, float overlap)
        {
            Vector2 delta = Vector2.Scale(b - a, field.rect.size);
            rect.anchorMin = rect.anchorMax = (a + b) / 2; rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(delta.magnitude + overlap, width);
            rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }
        // True only when the minimap is the topmost UI under the pointer, so a popup drawn over it keeps its own clicks.
        public bool TryWorldPoint(Vector2 screen, out Vector3 world)
        {
            world = default;
            if (field == null || !isActiveAndEnabled || !Normalized(screen, out var map) || map.x < 0 || map.x > 1 || map.y < 0 || map.y > 1) return false;
            if (EventSystem.current != null)
            {
                hits.Clear();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screen }, hits);
                if (hits.Count == 0 || !hits[0].gameObject.transform.IsChildOf(transform)) return false;
            }
            world = MapToWorld(map, HalfSize); return true;
        }
        // Dragging past the edge keeps the camera on the border instead of dropping the drag.
        public Vector3 ClampedWorldPoint(Vector2 screen) => Normalized(screen, out var map) ? MapToWorld(map, HalfSize) : Vector3.zero;
        public Vector2 ScreenPoint(Vector3 world)
        {
            var map = WorldToMap(world, HalfSize); var rect = field.rect;
            return RectTransformUtility.WorldToScreenPoint(null, field.TransformPoint(new Vector3(rect.xMin + map.x * rect.width, rect.yMin + map.y * rect.height)));
        }
        bool Normalized(Vector2 screen, out Vector2 map)
        {
            map = default;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(field, screen, null, out var local)) return false;
            var rect = field.rect;
            map = new Vector2((local.x - rect.xMin) / rect.width, (local.y - rect.yMin) / rect.height);
            return true;
        }
        void LateUpdate()
        {
            if (match == null || match.Waves == null || field == null) return;
            if (deposits.Count == 0)
                foreach (var deposit in FindObjectsByType<MineralDeposit>(FindObjectsSortMode.None))
                { var dot = Dot(structures, Mineral, 9); dot.name = "Deposit"; dot.rectTransform.localRotation = Quaternion.Euler(0, 0, 45); Place(dot.rectTransform, deposit.transform.position); deposits[deposit] = dot; }
            foreach (var pair in deposits) if (pair.Key != null) pair.Value.color = pair.Key.Stock != null && pair.Key.Stock.Remaining > 0 ? Mineral : Depleted;
            var dead = new List<StrategyEntity>();
            foreach (var pair in blips) if (pair.Key == null || !pair.Key.Alive) { Destroy(pair.Value.gameObject); dead.Add(pair.Key); }
            foreach (var entity in dead) blips.Remove(entity);
            float scale = field.rect.width / (2 * HalfSize);
            foreach (var entity in match.Entities)
            {
                if (entity == null || !entity.Alive) continue;
                if (!blips.TryGetValue(entity, out var blip))
                {
                    float size = Mathf.Max(entity.IsUnit ? 5 : 6, match.settings.Radius(entity.kind) * 2 * scale);
                    blip = Dot(entity.IsEnemy ? hostiles : entity.IsUnit ? units : structures, Color.white, size);
                    blip.name = entity.kind.ToString(); blips[entity] = blip;
                }
                blip.color = entity.Selected ? Color.white : entity.IsEnemy ? Hostile : entity.kind == EntityKind.Worker ? Worker : entity.IsUnit ? Ally : Structure;
                Place(blip.rectTransform, entity.transform.position);
            }
            UpdateFrame(); UpdateAlert();
        }
        // The camera footprint: the four viewport corners projected onto the ground, a trapezoid for this tilted camera, clipped to the map.
        void UpdateFrame()
        {
            if (view == null) return;
            var plane = new Plane(Vector3.up, Vector3.zero);
            footprint.Clear();
            foreach (var corner in Corners)
            {
                var ray = view.ViewportPointToRay(corner);
                footprint.Add(Unclamped(plane.Raycast(ray, out float distance) ? ray.GetPoint(distance) : ray.GetPoint(view.farClipPlane), HalfSize));
            }
            ClipToMap(footprint);
            for (int i = 0; i < frame.Length; i++)
            {
                bool used = i < footprint.Count;
                frame[i].gameObject.SetActive(used);
                if (used) Segment(frame[i].rectTransform, footprint[i], footprint[(i + 1) % footprint.Count], 2, 2);
            }
        }
        void UpdateAlert()
        {
            float age = Time.time - match.LastAlertTime;
            bool show = match.HasAlert && age >= 0 && age < AlertSeconds;
            alert.gameObject.SetActive(show);
            if (!show) return;
            float pulse = Mathf.Repeat(age, 1);
            Place(alert, match.LastAlertPosition); alert.sizeDelta = Vector2.one * Mathf.Lerp(12, 34, pulse);
            foreach (var edge in alertEdges) edge.color = new Color(Hostile.r, Hostile.g, Hostile.b, 1 - pulse * .7f);
        }
    }
}
