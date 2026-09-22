using UnityEngine;
using UnityEngine.InputSystem;
namespace Engchanok.StrategyGame
{

    public sealed class StrategyCameraController : MonoBehaviour
    {
        public StrategyMatch match;
        public StrategyCommander commander;
        public Camera view;
        [Min(.01f)] public float smoothingSeconds = .12f;
        Vector3 focus = new(0, 0, -12), desiredFocus = new(0, 0, -12);
        float height = 37, desiredHeight = 37;
        Vector3 panVelocity;

        public static Bounds VisualBounds(Transform root)
        {
            var result = new Bounds(root.position + Vector3.up, Vector3.one * 2); bool found = false;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (!(renderer is MeshRenderer || renderer is SkinnedMeshRenderer) || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
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
            if (!match.Running) return;
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
            if (!match.Running) return;
            panVelocity = Vector3.zero;
            desiredFocus = new Vector3(0, 0, -12); desiredHeight = 37;
        }
        public Vector3 Focus => focus;
        // Minimap and alert jumps move immediately so dragging across the map tracks the pointer; zoom is left alone.
        public void JumpTo(Vector3 point)
        {
            if (!match.Running) return;
            panVelocity = Vector3.zero;
            desiredFocus = new Vector3(point.x, 0, point.z); ClampFocus();
            focus = desiredFocus;
        }
        void ClampFocus() { desiredFocus.x = Mathf.Clamp(desiredFocus.x, -32, 32); desiredFocus.z = Mathf.Clamp(desiredFocus.z, -32, 32); }
        void LateUpdate()
        {
            if (match == null || view == null || match.Waves == null) return;
            var mouse = Mouse.current; var keys = Keyboard.current;
            if (!match.Running) return;
            if (keys != null)
            {
                if (keys.cKey.wasPressedThisFrame) FocusSelection();
                if (keys.homeKey.wasPressedThisFrame) ResetToHeadquarters();
                if (keys.spaceKey.wasPressedThisFrame && match.HasAlert) JumpTo(match.LastAlertPosition);
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
