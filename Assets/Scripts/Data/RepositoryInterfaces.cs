using System.Collections.Generic;

public interface IContentRepository
{
    IReadOnlyList<BiomeSeedData> GetBiomes();
    IReadOnlyList<CardSeedData> GetCards();
    IReadOnlyList<DivinePowerSeedData> GetDivinePowers();
    IReadOnlyList<EnemyArchetypeSeedData> GetEnemies();
    IReadOnlyList<WorldModifierSeedData> GetModifiers();
    IReadOnlyList<RelicSeedData> GetRelics();
    IReadOnlyList<ConsumableSeedData> GetConsumables();
    IReadOnlyList<ShopOfferSeedData> GetShopOffers();
    IReadOnlyList<RunResultSeedData> GetRunResults();
    BiomeSeedData GetBiome(string biomeId);
    CardSeedData GetCard(string cardId);
    EnemyArchetypeSeedData GetEnemy(string enemyId);
    WorldModifierSeedData GetModifier(string modifierId);
    DivinePowerSeedData GetDivinePower(string powerId);
    ShopOfferSeedData GetShopOffer(string offerId);
    RunResultSeedData GetRunResult(string resultId);
    IReadOnlyList<CardEnemyPoolSeedData> GetCardEnemyPool(string cardId);
    IReadOnlyList<CardModifierPoolSeedData> GetCardModifierPool(string cardId);
    IReadOnlyList<CardRewardPoolSeedData> GetCardRewardPool(string cardId);
}

public interface IProgressionRepository
{
    string GetActivePlayerId();
    IReadOnlyList<PlayerProfileSummaryData> LoadProfileSummaries();
    PlayerProfileData LoadProfile(string playerId);
    PlayerProgressData LoadProgress(string playerId);
    IReadOnlyList<PlayerCardUnlockData> LoadCardUnlocks(string playerId);
    IReadOnlyList<PlayerDivinePowerUnlockData> LoadDivinePowerUnlocks(string playerId);
    IReadOnlyList<PlayerRelicData> LoadRelics(string playerId);
    IReadOnlyList<PlayerConsumableStackData> LoadConsumableStacks(string playerId);
    bool SetActiveProfile(string playerId);
    PlayerProfileData CreateProfile(string displayName);
    void Save(PlayerProfileData profile, PlayerProgressData progress, IReadOnlyList<PlayerCardUnlockData> cardUnlocks, IReadOnlyList<PlayerDivinePowerUnlockData> divinePowerUnlocks, IReadOnlyList<PlayerRelicData> relics, IReadOnlyList<PlayerConsumableStackData> consumables);
}

public interface IRunRepository
{
    RunSessionData CreateRun(string playerId, int targetSegmentCount, string startingCardId, string heroMode, IReadOnlyList<string> equippedDivinePowerIds);
    RunSegmentData CreateSegment(RunSessionData run, CardSeedData card, float difficultyMultiplier);
    void SaveSegmentChoices(long runId, int segmentIndex, IReadOnlyList<CardSeedData> offeredCards, string selectedCardId);
    RunSegmentEnemyData AddSegmentEnemy(long runSegmentId, string enemyId, int spawnX, int spawnY, int maxHealth, int attack, int defense, float speed, string metadataJson);
    void MarkEnemyDefeated(long runSegmentEnemyId);
    void AddReward(long runId, long runSegmentId, string rewardType, string rewardId, int quantity, string metadataJson);
    void AddEvent(long runId, long runSegmentId, string eventType, string payloadJson);
    IReadOnlyList<RunRewardData> GetRewards(long runId);
    IReadOnlyList<RunEventData> GetEvents(long runId);
    void UpdateRun(RunSessionData run);
}
