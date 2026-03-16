using System;
using System.Collections.Generic;

[Serializable]
public class RunSessionData
{
    public long runId;
    public string playerId = BalanceConfig.LocalPlayerId;
    public string runSeed;
    public string status = "active";
    public string startingCardId;
    public int targetSegmentCount = BalanceConfig.DefaultShortRunLength;
    public int currentSegmentIndex = 1;
    public int segmentsCleared;
    public string heroMode = BalanceConfig.DefaultHeroMode;
    public int heroMaxHealth = BalanceConfig.HeroBaseMaxHealth;
    public int heroCurrentHealth = BalanceConfig.HeroBaseMaxHealth;
    public int heroAttack = BalanceConfig.HeroBaseAttack;
    public int heroDefense = BalanceConfig.HeroBaseDefense;
    public float heroSpeed = BalanceConfig.HeroBaseSpeed;
    public int cardsUnlockedThisRun;
    public int goldEarned;
    public int goldSpent;
    public bool shopAvailable;
    public List<string> equippedDivinePowerIds = new List<string>();
}

[Serializable]
public class RunSegmentData
{
    public long runSegmentId;
    public long runId;
    public int segmentIndex;
    public string cardId;
    public string biomeId;
    public string floorColorHex;
    public string wallColorHex;
    public int segmentWidth;
    public int segmentHeight;
    public int entryX;
    public int exitX;
    public float obstacleChance;
    public float enemyChance;
    public string generatedSeed;
    public float difficultyMultiplier = 1f;
    public string state = "generated";
    public int heroHealthOnEnter;
    public int heroHealthOnExit;
    public string cardType = "balanced";
    public int rewardTier = 1;
    public List<string> appliedModifierIds = new List<string>();
    public List<RunSegmentEnemyData> enemies = new List<RunSegmentEnemyData>();
}

[Serializable]
public class RunSegmentChoiceData
{
    public long runId;
    public int segmentIndex;
    public int choiceSlot;
    public string offeredCardId;
    public bool wasSelected;
}

[Serializable]
public class RunSegmentEnemyData
{
    public long runSegmentEnemyId;
    public long runSegmentId;
    public string enemyId;
    public int spawnX;
    public int spawnY;
    public int spawnedMaxHealth;
    public int spawnedAttack;
    public int spawnedDefense;
    public float spawnedSpeed;
    public bool defeated;
    public string metadataJson;
}

[Serializable]
public class RunRewardData
{
    public long runRewardId;
    public long runId;
    public long runSegmentId;
    public string rewardType;
    public string rewardId;
    public int quantity;
    public string metadataJson;
}

[Serializable]
public class RunEventData
{
    public long runEventId;
    public long runId;
    public long runSegmentId;
    public string eventType;
    public int eventOrder;
    public string payloadJson;
}

[Serializable]
public class HeroStatsData
{
    public int maxHealth = BalanceConfig.HeroBaseMaxHealth;
    public int currentHealth = BalanceConfig.HeroBaseMaxHealth;
    public int attack = BalanceConfig.HeroBaseAttack;
    public int defense = BalanceConfig.HeroBaseDefense;
    public float speed = BalanceConfig.HeroBaseSpeed;
}

[Serializable]
public class PendingHeroBonusData
{
    public int attackBonus;
    public int defenseBonus;
    public int maxHealthBonus;
    public float speedMultiplierBonus;
    public int durationSegments;
    public string sourceId;

    public bool IsActive => durationSegments > 0 && (attackBonus != 0 || defenseBonus != 0 || maxHealthBonus != 0 || Math.Abs(speedMultiplierBonus) > 0.001f);
}
