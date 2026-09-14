using UnityEngine;
namespace Engchanok.StrategyGame
{
    [CreateAssetMenu(menuName = "Strategy Game/Settings")]
    public sealed class StrategySettings : ScriptableObject
    {
        [Header("Economy")]
        public int startingMinerals = 250;
        public int depositMinerals = 1400, workerCapacity = 20;
        public float miningSeconds = 2;
        public int barracksCost = 150, turretCost = 100, rangerPostCost = 140, supportBayCost = 175;
        [Header("Supply")]
        public int supplyLimit = 60;
        public int headquartersSupply = 10, producerSupply = 4, relaySupply = 8;
        public int relayCost = 75;
        [Header("Support")]
        public float medicHealPerSecond = 9, engineerRepairPerSecond = 22;
        public int repairMineralsPerHundredHealth = 20;
        [Header("Research")]
        public int miningResearchCost = 125, soldierResearchCost = 150, turretResearchCost = 150;
        public float miningResearchSeconds = 20, soldierResearchSeconds = 25, turretResearchSeconds = 25;
        public float miningResearchBonus = .5f, soldierResearchBonus = .25f, turretResearchBonus = .25f;
        public int ResearchCost(UpgradeKind kind) => kind == UpgradeKind.Mining ? miningResearchCost : kind == UpgradeKind.SoldierWeapons ? soldierResearchCost : turretResearchCost;
        public float ResearchSeconds(UpgradeKind kind) => kind == UpgradeKind.Mining ? miningResearchSeconds : kind == UpgradeKind.SoldierWeapons ? soldierResearchSeconds : turretResearchSeconds;
        public static string ResearchName(UpgradeKind kind) => kind == UpgradeKind.Mining ? "Improved Mining" : kind == UpgradeKind.SoldierWeapons ? "Soldier Weapons" : "Turret Weapons";
        [Header("Map and waves")]
        public float mapHalfSize = 40, buildRadius = 23;
        public int waveCount = 5, firstWaveEnemies = 5, extraEnemiesPerWave = 4;
        public float preparationSeconds = 55, betweenWaveSeconds = 32;
        [Header("Combat")]
        public float headquartersHealth = 1200, buildingHealth = 350, enemyHealth = 90;
        public float turretDamage = 22, enemyDamage = 12;
        public float turretRange = 12, enemyRange = 2;
        public float attackInterval = 1, enemySpeed = 3.5f;
        [Header("Enemy variants (multipliers)")]
        public float runnerHealth = .6f, runnerSpeed = 1.6f, runnerDamage = .7f;
        public float bruteHealth = 2.5f, bruteSpeed = .65f, bruteDamage = 1.6f;
        [Header("Unit roster")]
        public UnitProfile[] units;
        [Header("Armor and damage counters")]
        public ArmorProfile[] armorProfiles;
        public UnitProfile Profile(EntityKind kind)
        {
            if (units != null) foreach (var profile in units) if (profile != null && profile.kind == kind) return profile;
            return null;
        }
        public ArmorProfile ArmorOf(EntityKind kind)
        {
            if (armorProfiles != null) foreach (var profile in armorProfiles) if (profile != null && profile.kind == kind) return profile;
            return null;
        }
        public bool IsProducer(EntityKind kind)
        {
            if (units != null) foreach (var profile in units) if (profile != null && profile.producer == kind) return true;
            return false;
        }
        public ArmorClass Armor(EntityKind kind) => ArmorOf(kind)?.armor ?? ArmorClass.Structure;
        public static string ArmorName(ArmorClass armor) => armor switch { ArmorClass.Light => "Light", ArmorClass.Medium => "Medium", ArmorClass.Heavy => "Heavy", _ => "Structure" };
        public static string Label(EntityKind kind) => kind switch { EntityKind.SupplyRelay => "Supply relay", EntityKind.RangerPost => "Ranger post", EntityKind.SupportBay => "Support bay", _ => kind.ToString() };
        // Armor now defends as well as counters: a Defender's Heavy class is what makes it a screen rather than a large health bar.
        // Structures are exempt in both directions, so headquarters and turret balance stay governed by their own numbers.
        public float DamageScale(EntityKind attacker, EntityKind target)
        {
            var armor = Armor(target);
            if (armor == ArmorClass.Structure) return 1;
            return ArmorOf(attacker)?.Scale(armor) ?? 1;
        }
        public WaveComposition[] waveCompositions;
        public WaveComposition Composition(int wave) => waveCompositions != null && wave > 0 && wave <= waveCompositions.Length && waveCompositions[wave - 1] != null
            ? waveCompositions[wave - 1] : new WaveComposition(firstWaveEnemies + (wave - 1) * extraEnemiesPerWave, 0, 0);
        public float EnemySpeed(EntityKind kind) => enemySpeed * (kind == EntityKind.Runner ? runnerSpeed : kind == EntityKind.Brute ? bruteSpeed : 1);
        public float EnemyDamage(EntityKind kind) => enemyDamage * (kind == EntityKind.Runner ? runnerDamage : kind == EntityKind.Brute ? bruteDamage : 1);
        public int Supply(EntityKind kind) => Profile(kind)?.supply ?? 0;
        public int SupplyProvided(EntityKind kind) => kind == EntityKind.Headquarters ? headquartersSupply : kind == EntityKind.SupplyRelay ? relaySupply : IsProducer(kind) ? producerSupply : 0;
        public float TrainSeconds(EntityKind kind) => Profile(kind)?.trainSeconds ?? 1;
        public float Damage(EntityKind kind) => Profile(kind)?.damage ?? (kind == EntityKind.Turret ? turretDamage : EnemyDamage(kind));
        public float Range(EntityKind kind) => Profile(kind)?.range ?? (kind == EntityKind.Turret ? turretRange : enemyRange);
        public float Speed(EntityKind kind) => Profile(kind)?.speed ?? EnemySpeed(kind);
        public int Cost(EntityKind kind) => Profile(kind)?.cost ?? kind switch { EntityKind.Barracks => barracksCost, EntityKind.Turret => turretCost, EntityKind.SupplyRelay => relayCost, EntityKind.RangerPost => rangerPostCost, EntityKind.SupportBay => supportBayCost, _ => 0 };
        public float Health(EntityKind kind) => Profile(kind)?.health ?? kind switch { EntityKind.Headquarters => headquartersHealth, EntityKind.Enemy => enemyHealth, EntityKind.Runner => enemyHealth * runnerHealth, EntityKind.Brute => enemyHealth * bruteHealth, _ => buildingHealth };
        public float Radius(EntityKind kind) => kind switch { EntityKind.Headquarters => 2.7f, EntityKind.Barracks => 2.2f, EntityKind.RangerPost => 2.2f, EntityKind.SupportBay => 2.2f, EntityKind.SupplyRelay => 1.5f, EntityKind.Turret => 1.2f, _ => .5f };
    }
}
