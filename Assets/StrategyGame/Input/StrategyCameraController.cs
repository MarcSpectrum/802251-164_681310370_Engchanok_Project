using UnityEngine;
using UnityEngine.InputSystem;
namespace Engchanok.StrategyGame
{
    public enum StrategyCameraState { Tactical, Picking, Entering, Inspecting, Returning }

    public sealed class StrategyCameraController : MonoBehaviour
    {
        public StrategyMatch match;
        public StrategyCommander commander;
        public Camera view;
        [Min(.01f)] public float transitionSeconds = .35f, smoothingSeconds = .12f;
        public float orbitSensitivity = .25f;
        public StrategyCameraState State { get; private set; }
        public bool Inspecting => State == StrategyCameraState.Entering || State == StrategyCameraState.Inspecting || State == StrategyCameraState.Returning;
        public bool Picking => State == StrategyCameraState.Picking;
        Vector3 focus = new(0, 0, -12), desiredFocus = new(0, 0, -12);
        float height = 37, desiredHeight = 37;
        Transform target;
        Bounds bounds;
        float yaw, pitch, distance, minimumDistance, elapsed;
        Vector3 savedPosition, fromPosition;
        Quaternion savedRotation, fromRotation;
        bool orbiting;
        Vector3 panVelocity;

        public void BeginPicking()
        {
            if (!match.Running || Inspecting) return;
            commander.CancelInteractions(); State = StrategyCameraState.Picking;
        }
        public void CancelPicking() { if (Picking) State = StrategyCameraState.Tactical; }
        public bool BeginInspection(Transform candidate)
        {
            if (!match.Running || Inspecting || candidate == null) return false;
            var entity = candidate.GetComponentInParent<StrategyEntity>();
            var deposit = candidate.GetComponentInParent<MineralDeposit>();
            if (entity != null && entity.Alive) target = entity.transform;
            else if (deposit != null) target = deposit.transform;
            else return false;
            commander.CancelInteractions();
            bounds = VisualBounds(target);
            savedPosition = view.transform.position; savedRotation = view.transform.rotation;
            panVelocity = Vector3.zero;
            desiredFocus = focus; desiredHeight = height;
            yaw = target.eulerAngles.y + 180; pitch = 35;
            minimumDistance = bounds.extents.magnitude + view.nearClipPlane + .3f;
            distance = Mathf.Max(minimumDistance, FrameDistance(bounds) * 1.15f);
            match.SetInspectionPaused(true);
            StartTransition(StrategyCameraState.Entering);
            return true;
        }
        public void EndInspection()
        {
            if (!Inspecting || State == StrategyCameraState.Returning) return;
            orbiting = false; StartTransition(StrategyCameraState.Returning);
        }
        void StartTransition(StrategyCameraState state)
        {
            State = state; elapsed = 0; fromPosition = view.transform.position; fromRotation = view.transform.rotation;
        }
        public static Bounds VisualBounds(Transform root)
        {
            var result = new Bounds(root.position + Vector3.up, Vector3.one * 2); bool found = false;
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (!found) { result = renderer.bounds; found = true; } else result.Encapsulate(renderer.bounds);
            }
            return result;
        }
        float FrameDistance(Bounds value)
        {
            float vertical = view.fieldOfView * Mathf.Deg2Rad * .5f;
            float horizontal = Mathf.Atan(Mathf.Tan(vertical) * view.aspect);
            return value.extents.magnitude / Mathf.Sin(Mathf.Min(vertical, horizontal));
        }
        public void FocusSelection()
        {
            if (!match.Running || State != StrategyCameraState.Tactical) return;
            Bounds combined = default; bool found = false;
            foreach (var entity in commander.Selection)
            {
                if (entity == null || !entity.Alive) continue;
                if (!found) { combined = VisualBounds(entity.transform); found = true; }
                else combined.Encapsulate(VisualBounds(entity.transform));
            }
            if (!found) return;
            panVelocity = Vector3.zero;
            desiredFocus = combined.center; desiredFocus.y = 0;
            desiredHeight = Mathf.Clamp(FrameDistance(combined) * 1.25f, 18, 60);
            ClampFocus();
        }
        public void ResetToHeadquarters()
        {
            if (!match.Running || State != StrategyCameraState.Tactical) return;
            panVelocity = Vector3.zero;
            desiredFocus = new Vector3(0, 0, -12); desiredHeight = 37;
        }
        void ClampFocus() { desiredFocus.x = Mathf.Clamp(desiredFocus.x, -32, 32); desiredFocus.z = Mathf.Clamp(desiredFocus.z, -32, 32); }
        void OnDisable()
        {
            if (!Inspecting) return;
            if (view != null) view.transform.SetPositionAndRotation(savedPosition, savedRotation);
            if (match != null) match.SetInspectionPaused(false);
            State = StrategyCameraState.Tactical; target = null; orbiting = false;
        }
        void LateUpdate()
        {
            if (match == null || view == null || match.Waves == null) return;
            if (match.Waves.Result != MatchResult.Playing)
            {
                if (Inspecting) { view.transform.SetPositionAndRotation(savedPosition, savedRotation); match.SetInspectionPaused(false); }
                State = StrategyCameraState.Tactical; return;
            }
            var mouse = Mouse.current; var keys = Keyboard.current;
            if (Inspecting)
            {
                if ((target == null || (target.TryGetComponent<StrategyEntity>(out var e) && !e.Alive)) && State != StrategyCameraState.Returning) EndInspection();
                if (State == StrategyCameraState.Inspecting && mouse != null)
                {
                    if (mouse.leftButton.wasPressedThisFrame) orbiting = !commander.PointerOverUI;
                    if (!mouse.leftButton.isPressed) orbiting = false;
                    if (orbiting && !commander.PointerOverUI) { var delta = mouse.delta.ReadValue(); yaw += delta.x * orbitSensitivity; pitch = Mathf.Clamp(pitch - delta.y * orbitSensitivity, 15, 80); }
                    if (!commander.PointerOverUI) distance = Mathf.Clamp(distance - mouse.scroll.ReadValue().y * .0025f * distance, minimumDistance, Mathf.Max(minimumDistance * 6, FrameDistance(bounds) * 3));
                }
                Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
                Vector3 position = bounds.center - rotation * Vector3.forward * distance;
                position.y = Mathf.Max(position.y, view.nearClipPlane + .2f);
                if (State == StrategyCameraState.Returning) { position = savedPosition; rotation = savedRotation; }
                if (State == StrategyCameraState.Entering || State == StrategyCameraState.Returning)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / Mathf.Max(.01f, transitionSeconds)));
                    view.transform.SetPositionAndRotation(Vector3.Lerp(fromPosition, position, t), Quaternion.Slerp(fromRotation, rotation, t));
                    if (t >= 1)
                    {
                        if (State == StrategyCameraState.Returning) { State = StrategyCameraState.Tactical; target = null; match.SetInspectionPaused(false); }
                        else State = StrategyCameraState.Inspecting;
                    }
                }
                else view.transform.SetPositionAndRotation(position, rotation);
                return;
            }
            if (!match.Running) return;
            if (keys != null)
            {
                if (keys.cKey.wasPressedThisFrame) FocusSelection();
                if (keys.homeKey.wasPressedThisFrame) ResetToHeadquarters();
                float x = (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0) - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0);
                float z = (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0) - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0);
                float panBlend = 1 - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(.01f, smoothingSeconds));
                panVelocity = Vector3.Lerp(panVelocity, new Vector3(x, 0, z).normalized * (height * .7f), panBlend);
                if (x != 0 || z != 0 || panVelocity.sqrMagnitude > .01f)
                {
                    desiredFocus = focus + panVelocity * Time.unscaledDeltaTime;
                    focus = desiredFocus;
                }
            }
            if (mouse != null && !commander.PointerOverUI) desiredHeight = Mathf.Clamp(desiredHeight - mouse.scroll.ReadValue().y * .025f, 18, 60);
            ClampFocus();
            float blend = 1 - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(.01f, smoothingSeconds));
            focus = Vector3.Lerp(focus, desiredFocus, blend); focus.x = Mathf.Clamp(focus.x, -32, 32); focus.z = Mathf.Clamp(focus.z, -32, 32); height = Mathf.Lerp(height, desiredHeight, blend);
            view.transform.SetPositionAndRotation(focus + new Vector3(0, height, -height * .65f), Quaternion.Euler(57, 0, 0));
        }
    }
}
