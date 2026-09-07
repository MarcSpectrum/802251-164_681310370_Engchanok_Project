using System;

namespace Engchanok.HeroShooter
{
    [Serializable]
    public sealed class MagazineModel
    {
        private readonly int _capacity;
        private readonly float _shotInterval;
        private readonly float _reloadDuration;
        private float _nextShotTime;
        private float _reloadCompleteTime;

        public MagazineModel(int capacity, float shotInterval, float reloadDuration)
        {
            _capacity = Math.Max(1, capacity);
            _shotInterval = Math.Max(0f, shotInterval);
            _reloadDuration = Math.Max(0f, reloadDuration);
            Ammo = _capacity;
        }

        public int Ammo { get; private set; }
        public int Capacity => _capacity;
        public bool IsReloading { get; private set; }

        public bool TryFire(float currentTime)
        {
            CompleteReload(currentTime);
            if (IsReloading || Ammo <= 0 || currentTime < _nextShotTime) return false;
            Ammo--;
            _nextShotTime = currentTime + _shotInterval;
            return true;
        }

        public bool StartReload(float currentTime)
        {
            CompleteReload(currentTime);
            if (IsReloading || Ammo >= _capacity) return false;
            IsReloading = true;
            _reloadCompleteTime = currentTime + _reloadDuration;
            return true;
        }

        public bool CompleteReload(float currentTime)
        {
            if (!IsReloading || currentTime < _reloadCompleteTime) return false;
            Ammo = _capacity;
            IsReloading = false;
            return true;
        }

        public void Reset()
        {
            Ammo = _capacity;
            IsReloading = false;
            _nextShotTime = 0f;
            _reloadCompleteTime = 0f;
        }
    }
}
