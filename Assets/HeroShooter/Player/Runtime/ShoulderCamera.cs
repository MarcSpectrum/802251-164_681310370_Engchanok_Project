using UnityEngine;

namespace Engchanok.HeroShooter
{
    [RequireComponent(typeof(Camera))]
    public sealed class ShoulderCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private HeroInput input;
        [SerializeField] private LayerMask collisionMask = 1;
        [SerializeField] private float sensitivity = 0.08f;
        [SerializeField] private float normalDistance = 5f;
        [SerializeField] private float aimDistance = 3.4f;
        [SerializeField] private float shoulderOffset = 0.75f;
        [SerializeField] private float targetHeight = 1.45f;
        [SerializeField] private float collisionRadius = 0.2f;
        [SerializeField] private Vector2 pitchLimits = new(-35f, 70f);

        private float _yaw;
        private float _pitch = 12f;

        private void LateUpdate()
        {
            if (target == null || input == null) return;
            Vector2 look = input.Look;
            _yaw += look.x * sensitivity;
            _pitch = Mathf.Clamp(_pitch - look.y * sensitivity, pitchLimits.x, pitchLimits.y);

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 pivot = target.position + Vector3.up * targetHeight;
            float distance = input.AimHeld ? aimDistance : normalDistance;
            Vector3 desired = pivot + rotation * new Vector3(shoulderOffset, 0.25f, -distance);

            Vector3 cast = desired - pivot;
            if (Physics.SphereCast(pivot, collisionRadius, cast.normalized, out RaycastHit hit,
                    cast.magnitude, collisionMask, QueryTriggerInteraction.Ignore))
                desired = pivot + cast.normalized * Mathf.Max(0.1f, hit.distance - collisionRadius);

            transform.SetPositionAndRotation(desired, rotation);
        }
    }
}
