using UnityEngine;

namespace Engchanok.HeroShooter
{
    [CreateAssetMenu(menuName = "Hero Shooter/Movement Settings")]
    public sealed class MovementSettings : ScriptableObject
    {
        [Min(0f)] public float moveSpeed = 7f;
        [Min(1f)] public float sprintMultiplier = 1.45f;
        [Min(0f)] public float groundAcceleration = 45f;
        [Min(0f)] public float airAcceleration = 16f;
        [Min(0f)] public float jumpHeight = 1.8f;
        [Min(0f)] public float gravity = 25f;
        [Min(0f)] public float rotationSpeed = 18f;
        [Min(0f)] public float dashSpeed = 20f;
        [Min(0.01f)] public float dashDuration = 0.18f;
        [Min(0f)] public float dashCooldown = 1.25f;
    }
}
