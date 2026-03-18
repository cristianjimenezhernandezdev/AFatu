using System;

[Serializable]
public class LocalPlayerSeed
{
    public PlayerProfileData profile = new PlayerProfileData();
    public PlayerProgressData progress = new PlayerProgressData();
    public PlayerCardUnlockData[] cardUnlocks = Array.Empty<PlayerCardUnlockData>();
    public PlayerDivinePowerUnlockData[] divinePowerUnlocks = Array.Empty<PlayerDivinePowerUnlockData>();
    public PlayerRelicData[] relics = Array.Empty<PlayerRelicData>();
    public PlayerConsumableStackData[] consumables = Array.Empty<PlayerConsumableStackData>();
}

[Serializable]
public class LocalProgressionDatabaseSeed
{
    public string activePlayerId = BalanceConfig.LocalPlayerId;
    public PlayerProfileData[] players = Array.Empty<PlayerProfileData>();
    public PlayerProgressData[] playerProgress = Array.Empty<PlayerProgressData>();
    public PlayerCardUnlockData[] playerCardUnlocks = Array.Empty<PlayerCardUnlockData>();
    public PlayerDivinePowerUnlockData[] playerDivinePowerUnlocks = Array.Empty<PlayerDivinePowerUnlockData>();
    public PlayerRelicData[] playerRelics = Array.Empty<PlayerRelicData>();
    public PlayerConsumableStackData[] playerConsumables = Array.Empty<PlayerConsumableStackData>();
}

[Serializable]
public class PlayerProfileSummaryData
{
    public string playerId;
    public string displayName;
    public int emeralds;
    public int completedRuns;
    public int highestSegmentReached;
    public string lastSeenAtUtc;
}

[Serializable]
public class PlayerProfileData
{
    public string playerId = BalanceConfig.LocalPlayerId;
    public string displayName = "Jugador Local";
    public string preferredLanguage = "ca";
    public int preferredRunLength = BalanceConfig.DefaultShortRunLength;
    public string selectedHeroMode = BalanceConfig.DefaultHeroMode;
    public string createdAtUtc = string.Empty;
    public string lastSeenAtUtc = string.Empty;
}

[Serializable]
public class PlayerProgressData
{
    public string playerId = BalanceConfig.LocalPlayerId;
    public string[] unlockedCardIds = Array.Empty<string>();
    public string[] unlockedRelicIds = Array.Empty<string>();
    public string[] unlockedModifierIds = Array.Empty<string>();
    public string[] unlockedDivinePowerIds = Array.Empty<string>();
    public int completedRuns;
    public int failedRuns;
    public int totalRunsStarted;
    public int totalCardsUnlocked;
    public int highestSegmentReached;
    public int totalEnemiesDefeated;
    public int totalDamageDealt;
    public int totalDamageTaken;
    public int softCurrency;
    public int hardCurrency;
    public bool run5Unlocked = true;
    public bool run7Unlocked;
    public bool shopEnabled = true;
    public string[] biomesUnlocked = Array.Empty<string>();
}

[Serializable]
public class PlayerCardUnlockData
{
    public string playerId;
    public string cardId;
    public string unlockSource;
}

[Serializable]
public class PlayerDivinePowerUnlockData
{
    public string playerId;
    public string powerId;
    public string unlockSource;
}

[Serializable]
public class PlayerConsumableStackData
{
    public string playerId;
    public string consumableId;
    public int quantity;
}

[Serializable]
public class PlayerRelicData
{
    public string playerId;
    public string relicId;
    public int quantity = 1;
    public string firstObtainedAtUtc = string.Empty;
    public string lastObtainedAtUtc = string.Empty;
}
