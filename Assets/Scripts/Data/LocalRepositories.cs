using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public sealed class LocalJsonContentRepository : IContentRepository
{
    private readonly Dictionary<string, BiomeSeedData> biomes;
    private readonly Dictionary<string, CardSeedData> cards;
    private readonly Dictionary<string, EnemyArchetypeSeedData> enemies;
    private readonly Dictionary<string, WorldModifierSeedData> modifiers;
    private readonly Dictionary<string, DivinePowerSeedData> divinePowers;
    private readonly Dictionary<string, RelicSeedData> relics;
    private readonly Dictionary<string, ConsumableSeedData> consumables;
    private readonly Dictionary<string, List<CardEnemyPoolSeedData>> cardEnemyPool;
    private readonly Dictionary<string, List<CardModifierPoolSeedData>> cardModifierPool;
    private readonly Dictionary<string, List<CardRewardPoolSeedData>> cardRewardPool;

    private readonly LocalContentSeed seed;

    public LocalJsonContentRepository(string resourcePath = "Seeds/vertical_slice_content")
    {
        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
        if (textAsset == null)
            throw new FileNotFoundException($"No s'ha trobat el seed de contingut a Resources/{resourcePath}.json");

        seed = JsonUtility.FromJson<LocalContentSeed>(textAsset.text) ?? new LocalContentSeed();
        biomes = seed.biomes.Where(item => item != null).ToDictionary(item => item.biomeId, item => item);
        cards = seed.cards.Where(item => item != null).ToDictionary(item => item.cardId, item => item);
        enemies = seed.enemies.Where(item => item != null).ToDictionary(item => item.enemyId, item => item);
        modifiers = seed.modifiers.Where(item => item != null).ToDictionary(item => item.modifierId, item => item);
        divinePowers = seed.divinePowers.Where(item => item != null).ToDictionary(item => item.powerId, item => item);
        relics = seed.relics.Where(item => item != null).ToDictionary(item => item.relicId, item => item);
        consumables = seed.consumables.Where(item => item != null).ToDictionary(item => item.consumableId, item => item);
        cardEnemyPool = seed.cardEnemyPool.GroupBy(item => item.cardId).ToDictionary(group => group.Key, group => group.ToList());
        cardModifierPool = seed.cardModifierPool.GroupBy(item => item.cardId).ToDictionary(group => group.Key, group => group.ToList());
        cardRewardPool = seed.cardRewardPool.GroupBy(item => item.cardId).ToDictionary(group => group.Key, group => group.ToList());
    }

    public IReadOnlyList<BiomeSeedData> GetBiomes() => seed.biomes;
    public IReadOnlyList<CardSeedData> GetCards() => seed.cards;
    public IReadOnlyList<DivinePowerSeedData> GetDivinePowers() => seed.divinePowers;
    public IReadOnlyList<EnemyArchetypeSeedData> GetEnemies() => seed.enemies;
    public IReadOnlyList<WorldModifierSeedData> GetModifiers() => seed.modifiers;
    public IReadOnlyList<RelicSeedData> GetRelics() => seed.relics;
    public IReadOnlyList<ConsumableSeedData> GetConsumables() => seed.consumables;
    public BiomeSeedData GetBiome(string biomeId) => biomes.TryGetValue(biomeId, out BiomeSeedData biome) ? biome : null;
    public CardSeedData GetCard(string cardId) => cards.TryGetValue(cardId, out CardSeedData card) ? card : null;
    public EnemyArchetypeSeedData GetEnemy(string enemyId) => enemies.TryGetValue(enemyId, out EnemyArchetypeSeedData enemy) ? enemy : null;
    public WorldModifierSeedData GetModifier(string modifierId) => modifiers.TryGetValue(modifierId, out WorldModifierSeedData modifier) ? modifier : null;
    public DivinePowerSeedData GetDivinePower(string powerId) => divinePowers.TryGetValue(powerId, out DivinePowerSeedData power) ? power : null;
    public IReadOnlyList<CardEnemyPoolSeedData> GetCardEnemyPool(string cardId) => cardEnemyPool.TryGetValue(cardId, out List<CardEnemyPoolSeedData> pool) ? pool : Array.Empty<CardEnemyPoolSeedData>();
    public IReadOnlyList<CardModifierPoolSeedData> GetCardModifierPool(string cardId) => cardModifierPool.TryGetValue(cardId, out List<CardModifierPoolSeedData> pool) ? pool : Array.Empty<CardModifierPoolSeedData>();
    public IReadOnlyList<CardRewardPoolSeedData> GetCardRewardPool(string cardId) => cardRewardPool.TryGetValue(cardId, out List<CardRewardPoolSeedData> pool) ? pool : Array.Empty<CardRewardPoolSeedData>();
}

