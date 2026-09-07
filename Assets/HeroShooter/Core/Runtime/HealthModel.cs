using System;

namespace Engchanok.HeroShooter
{
    [Serializable]
    public sealed class HealthModel
    {
        public HealthModel(float maximum) => Reset(maximum);

        public float Maximum { get; private set; }
        public float Current { get; private set; }
        public bool IsAlive => Current > 0f;

        public float Damage(float amount)
        {
            if (!IsAlive || amount <= 0f) return 0f;
            float previous = Current;
            Current = Math.Max(0f, Current - amount);
            return previous - Current;
        }

        public void Reset(float maximum)
        {
            Maximum = Math.Max(1f, maximum);
            Current = Maximum;
        }
    }
}
