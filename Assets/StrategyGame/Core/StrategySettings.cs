using UnityEngine;
namespace Engchanok.StrategyGame
{
    [CreateAssetMenu(menuName = "Strategy Game/Settings")]
    public sealed class StrategySettings : ScriptableObject
    {
        [Header("Economy")]
        public int startingMinerals = 250;
        public int depositMinerals = 6000, workerCapacity = 20;
        public float miningSeconds = 2;
        public int workerCost = 50, soldierCost = 75, barracksCost = 150, turretCost = 100;
        public float workerTraining = 6, soldierTraining = 8;
        [Header("Map and waves")]
        public float mapHalfSize = 40, buildRadius = 23;
        public int waveCount = 5, firstWaveEnemies = 5, extraEnemiesPerWave = 4;
        public float preparationSeconds = 65, betweenWaveSeconds = 40;
        [Header("Combat")]
        public float headquartersHealth = 1600, workerHealth = 70, soldierHealth = 120, buildingHealth = 350, enemyHealth = 90;
        public float soldierDamage = 15, turretDamage = 22, enemyDamage = 10;
        public float soldierRange = 7, turretRange = 12, enemyRange = 2;
        public float attackInterval = 1, workerSpeed = 5, soldierSpeed = 5, enemySpeed = 3.5f;
        [Header("Enemy variants (multipliers)")]
        public float runnerHealth = .6f, runnerSpeed = 1.6f, runnerDamage = .7f;
        public float bruteHealth = 2.5f, bruteSpeed = .65f, bruteDamage = 1.6f;
        public WaveComposition[] waveCompositions;
        public WaveComposition Composition(int wave) => waveCompositions != null && wave > 0 && wave <= waveCompositions.Length && waveCompositions[wave - 1] != null
            ? waveCompositions[wave - 1] : new WaveComposition(firstWaveEnemies + (wave - 1) * extraEnemiesPerWave, 0, 0);
        public float EnemySpeed(EntityKind kind) => enemySpeed * (kind == EntityKind.Runner ? runnerSpeed : kind == EntityKind.Brute ? bruteSpeed : 1);
        public float EnemyDamage(EntityKind kind) => enemyDamage * (kind == EntityKind.Runner ? runnerDamage : kind == EntityKind.Brute ? bruteDamage : 1);
        public int Cost(EntityKind kind) => kind switch { EntityKind.Worker => workerCost, EntityKind.Soldier => soldierCost, EntityKind.Barracks => barracksCost, EntityKind.Turret => turretCost, _ => 0 };
        public float Health(EntityKind kind) => kind switch { EntityKind.Headquarters => headquartersHealth, EntityKind.Worker => workerHealth, EntityKind.Soldier => soldierHealth, EntityKind.Enemy => enemyHealth, EntityKind.Runner => enemyHealth * runnerHealth, EntityKind.Brute => enemyHealth * bruteHealth, _ => buildingHealth };
        public float Radius(EntityKind kind) => kind switch { EntityKind.Headquarters => 2.7f, EntityKind.Barracks => 2.2f, EntityKind.Turret => 1.2f, _ => .5f };
    }
}