public sealed class LocalFileProgressionRepository : IProgressionRepository
{
    private const string SaveFileName = "architectusfati_local_player_progress.json";

    private readonly string savePath;
    private LocalPlayerSeed cachedSeed;

    public LocalFileProgressionRepository(string resourcePath = "Seeds/local_player_seed")
    {
        savePath = Path.Combine(Application.persistentDataPath, SaveFileName);
        LoadOrCreate(resourcePath);
    }

    public PlayerProfileData LoadProfile(string playerId) => cachedSeed.profile;
    public PlayerProgressData LoadProgress(string playerId) => cachedSeed.progress;
    public IReadOnlyList<PlayerCardUnlockData> LoadCardUnlocks(string playerId) => cachedSeed.cardUnlocks;
    public IReadOnlyList<PlayerDivinePowerUnlockData> LoadDivinePowerUnlocks(string playerId) => cachedSeed.divinePowerUnlocks;
    public IReadOnlyList<PlayerConsumableStackData> LoadConsumableStacks(string playerId) => cachedSeed.consumables;

    public void Save(PlayerProfileData profile, PlayerProgressData progress, IReadOnlyList<PlayerCardUnlockData> cardUnlocks, IReadOnlyList<PlayerDivinePowerUnlockData> divinePowerUnlocks, IReadOnlyList<PlayerConsumableStackData> consumables)
    {
        cachedSeed.profile = profile;
        cachedSeed.progress = progress;
        cachedSeed.cardUnlocks = cardUnlocks?.ToArray() ?? Array.Empty<PlayerCardUnlockData>();
        cachedSeed.divinePowerUnlocks = divinePowerUnlocks?.ToArray() ?? Array.Empty<PlayerDivinePowerUnlockData>();
        cachedSeed.consumables = consumables?.ToArray() ?? Array.Empty<PlayerConsumableStackData>();

        string directory = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(savePath, JsonUtility.ToJson(cachedSeed, true));
    }

    private void LoadOrCreate(string resourcePath)
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            cachedSeed = JsonUtility.FromJson<LocalPlayerSeed>(json);
        }

        if (cachedSeed != null)
            return;

        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
        if (textAsset == null)
            throw new FileNotFoundException($"No s'ha trobat el seed de progressio a Resources/{resourcePath}.json");

        cachedSeed = JsonUtility.FromJson<LocalPlayerSeed>(textAsset.text) ?? new LocalPlayerSeed();
        Save(cachedSeed.profile, cachedSeed.progress, cachedSeed.cardUnlocks, cachedSeed.divinePowerUnlocks, cachedSeed.consumables);
    }
}

public sealed class InMemoryRunRepository : IRunRepository
{
    private readonly List<RunRewardData> rewards = new List<RunRewardData>();
    private readonly List<RunEventData> events = new List<RunEventData>();
    private readonly Dictionary<long, RunSessionData> runs = new Dictionary<long, RunSessionData>();
    private readonly Dictionary<long, RunSegmentData> segments = new Dictionary<long, RunSegmentData>();
    private readonly Dictionary<long, RunSegmentEnemyData> segmentEnemies = new Dictionary<long, RunSegmentEnemyData>();

    private long nextRunId = 1;
    private long nextSegmentId = 1;
    private long nextSegmentEnemyId = 1;
    private long nextRewardId = 1;
    private long nextEventId = 1;

