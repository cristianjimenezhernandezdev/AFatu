using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyBehaviorConfigData
{
    public float aggression = 1f;
    public float avoidance;
    public bool ranged;
    public int range = 1;
}

[Serializable]
public class EnemyRewardConfigData
{
    public int gold;
}

[Serializable]
public class ModifierEffectConfigData
{
    public float extraObstacleChance;
    public bool slowZones;
    public float heroSpeedMultiplier = 1f;
    public int healOnEnter;
    public int enemyAttackBonus;
    public float eliteChanceBonus;
}

[Serializable]
public class DivinePowerEffectConfigData
{
    public float speedMultiplier = 1f;
    public int attackBonus;
    public int defenseBonus;
    public string heroMode;
    public bool spawnCompanion;
    public int durationSegments;
}

[Serializable]
public class ConsumableEffectConfigData
{
    public int heal;
    public bool skipEncounter;
    public float speedMultiplier = 1f;
    public int defenseBonus;
    public int durationSeconds;
}

[Serializable]
public class RelicEffectConfigData
{
    public int heroMaxHealthBonus;
    public int heroAttackBonus;
    public float heroSpeedBonus;
    public int extraCardChoice;
}

[Serializable]
public class ShopOfferEffectConfigData
{
    public int heal;
    public int attackBonus;
    public int defenseBonus;
    public float speedMultiplierBonus = 1f;
    public int durationSegments;
    public bool rerollCards;
    public bool spawnCompanion;
}

[Serializable]
public class SegmentModifierRuntimeData
{
    public float heroSpeedMultiplier = 1f;
    public int heroHealOnEnter;
    public int enemyAttackBonus;
    public float eliteChanceBonus;
}

[Serializable]
public class GeneratedEnemySpawnData
{
    public EnemyArchetypeSeedData archetype;
    public RunSegmentEnemyData segmentEnemy;
    public Vector2Int gridPosition;
    public EnemyBehaviorConfigData behavior = new EnemyBehaviorConfigData();
    public EnemyRewardConfigData reward = new EnemyRewardConfigData();
}

[Serializable]
public class ChestRewardRuntimeData
{
    public int gold;
    public int emeralds;
    public string chestTier = "small";
}

[Serializable]
public class GeneratedChestSpawnData
{
    public Vector2Int gridPosition;
    public ChestRewardRuntimeData reward = new ChestRewardRuntimeData();
}

[Serializable]
public class SegmentRuntimeData
{
    public RunSegmentData segment;
    public CardSeedData card;
    public BiomeSeedData biome;
    public Color floorColor = Color.gray;
    public Color wallColor = Color.black;
    public List<Vector2Int> wallPositions = new List<Vector2Int>();
    public List<GeneratedEnemySpawnData> enemySpawns = new List<GeneratedEnemySpawnData>();
    public List<GeneratedChestSpawnData> chestSpawns = new List<GeneratedChestSpawnData>();
    public List<WorldModifierSeedData> appliedModifiers = new List<WorldModifierSeedData>();
    public SegmentModifierRuntimeData modifierRuntime = new SegmentModifierRuntimeData();

    public Vector2Int EntryPosition => new Vector2Int(segment != null ? segment.entryX : 1, (segment != null ? segment.segmentHeight : 1) / 2);
    public Vector2Int ExitPosition => new Vector2Int(segment != null ? segment.exitX : 1, (segment != null ? segment.segmentHeight : 1) / 2);
}

[Serializable]
public class ShopOfferData
{
    public string offerId;
    public string title;
    public string description;
    public string artKey;
    public string offerType;
    public string rewardType;
    public string rewardId;
    public int quantity = 1;
    public int durationSegments;
    public int cost;
    public float value;
    public string effectConfigJson;
}
