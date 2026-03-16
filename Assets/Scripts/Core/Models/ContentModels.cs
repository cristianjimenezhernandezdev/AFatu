using System;

[Serializable]
public class LocalContentSeed
{
    public BiomeSeedData[] biomes = Array.Empty<BiomeSeedData>();
    public EnemyArchetypeSeedData[] enemies = Array.Empty<EnemyArchetypeSeedData>();
    public WorldModifierSeedData[] modifiers = Array.Empty<WorldModifierSeedData>();
    public RelicSeedData[] relics = Array.Empty<RelicSeedData>();
    public ConsumableSeedData[] consumables = Array.Empty<ConsumableSeedData>();
    public DivinePowerSeedData[] divinePowers = Array.Empty<DivinePowerSeedData>();
    public ShopOfferSeedData[] shopOffers = Array.Empty<ShopOfferSeedData>();
    public RunResultSeedData[] runResults = Array.Empty<RunResultSeedData>();
    public CardSeedData[] cards = Array.Empty<CardSeedData>();
    public CardEnemyPoolSeedData[] cardEnemyPool = Array.Empty<CardEnemyPoolSeedData>();
    public CardModifierPoolSeedData[] cardModifierPool = Array.Empty<CardModifierPoolSeedData>();
    public CardRewardPoolSeedData[] cardRewardPool = Array.Empty<CardRewardPoolSeedData>();
}

[Serializable]
public class BiomeSeedData
{
    public string biomeId;
    public string displayName;
    public string description;
    public string floorColorHex;
    public string wallColorHex;
    public string ambientConfigJson;
    public bool isActive = true;
}

[Serializable]
public class EnemyArchetypeSeedData
{
    public string enemyId;
    public string displayName;
    public string description;
    public int maxHealth;
    public int attack;
    public int defense;
    public float speed = 1f;
    public string movementPattern;
    public string rarity;
    public string spriteKey;
    public string behaviorConfigJson;
    public string rewardConfigJson;
    public bool isActive = true;
}

[Serializable]
public class WorldModifierSeedData
{
    public string modifierId;
    public string displayName;
    public string description;
    public string artKey;
    public string modifierType;
    public string rarity;
    public string stackMode;
    public string effectConfigJson;
    public bool isPositive;
    public bool isActive = true;
}

[Serializable]
public class RelicSeedData
{
    public string relicId;
    public string displayName;
    public string description;
    public string artKey;
    public string rarity;
    public string effectConfigJson;
    public bool isActive = true;
}

[Serializable]
public class ConsumableSeedData
{
    public string consumableId;
    public string displayName;
    public string description;
    public string artKey;
    public int maxStack = 1;
    public string effectConfigJson;
    public bool isActive = true;
}

[Serializable]
public class DivinePowerSeedData
{
    public string powerId;
    public string displayName;
    public string description;
    public string artKey;
    public string powerType;
    public int cooldownSeconds;
    public int durationSeconds;
    public int unlockCost;
    public bool startsUnlocked;
    public int sortOrder;
    public string effectConfigJson;
    public bool isActive = true;
}

[Serializable]
public class ShopOfferSeedData
{
    public string offerId;
    public string displayName;
    public string description;
    public string artKey;
    public string offerType;
    public int costGold;
    public string rewardType;
    public string rewardId;
    public int rewardQuantity = 1;
    public int durationSegments;
    public string effectConfigJson;
    public int sortOrder;
    public bool isActive = true;
}

[Serializable]
public class RunResultSeedData
{
    public string resultId;
    public string displayName;
    public string description;
    public string artKey;
    public int sortOrder;
    public bool isActive = true;
}

[Serializable]
public class CardSeedData
{
    public string cardId;
    public string displayName;
    public string description;
    public string artKey;
    public bool startsUnlocked;
    public string biomeId;
    public string floorColorHex;
    public string wallColorHex;
    public int segmentWidth;
    public int segmentHeight;
    public int entryX;
    public int exitX;
    public float obstacleChance;
    public float enemyChance;
    public string[] enemyIds = Array.Empty<string>();
    public int baseDifficulty = 1;
    public string cardType = "balanced";
    public int rewardTier = 1;
    public string[] generationTags = Array.Empty<string>();
    public int shopUnlockSegment;
    public int sortOrder;
    public bool isActive = true;
}

[Serializable]
public class CardEnemyPoolSeedData
{
    public string cardId;
    public string enemyId;
    public int weight = 1;
    public int minCount;
    public int maxCount = 3;
}

[Serializable]
public class CardModifierPoolSeedData
{
    public string cardId;
    public string modifierId;
    public int weight = 1;
    public bool guaranteed;
}

[Serializable]
public class CardRewardPoolSeedData
{
    public string cardId;
    public string rewardType;
    public string rewardId;
    public int weight = 1;
    public int quantity = 1;
}
