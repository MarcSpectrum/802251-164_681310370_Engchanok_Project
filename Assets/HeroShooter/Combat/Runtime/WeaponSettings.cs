using UnityEngine;

namespace Engchanok.HeroShooter
{
    [CreateAssetMenu(menuName = "Hero Shooter/Weapon Settings")]
    public sealed class WeaponSettings : ScriptableObject
    {
        [Min(0f)] public float damage = 25f;
        [Min(1f)] public float range = 100f;
        [Min(0f)] public float shotInterval = 0.18f;
        [Min(1)] public int magazineSize = 12;
        [Min(0f)] public float reloadDuration = 1.5f;
        public LayerMask hitMask = ~0;
    }
}
