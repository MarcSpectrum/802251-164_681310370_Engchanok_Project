using System;

namespace Engchanok.HeroShooter
{
    [Serializable]
    public sealed class CooldownTimer
    {
        private float _remaining;

        public float Remaining => Math.Max(0f, _remaining);
        public bool IsReady => _remaining <= 0f;

        public bool TryUse(float duration)
        {
            if (!IsReady) return false;
            _remaining = Math.Max(0f, duration);
            return true;
        }

        public void Tick(float deltaTime)
        {
            _remaining = Math.Max(0f, _remaining - Math.Max(0f, deltaTime));
        }

        public void Reset() => _remaining = 0f;
    }
}
