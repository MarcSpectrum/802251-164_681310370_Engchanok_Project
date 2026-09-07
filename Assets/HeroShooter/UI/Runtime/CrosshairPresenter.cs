using UnityEngine;
using UnityEngine.UI;

namespace Engchanok.HeroShooter
{
    public sealed class CrosshairPresenter : MonoBehaviour
    {
        [SerializeField] private HitscanWeapon weapon;
        [SerializeField] private Image top;
        [SerializeField] private Image bottom;
        [SerializeField] private Image left;
        [SerializeField] private Image right;
        [SerializeField] private Image centerDot;
        [SerializeField] private Color neutralColor = Color.white;
        [SerializeField] private Color targetColor = new(1f, 0.15f, 0.12f);

        private void Update()
        {
            if (weapon == null) return;
            bool target = weapon.HasValidTarget;
            float gap = target ? 5f : 10f;
            Color color = target ? targetColor : neutralColor;
            Set(top, color, new Vector2(0f, gap + 6f));
            Set(bottom, color, new Vector2(0f, -gap - 6f));
            Set(left, color, new Vector2(-gap - 6f, 0f));
            Set(right, color, new Vector2(gap + 6f, 0f));
            centerDot.gameObject.SetActive(target || weapon.ShowHitMarker);
            centerDot.color = weapon.ShowHitMarker ? Color.white : color;
        }

        private static void Set(Image image, Color color, Vector2 position)
        {
            image.color = color;
            image.rectTransform.anchoredPosition = position;
        }
    }
}
