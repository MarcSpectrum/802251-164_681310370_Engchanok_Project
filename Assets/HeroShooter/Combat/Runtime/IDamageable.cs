namespace Engchanok.HeroShooter
{
    public interface IDamageable
    {
        bool IsTargetable { get; }
        void ApplyDamage(DamageInfo damage);
    }
}