    public RunSessionData CreateRun(string playerId, int targetSegmentCount, string startingCardId, string heroMode, IReadOnlyList<string> equippedDivinePowerIds)
    {
        RunSessionData run = new RunSessionData
        {
            runId = nextRunId++,
            playerId = playerId,
            targetSegmentCount = targetSegmentCount,
            startingCardId = startingCardId,
            heroMode = heroMode,
            equippedDivinePowerIds = new List<string>(equippedDivinePowerIds ?? Array.Empty<string>()),
            runSeed = Guid.NewGuid().ToString("N")
        };

        runs[run.runId] = run;
        return run;
    }

    public RunSegmentData CreateSegment(RunSessionData run, CardSeedData card, float difficultyMultiplier)
    {
        RunSegmentData segment = new RunSegmentData
        {
            runSegmentId = nextSegmentId++,
            runId = run.runId,
            segmentIndex = run.currentSegmentIndex,
            cardId = card.cardId,
            biomeId = card.biomeId,
            floorColorHex = card.floorColorHex,
            wallColorHex = card.wallColorHex,
            segmentWidth = card.segmentWidth,
            segmentHeight = card.segmentHeight,
            entryX = card.entryX,
            exitX = card.exitX,
            obstacleChance = card.obstacleChance,
            enemyChance = card.enemyChance,
            generatedSeed = $"{run.runSeed}_{run.currentSegmentIndex}_{card.cardId}",
            difficultyMultiplier = difficultyMultiplier,
            cardType = card.cardType,
            rewardTier = card.rewardTier
        };

        segments[segment.runSegmentId] = segment;
        return segment;
    }

    public void SaveSegmentChoices(long runId, int segmentIndex, IReadOnlyList<CardSeedData> offeredCards, string selectedCardId)
    {
        for (int i = 0; i < offeredCards.Count; i++)
        {
            AddEvent(runId, 0, "card_choice_offered", $"{{\"segmentIndex\":{segmentIndex},\"slot\":{i + 1},\"cardId\":\"{offeredCards[i].cardId}\",\"selected\":{(offeredCards[i].cardId == selectedCardId).ToString().ToLowerInvariant()}}}");
        }
    }

    public RunSegmentEnemyData AddSegmentEnemy(long runSegmentId, string enemyId, int spawnX, int spawnY, int maxHealth, int attack, int defense, float speed, string metadataJson)
    {
        RunSegmentEnemyData enemy = new RunSegmentEnemyData
        {
            runSegmentEnemyId = nextSegmentEnemyId++,
            runSegmentId = runSegmentId,
            enemyId = enemyId,
            spawnX = spawnX,
            spawnY = spawnY,
            spawnedMaxHealth = maxHealth,
            spawnedAttack = attack,
            spawnedDefense = defense,
            spawnedSpeed = speed,
            metadataJson = metadataJson
        };

        segmentEnemies[enemy.runSegmentEnemyId] = enemy;
        if (segments.TryGetValue(runSegmentId, out RunSegmentData segment))
        {
            segment.enemies.Add(enemy);
        }

        return enemy;
    }

    public void MarkEnemyDefeated(long runSegmentEnemyId)
    {
        if (segmentEnemies.TryGetValue(runSegmentEnemyId, out RunSegmentEnemyData enemy))
        {
            enemy.defeated = true;
        }
    }

    public void AddReward(long runId, long runSegmentId, string rewardType, string rewardId, int quantity, string metadataJson)
    {
        rewards.Add(new RunRewardData
        {
            runRewardId = nextRewardId++,
            runId = runId,
            runSegmentId = runSegmentId,
            rewardType = rewardType,
            rewardId = rewardId,
            quantity = quantity,
            metadataJson = metadataJson
        });
    }

    public void AddEvent(long runId, long runSegmentId, string eventType, string payloadJson)
    {
        events.Add(new RunEventData
        {
            runEventId = nextEventId++,
            runId = runId,
            runSegmentId = runSegmentId,
            eventType = eventType,
            eventOrder = events.Count,
            payloadJson = payloadJson
        });
    }

    public IReadOnlyList<RunRewardData> GetRewards(long runId) => rewards.Where(item => item.runId == runId).ToArray();
    public IReadOnlyList<RunEventData> GetEvents(long runId) => events.Where(item => item.runId == runId).ToArray();

    public void UpdateRun(RunSessionData run)
    {
        runs[run.runId] = run;
    }
}
