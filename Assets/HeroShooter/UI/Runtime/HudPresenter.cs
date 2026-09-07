using UnityEngine;
using UnityEngine.UI;

namespace Engchanok.HeroShooter
{
    public sealed class HudPresenter : MonoBehaviour
    {
        [SerializeField] private HitscanWeapon weapon;
        [SerializeField] private HeroMotor motor;
        [SerializeField] private Text ammoText;
        [SerializeField] private Text dashText;

        private void Update()
        {
            if (weapon != null)
                ammoText.text = weapon.IsReloading ? "RELOADING" : $"{weapon.Ammo:00} / {weapon.MagazineSize:00}";
            if (motor != null)
                dashText.text = motor.DashCooldownRemaining <= 0f ? "DASH  READY" : $"DASH  {motor.DashCooldownRemaining:0.0}s";
        }
    }
}
