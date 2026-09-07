using UnityEngine;
using UnityEngine.InputSystem;

namespace Engchanok.HeroShooter
{
    public sealed class HeroInput : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        private InputActionMap _player;
        private InputAction _move;
        private InputAction _look;
        private InputAction _attack;
        private InputAction _jump;
        private InputAction _sprint;
        private InputAction _dash;
        private InputAction _aim;
        private InputAction _reload;
        private InputAction _pause;

        public Vector2 Move => _move?.ReadValue<Vector2>() ?? Vector2.zero;
        public Vector2 Look => _look?.ReadValue<Vector2>() ?? Vector2.zero;
        public bool SprintHeld => _sprint?.IsPressed() == true;
        public bool AimHeld => _aim?.IsPressed() == true;
        public bool JumpPressed => _jump?.WasPressedThisFrame() == true;
        public bool DashPressed => _dash?.WasPressedThisFrame() == true;
        public bool FirePressed => _attack?.WasPressedThisFrame() == true;
        public bool ReloadPressed => _reload?.WasPressedThisFrame() == true;
        public bool PausePressed => _pause?.WasPressedThisFrame() == true;

        private void Awake()
        {
            if (actions == null)
            {
                Debug.LogError("HeroInput requires an InputActionAsset.", this);
                enabled = false;
                return;
            }

            _player = actions.FindActionMap("Player", true);
            _move = _player.FindAction("Move", true);
            _look = _player.FindAction("Look", true);
            _attack = _player.FindAction("Attack", true);
            _jump = _player.FindAction("Jump", true);
            _sprint = _player.FindAction("Sprint", true);
            _dash = _player.FindAction("Dash", true);
            _aim = _player.FindAction("Aim", true);
            _reload = _player.FindAction("Reload", true);
            _pause = _player.FindAction("Pause", true);
        }

        private void OnEnable() => _player?.Enable();
        private void OnDisable() => _player?.Disable();
    }
}
