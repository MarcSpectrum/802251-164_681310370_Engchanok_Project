using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
namespace Engchanok.StrategyGame
{
    public sealed class StrategyCommander : MonoBehaviour
    {
        public StrategyMatch match;
        public Camera view;
        public readonly List<StrategyEntity> Selection = new();
        public EntityKind? Placement { get; private set; }
        public Vector2 DragStart { get; private set; }
        public bool Dragging { get; private set; }
        public Vector2 Pointer => Mouse.current == null ? Vector2.zero : Mouse.current.position.ReadValue();
        public bool PointerOverUI
        {
            get
            {
                if (EventSystem.current == null) return false;
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = Pointer }, hits);
                return hits.Count > 0;
            }
        }
        public event System.Action TargetingStarted;
        public UnitOrder? TargetingOrder { get; private set; }
        public bool Targeting => TargetingAttackMove || TargetingOrder.HasValue || TargetingRally;
        public void BeginOrder(UnitOrder order)
        {
            if (!match.Running || (order != UnitOrder.Move && order != UnitOrder.Gather) || !Selection.Exists(e => e != null && e.Alive && (order == UnitOrder.Gather ? e.kind == EntityKind.Worker : e.IsUnit))) return;
            CancelInteractions(); TargetingOrder = order; Dragging = false; TargetingStarted?.Invoke();
        }
        public bool TargetingAttackMove { get; private set; }
        readonly Dictionary<int, List<StrategyEntity>> groups = new();
        public void BeginAttackMove()
        {
            if (!match.Running || !Selection.Exists(e => e != null && e.Alive && e.kind == EntityKind.Soldier)) return;
            CancelInteractions(); TargetingAttackMove = true; Dragging = false; TargetingStarted?.Invoke();
        }
        public void StoreGroup(int number)
        {
            if (!match.Running || number < 1 || number > 9) return;
            groups[number] = Selection.FindAll(e => e != null && e.Alive && !e.IsEnemy && e.IsUnit);
        }
        public void RecallGroup(int number)
        {
            if (!match.Running || !groups.TryGetValue(number, out var group)) return;
            group.RemoveAll(e => e == null || !e.Alive);
            if (group.Count == 0) return;
            CancelInteractions(); ClearSelection(); foreach (var e in group) Select(e);
        }
        public bool IssueAttackMove(Vector3 destination)
        {
            if (!match.Running) return false;
            bool accepted = false;
            foreach (var e in Selection) if (e != null && e.Alive) accepted |= e.AttackMove(destination);
            if (accepted) { StrategyFeedback.Marker(match, destination, Color.cyan); match.TutorialAttackIssued = true; }
            return accepted;
        }
        public Vector3 PlacementPoint { get; private set; }
        public bool PlacementValid { get; private set; }
        public string PlacementReason { get; private set; }
        GameObject preview;
        Material previewMaterial;
        public StrategyCameraController CameraController { get; private set; }
        void Awake()
        {
            CameraController = GetComponent<StrategyCameraController>();
            if (CameraController == null) CameraController = gameObject.AddComponent<StrategyCameraController>();
            CameraController.match = match; CameraController.commander = this; CameraController.view = view;
        }
        public void CancelInteractions()
        {
            CancelPlacement(); TargetingAttackMove = false; TargetingOrder = null; TargetingRally = false; rallyProducer = null; Dragging = false;
        }
        public Transform InspectedObject { get; private set; }
        public bool PopupOpen { get; private set; }
        public bool TargetingRally { get; private set; }
        StrategyEntity rallyProducer;
        public void ClosePopup() { PopupOpen = false; }
        public bool InspectObject(Transform candidate)
        {
            if (!match.Running || candidate == null) return false;
            var entity = candidate.GetComponentInParent<StrategyEntity>();
            var deposit = candidate.GetComponentInParent<MineralDeposit>();
            if (entity != null && entity.Alive) candidate = entity.transform;
            else if (deposit != null) candidate = deposit.transform;
            else return false;
            CancelInteractions(); ClearSelection();
            if (entity != null && !entity.IsEnemy) Select(entity);
            InspectedObject = candidate; PopupOpen = true;
            return true;
        }
        public void BeginRallyPoint()
        {
            if (!match.Running || Selection.Count != 1 || Selection[0] == null || !Selection[0].Alive || Selection[0].kind != EntityKind.Barracks) return;
            CancelInteractions(); rallyProducer = Selection[0]; TargetingRally = true; TargetingStarted?.Invoke();
        }
        public void BeginPlacement(EntityKind kind) { if (match.Running) { CancelInteractions(); Placement = kind; TargetingStarted?.Invoke(); } }
        public void CancelPlacement() { Placement = null; if (preview != null) Destroy(preview); if (previewMaterial != null) Destroy(previewMaterial); }
        void OnDestroy() { if (previewMaterial != null) Destroy(previewMaterial); }
        void Update()
        {
            if (match.Waves == null) return;
            Selection.RemoveAll(e => e == null || !e.Alive);
            if (InspectedObject == null || (InspectedObject.TryGetComponent<StrategyEntity>(out var inspected) && !inspected.Alive))
            {
                InspectedObject = Selection.Count > 0 ? Selection[0].transform : null;
                if (InspectedObject == null) PopupOpen = false;
            }
            if (Mouse.current == null || Keyboard.current == null) return;
            var mouse = Mouse.current; var keys = Keyboard.current;
            if (keys.escapeKey.wasPressedThisFrame)
            {
                if (Targeting) CancelInteractions();
                else if (Placement.HasValue) CancelPlacement();
                else if (match.Waves.Result == MatchResult.Playing) match.SetPaused(!match.Paused);
                Dragging = false;
                return;
            }
            if (!match.Running) { Dragging = false; if (preview != null) preview.SetActive(false); return; }
            if (keys.fKey.wasPressedThisFrame) BeginAttackMove();
            for (int n = 1; n <= 9; n++)
                if (keys[(Key)((int)Key.Digit1 + n - 1)].wasPressedThisFrame)
                { if (keys.leftCtrlKey.isPressed || keys.rightCtrlKey.isPressed) StoreGroup(n); else RecallGroup(n); }
            if (TargetingRally)
            {
                if (rallyProducer == null || !rallyProducer.Alive || mouse.rightButton.wasPressedThisFrame) { CancelInteractions(); return; }
                if (mouse.leftButton.wasPressedThisFrame && !PointerOverUI && Physics.Raycast(view.ScreenPointToRay(Pointer), out var rallyHit, 300))
                {
                    if (rallyProducer.SetRallyPoint(rallyHit.point)) { StrategyFeedback.Marker(match, rallyHit.point, Color.cyan); CancelInteractions(); }
                    else match.Notify("Choose reachable ground for the rally point.");
                }
                return;
            }
            if (TargetingOrder.HasValue)
            {
                if (mouse.rightButton.wasPressedThisFrame) { TargetingOrder = null; return; }
                if (mouse.leftButton.wasPressedThisFrame && !PointerOverUI && Physics.Raycast(view.ScreenPointToRay(Pointer), out var orderHit, 300))
                {
                    var deposit = orderHit.collider.GetComponentInParent<MineralDeposit>();
                    if (TargetingOrder == UnitOrder.Gather && deposit == null) { match.Notify("Click a teal mineral deposit."); return; }
                    int index = 0;
                    foreach (var unit in Selection)
                        if (unit != null && unit.Alive && unit.IsUnit) { if (TargetingOrder == UnitOrder.Gather) unit.Gather(deposit); else { float angle = index * 2.4f, radius = Mathf.Sqrt(index++) * 1.3f; unit.Move(orderHit.point + new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius); } }
                    StrategyFeedback.Marker(match, orderHit.point, Color.cyan); TargetingOrder = null;
                }
                return;
            }
            if (TargetingAttackMove)
            {
                if (mouse.rightButton.wasPressedThisFrame) { TargetingAttackMove = false; return; }
                if (mouse.leftButton.wasPressedThisFrame && !PointerOverUI && Physics.Raycast(view.ScreenPointToRay(Pointer), out var targetHit, 300))
                { IssueAttackMove(targetHit.point); TargetingAttackMove = false; }
                return;
            }
            if (Placement.HasValue)
            {
                Dragging = false;
                if (mouse.rightButton.wasPressedThisFrame) { CancelPlacement(); return; }
                UpdatePreview();
                if (mouse.leftButton.wasPressedThisFrame && !PointerOverUI && PlacementValid && match.Build(Placement.Value, PlacementPoint)) CancelPlacement();
                return;
            }
            if (mouse.leftButton.wasPressedThisFrame && !PointerOverUI) { DragStart = Pointer; Dragging = true; }
            if (mouse.leftButton.wasReleasedThisFrame && Dragging)
            {
                Dragging = false;
                if (PointerOverUI) return;
                if (!keys.leftShiftKey.isPressed && !keys.rightShiftKey.isPressed) ClearSelection();
                if (Vector2.Distance(Pointer, DragStart) > 8)
                {
                    var rect = Rect.MinMaxRect(Mathf.Min(Pointer.x, DragStart.x), Mathf.Min(Pointer.y, DragStart.y), Mathf.Max(Pointer.x, DragStart.x), Mathf.Max(Pointer.y, DragStart.y));
                    foreach (var e in match.Entities)
                    {
                        var p = view.WorldToScreenPoint(e.transform.position);
                        if (!e.IsEnemy && e.IsUnit && p.z > 0 && rect.Contains(p)) Select(e);
                    }
                }
                else if (Physics.Raycast(view.ScreenPointToRay(Pointer), out var hit, 300))
                {
                    var e = hit.collider.GetComponentInParent<StrategyEntity>();
                    if (e != null && !e.IsEnemy) Select(e);
                    else if (!keys.leftShiftKey.isPressed && !keys.rightShiftKey.isPressed) InspectObject(hit.collider.transform);
                }
            }
            if (mouse.rightButton.wasPressedThisFrame && !PointerOverUI && Physics.Raycast(view.ScreenPointToRay(Pointer), out var commandHit, 300))
            {
                var enemy = commandHit.collider.GetComponentInParent<StrategyEntity>();
                var deposit = commandHit.collider.GetComponentInParent<MineralDeposit>();
                if (Selection.Count == 1 && Selection[0].kind == EntityKind.Barracks)
                {
                    if (Selection[0].SetRallyPoint(commandHit.point)) StrategyFeedback.Marker(match, commandHit.point, Color.cyan);
                    else match.Notify("Choose reachable ground for the rally point.");
                    return;
                }
                StrategyFeedback.Marker(match, commandHit.point, Color.cyan);
                int index = 0;
                foreach (var unit in Selection)
                {
                    if (!unit.IsUnit) continue;
                    if (enemy != null && enemy.IsEnemy && unit.kind == EntityKind.Soldier) unit.Attack(enemy);
                    else if (deposit != null && unit.kind == EntityKind.Worker) unit.Gather(deposit);
                    else
                    {
                        float angle = index * 2.4f, radius = Mathf.Sqrt(index) * 1.3f;
                        unit.Move(commandHit.point + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius);
                        index++;
                    }
                }
            }
        }
        public void ClearSelection() { foreach (var e in Selection) if (e != null) e.Selected = false; Selection.Clear(); InspectedObject = null; PopupOpen = false; }
        public void Select(StrategyEntity entity)
        {
            if (!match.Running || entity == null || !entity.Alive || entity.IsEnemy) return;
            if (!Selection.Contains(entity)) Selection.Add(entity);
            entity.Selected = true; InspectedObject = entity.transform; PopupOpen = true;
        }
        void UpdatePreview()
        {
            if (preview == null)
            {
                preview = GameObject.CreatePrimitive(PrimitiveType.Cube); preview.name = "Placement preview";
                Destroy(preview.GetComponent<Collider>()); preview.layer = 2;
                previewMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")); preview.GetComponent<Renderer>().sharedMaterial = previewMaterial;
            }
            preview.SetActive(true);
            var plane = new Plane(Vector3.up, Vector3.zero); var ray = view.ScreenPointToRay(Pointer);
            bool ground = plane.Raycast(ray, out float distance);
            PlacementPoint = ground ? ray.GetPoint(distance) : Vector3.zero;
            string reason = "Choose ground outside the HUD.";
            PlacementValid = ground && !PointerOverUI && match.CanPlace(Placement.Value, PlacementPoint, out reason);
            PlacementReason = reason;
            float radius = match.settings.Radius(Placement.Value);
            preview.transform.position = PlacementPoint + Vector3.up * .15f;
            preview.transform.localScale = new Vector3(radius * 2, .25f, radius * 2);
            previewMaterial.color = PlacementValid ? new Color(.1f, .9f, .5f) : new Color(1, .2f, .2f);
        }
    }
}
