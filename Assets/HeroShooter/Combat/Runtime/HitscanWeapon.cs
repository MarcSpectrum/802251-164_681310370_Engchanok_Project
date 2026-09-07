using UnityEngine;

namespace Engchanok.HeroShooter
{
    public sealed class HitscanWeapon : MonoBehaviour
    {
        [SerializeField] private HeroInput input;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private Transform muzzle;
        [SerializeField] private WeaponSettings settings;

        private MagazineModel _magazine;
        private bool _hasValidTarget;
        private float _hitMarkerUntil;

        public int Ammo => _magazine?.Ammo ?? 0;
        public int MagazineSize => _magazine?.Capacity ?? 0;
        public bool IsReloading => _magazine?.IsReloading == true;
        public bool HasValidTarget => _hasValidTarget;
        public bool ShowHitMarker => Time.unscaledTime < _hitMarkerUntil;

        private void Awake()
        {
            if (settings != null)
                _magazine = new MagazineModel(settings.magazineSize, settings.shotInterval, settings.reloadDuration);
        }

        private void Update()
        {
            if (_magazine == null || input == null || aimCamera == null) return;
            _magazine.CompleteReload(Time.time);
            _hasValidTarget = TryGetAimHit(out _, out IDamageable target) && target.IsTargetable;

            if (input.ReloadPressed) _magazine.StartReload(Time.time);
            if (input.FirePressed) Fire();
        }

        public void Fire()
        {
            if (!_magazine.TryFire(Time.time)) return;

            Ray aimRay = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            Vector3 aimPoint = aimRay.GetPoint(settings.range);
            if (Physics.Raycast(aimRay, out RaycastHit aimHit, settings.range, settings.hitMask,
                    QueryTriggerInteraction.Ignore))
                aimPoint = aimHit.point;

            Vector3 origin = muzzle != null ? muzzle.position : transform.position;
            Vector3 direction = aimPoint - origin;
            float distance = Mathf.Min(direction.magnitude, settings.range);
            if (distance <= 0.001f) return;

            if (Physics.Raycast(origin, direction.normalized, out RaycastHit shotHit, distance,
                    settings.hitMask, QueryTriggerInteraction.Ignore) &&
                TryFindDamageable(shotHit.collider, out IDamageable damageable) && damageable.IsTargetable)
            {
                damageable.ApplyDamage(new DamageInfo(settings.damage, shotHit.point, shotHit.normal, gameObject));
                _hitMarkerUntil = Time.unscaledTime + 0.1f;
            }

            if (_magazine.Ammo == 0) _magazine.StartReload(Time.time);
        }

        private bool TryGetAimHit(out RaycastHit hit, out IDamageable damageable)
        {
            damageable = null;
            Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            if (!Physics.Raycast(ray, out hit, settings.range, settings.hitMask, QueryTriggerInteraction.Ignore))
                return false;
            return TryFindDamageable(hit.collider, out damageable);
        }

        private static bool TryFindDamageable(Collider hitCollider, out IDamageable damageable)
        {
            MonoBehaviour[] behaviours = hitCollider.GetComponentsInParent<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IDamageable found)
                {
                    damageable = found;
                    return true;
                }
            }
            damageable = null;
            return false;
        }
    }
}
