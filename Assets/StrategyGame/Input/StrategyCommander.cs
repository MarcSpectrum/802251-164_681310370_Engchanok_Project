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
        public bool Targeting => TargetingAttackMove || TargetingOrder.HasValue || TargetingRally || TargetingPower.HasValue;
        // A commander power waiting for its target. It needs no selection: powers are called onto the battlefield, not issued to units.
        public PowerKind? TargetingPower { get; private set; }
        LineRenderer powerPreview;
        PowerKind? previewShape;
        public void BeginPower(PowerKind kind)
        {
            if (!match.Running) return;
            // Asking for the power already being aimed puts it away again.
            if (TargetingPower == kind) { CancelInteractions(); return; }
            if (!match.CanUsePower(kind, out var reason)) { match.Notify(StrategySettings.PowerName(kind) + ": " + reason + "."); return; }
            CancelInteractions(); TargetingPower = kind; Dragging = false; TargetingStarted?.Invoke();
        }
        public void BeginOrder(UnitOrder order)
        {
            if (!match.Running || (order != UnitOrder.Move && order != UnitOrder.Gather) || !Selection.Exists(e => e != null && e.Alive && (order == UnitOrder.Gather ? e.kind == EntityKind.Worker : e.IsUnit))) return;
            CancelInteractions(); TargetingOrder = order; Dragging = false; TargetingStarted?.Invoke();
        }
        public bool TargetingAttackMove { get; private set; }
        readonly Dictionary<int, List<StrategyEntity>> groups = new();
        public void BeginAttackMove()
        {
            if (!match.Running || !Selection.Exists(e => e != null && e.Alive && e.IsUnit && !e.IsEnemy && e.kind != EntityKind.Worker)) return;
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
            CancelPlacement(); TargetingAttackMove = false; TargetingOrder = null; TargetingRally = false; rallyProducer = null; Dragging = false; minimapDragging = false;
            TargetingPower = null; if (powerPreview != null) powerPreview.gameObject.SetActive(false);
        }
        // The minimap only draws and maps coordinates; every click on it is routed through this Update so input order stays deterministic.
        public StrategyMinimap Minimap { get; set; }
        bool minimapDragging;
        bool MinimapPoint(out Vector3 point) { point = default; return Minimap != null && Minimap.TryWorldPoint(Pointer, out point); }
        StrategyEntity lastIdleWorker;
        // Cycles through idle workers in spawn order, so repeated presses visit each one before wrapping.
        public bool SelectNextIdleWorker()
        {
            if (!match.Running) return false;
            var idle = match.Entities.FindAll(e => e != null && e.IsIdleWorker);
            if (idle.Count == 0) return false;
            int after = lastIdleWorker != null ? match.Entities.IndexOf(lastIdleWorker) : -1;
            var worker = idle.Find(e => match.Entities.IndexOf(e) > after) ?? idle[0];
            lastIdleWorker = worker;
            CancelInteractions(); ClearSelection(); Select(worker);
            CameraController.JumpTo(worker.transform.position);
            return true;
        }
        // Finishes whichever destination targeting is active. Shared by world and minimap clicks so both pass the same validation.
        public bool CompleteTargeting(Vector3 point, MineralDeposit deposit)
        {
            if (!match.Running) return false;
            if (TargetingRally)
            {
                if (rallyProducer == null || !rallyProducer.Alive) { CancelInteractions(); return false; }
                if (!rallyProducer.SetRallyPoint(point)) { match.Notify("Choose reachable ground for the rally point."); return false; }
                StrategyFeedback.Marker(match, point, Color.cyan); CancelInteractions(); return true;
            }
            if (TargetingOrder.HasValue)
            {
                if (TargetingOrder == UnitOrder.Gather && deposit == null) { match.Notify("Click a teal mineral deposit."); return false; }
                int index = 0;
                foreach (var unit in Selection)
                    if (unit != null && unit.Alive && unit.IsUnit) { if (TargetingOrder == UnitOrder.Gather) unit.Gather(deposit); else { float angle = index * 2.4f, radius = Mathf.Sqrt(index++) * 1.3f; unit.Move(point + new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius); } }
                StrategyFeedback.Marker(match, point, Color.cyan); TargetingOrder = null; return true;
            }
            if (TargetingAttackMove) { IssueAttackMove(point); TargetingAttackMove = false; return true; }
            if (TargetingPower.HasValue)
            {
                var kind = TargetingPower.Value;
                // A bad point keeps the power aimed; a power that can no longer be afforded or is recharging stops aiming.
                if (!match.UsePower(kind, point)) { if (!match.CanUsePower(kind, out _)) CancelInteractions(); return false; }
                CancelInteractions(); return true;
            }
            return false;
        }
        // Right-click semantics, shared by world and minimap: a lone producer takes a rally point, otherwise units attack, gather or move.
        public void CommandAt(Vector3 point, StrategyEntity enemy, MineralDeposit deposit)
        {
            if (!match.Running) return;
            if (Selection.Count == 1 && match.settings.IsProducer(Selection[0].kind))
            {
                if (Selection[0].SetRallyPoint(point)) StrategyFeedback.Marker(match, point, Color.cyan);
                else match.Notify("Choose reachable ground for the rally point.");
                return;
            }
            StrategyFeedback.Marker(match, point, Color.cyan);
            int index = 0;
            foreach (var unit in Selection)
            {
                if (!unit.IsUnit) continue;
                if (enemy != null && enemy.IsEnemy && StrategyEntity.IsFighter(unit.kind)) unit.Attack(enemy);
                else if (deposit != null && unit.kind == EntityKind.Worker) unit.Gather(deposit);
                else
                {
                    float angle = index * 2.4f, radius = Mathf.Sqrt(index) * 1.3f;
                    unit.Move(point + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius);
                    index++;
                }
            }
        }
        // A world click or a minimap click. The minimap is UI, so the world raycast alone would never see it.
        bool TryTargetPoint(out Vector3 point, out MineralDeposit deposit)
        {
            deposit = null;
            if (MinimapPoint(out point)) { deposit = NearestDeposit(point, 4); return true; }
            if (PointerOverUI || !Physics.Raycast(view.ScreenPointToRay(Pointer), out var hit, 300)) return false;
            point = hit.point; deposit = hit.collider.GetComponentInParent<MineralDeposit>(); return true;
        }
        static MineralDeposit NearestDeposit(Vector3 point, float range)
        {
            MineralDeposit best = null;
            foreach (var deposit in FindObjectsByType<MineralDeposit>(FindObjectsSortMode.None))
            {
                var offset = deposit.transform.position - point; offset.y = 0;
                if (offset.magnitude < range) { best = deposit; range = offset.magnitude; }
            }
            return best;
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
            if (!match.Running || Selection.Count != 1 || Selection[0] == null || !Selection[0].Alive || !match.settings.IsProducer(Selection[0].kind)) return;
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
            if (!match.Running) { Dragging = false; minimapDragging = false; if (preview != null) preview.SetActive(false); if (powerPreview != null) powerPreview.gameObject.SetActive(false); return; }
            if (keys.fKey.wasPressedThisFrame) BeginAttackMove();
            if (keys.iKey.wasPressedThisFrame) SelectNextIdleWorker();
            for (int n = 1; n <= 9; n++)
                if (keys[(Key)((int)Key.Digit1 + n - 1)].wasPressedThisFrame)
                { if (keys.leftCtrlKey.isPressed || keys.rightCtrlKey.isPressed) StoreGroup(n); else RecallGroup(n); }
            for (int p = 0; p < StrategyHud.PowerKeys.Length; p++)
                if (keys[StrategyHud.PowerKeys[p]].wasPressedThisFrame) BeginPower((PowerKind)p);
            if (Targeting)
            {
                if (mouse.rightButton.wasPressedThisFrame || (TargetingRally && (rallyProducer == null || !rallyProducer.Alive))) { CancelInteractions(); return; }
                UpdatePowerPreview();
                if (mouse.leftButton.wasPressedThisFrame && TryTargetPoint(out var targetPoint, out var targetDeposit)) CompleteTargeting(targetPoint, targetDeposit);
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
            if (minimapDragging)
            {
                if (!mouse.leftButton.isPressed) minimapDragging = false;
                else if (Minimap != null) CameraController.JumpTo(Minimap.ClampedWorldPoint(Pointer));
            }
            if (mouse.leftButton.wasPressedThisFrame && MinimapPoint(out var pressed)) { minimapDragging = true; Dragging = false; CameraController.JumpTo(pressed); }
            else if (mouse.leftButton.wasPressedThisFrame && !PointerOverUI) { DragStart = Pointer; Dragging = true; }
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
            if (mouse.rightButton.wasPressedThisFrame)
            {
                if (MinimapPoint(out var mapTarget)) CommandAt(mapTarget, null, null);
                else if (!PointerOverUI && Physics.Raycast(view.ScreenPointToRay(Pointer), out var commandHit, 300))
                    CommandAt(commandHit.point, commandHit.collider.GetComponentInParent<StrategyEntity>(), commandHit.collider.GetComponentInParent<MineralDeposit>());
            }
        }
        public void ClearSelection() { foreach (var e in Selection) if (e != null) e.Selected = false; Selection.Clear(); InspectedObject = null; PopupOpen = false; }
        public void Select(StrategyEntity entity)
        {
            if (!match.Running || entity == null || !entity.Alive || entity.IsEnemy) return;
            if (!Selection.Contains(entity)) Selection.Add(entity);
            entity.Selected = true; InspectedObject = entity.transform; PopupOpen = true;
        }
        // Outlines the aimed power's area on the ground under the pointer, in the same shape its zone will take.
        void UpdatePowerPreview()
        {
            var power = TargetingPower.HasValue ? match.settings.PowerOf(TargetingPower.Value) : null;
            if (power == null) { if (powerPreview != null) powerPreview.gameObject.SetActive(false); return; }
            if (powerPreview == null)
            {
                powerPreview = StrategyFeedback.Ring(transform, match.beamMaterial, 1, Color.white);
                powerPreview.name = "Power preview"; powerPreview.gameObject.layer = 2; powerPreview.startWidth = powerPreview.endWidth = .12f;
            }
            if (previewShape != power.kind)
            {
                StrategyPowers.Shape(powerPreview, power); previewShape = power.kind;
                var tint = new MaterialPropertyBlock(); tint.SetColor("_BaseColor", StrategyPowers.ColorOf(power.kind)); powerPreview.SetPropertyBlock(tint);
            }
            var plane = new Plane(Vector3.up, Vector3.zero); var ray = view.ScreenPointToRay(Pointer);
            bool ground = plane.Raycast(ray, out float distance) && !PointerOverUI;
            powerPreview.gameObject.SetActive(ground);
            if (ground) powerPreview.transform.position = ray.GetPoint(distance);
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
