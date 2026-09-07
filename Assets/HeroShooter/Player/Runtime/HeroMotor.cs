using UnityEngine;

namespace Engchanok.HeroShooter
{
    [RequireComponent(typeof(CharacterController), typeof(HeroInput))]
    public sealed class HeroMotor : MonoBehaviour
    {
        [SerializeField] private MovementSettings settings;
        [SerializeField] private HeroInput input;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform visualRoot;

        private CharacterController _controller;
        private readonly CooldownTimer _dashCooldown = new();
        private Vector3 _horizontalVelocity;
        private Vector3 _dashDirection;
        private float _verticalVelocity;
        private float _dashRemaining;

        public float DashCooldownRemaining => _dashCooldown.Remaining;
        public float DashCooldownDuration => settings != null ? settings.dashCooldown : 0f;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (input == null) input = GetComponent<HeroInput>();
            if (visualRoot == null) visualRoot = transform;
        }

        private void Update()
        {
            if (settings == null || cameraTransform == null) return;
            float dt = Time.deltaTime;
            _dashCooldown.Tick(dt);

            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            Vector2 moveInput = Vector2.ClampMagnitude(input.Move, 1f);
            Vector3 desiredDirection = (forward * moveInput.y + right * moveInput.x).normalized;

            if (input.DashPressed && _dashCooldown.TryUse(settings.dashCooldown))
            {
                _dashDirection = desiredDirection.sqrMagnitude > 0.01f ? desiredDirection : forward;
                _dashRemaining = settings.dashDuration;
            }

            Vector3 horizontal;
            if (_dashRemaining > 0f)
            {
                _dashRemaining -= dt;
                horizontal = _dashDirection * settings.dashSpeed;
            }
            else
            {
                float speed = settings.moveSpeed * (input.SprintHeld ? settings.sprintMultiplier : 1f);
                Vector3 desiredVelocity = desiredDirection * speed;
                float acceleration = _controller.isGrounded ? settings.groundAcceleration : settings.airAcceleration;
                _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, desiredVelocity, acceleration * dt);
                horizontal = _horizontalVelocity;
            }

            if (_controller.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            if (_controller.isGrounded && input.JumpPressed)
                _verticalVelocity = Mathf.Sqrt(2f * settings.gravity * settings.jumpHeight);
            _verticalVelocity -= settings.gravity * dt;

            _controller.Move((horizontal + Vector3.up * _verticalVelocity) * dt);

            Vector3 facing = input.AimHeld ? forward : desiredDirection;
            if (facing.sqrMagnitude > 0.01f)
            {
                Quaternion target = Quaternion.LookRotation(facing, Vector3.up);
                visualRoot.rotation = Quaternion.Slerp(visualRoot.rotation, target, 1f - Mathf.Exp(-settings.rotationSpeed * dt));
            }
        }
    }
}
