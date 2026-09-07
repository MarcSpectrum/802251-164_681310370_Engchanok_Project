using System.Collections;
using UnityEngine;

namespace Engchanok.HeroShooter
{
    public sealed class TargetDummy : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        [SerializeField, Min(0f)] private float respawnDelay = 3f;
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Collider[] colliders;

        private HealthModel _health;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private Color[] _baseColors;
        private Coroutine _feedback;

        public bool IsTargetable => _health?.IsAlive == true;
        public float CurrentHealth => _health?.Current ?? 0f;

        private void Awake()
        {
            if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<Renderer>(true);
            if (colliders == null || colliders.Length == 0) colliders = GetComponentsInChildren<Collider>(true);
            _baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) _baseColors[i] = renderers[i].material.color;
            _health = new HealthModel(maximumHealth);
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
        }

        public void ApplyDamage(DamageInfo damage)
        {
            if (_health.Damage(damage.Amount) <= 0f) return;
            if (_feedback != null) StopCoroutine(_feedback);
            _feedback = StartCoroutine(HitFeedback());
            if (!_health.IsAlive) StartCoroutine(RespawnRoutine());
        }

        private IEnumerator HitFeedback()
        {
            foreach (Renderer item in renderers) item.material.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            RestoreColors();
            _feedback = null;
        }

        private IEnumerator RespawnRoutine()
        {
            foreach (Renderer item in renderers) item.enabled = false;
            foreach (Collider item in colliders) item.enabled = false;
            yield return new WaitForSeconds(respawnDelay);
            transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
            _health.Reset(maximumHealth);
            RestoreColors();
            foreach (Renderer item in renderers) item.enabled = true;
            foreach (Collider item in colliders) item.enabled = true;
        }

        private void RestoreColors()
        {
            for (int i = 0; i < renderers.Length; i++) renderers[i].material.color = _baseColors[i];
        }
    }
}
