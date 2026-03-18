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
    private readonly Dictionary<string, ShopOfferSeedData> shopOffers;
    private readonly Dictionary<string, RunResultSeedData> runResults;
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
        shopOffers = seed.shopOffers.Where(item => item != null).ToDictionary(item => item.offerId, item => item);
        runResults = seed.runResults.Where(item => item != null).ToDictionary(item => item.resultId, item => item);
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
    public IReadOnlyList<ShopOfferSeedData> GetShopOffers() => seed.shopOffers;
    public IReadOnlyList<RunResultSeedData> GetRunResults() => seed.runResults;
    public BiomeSeedData GetBiome(string biomeId) => biomes.TryGetValue(biomeId, out BiomeSeedData biome) ? biome : null;
    public CardSeedData GetCard(string cardId) => cards.TryGetValue(cardId, out CardSeedData card) ? card : null;
    public EnemyArchetypeSeedData GetEnemy(string enemyId) => enemies.TryGetValue(enemyId, out EnemyArchetypeSeedData enemy) ? enemy : null;
    public WorldModifierSeedData GetModifier(string modifierId) => modifiers.TryGetValue(modifierId, out WorldModifierSeedData modifier) ? modifier : null;
    public DivinePowerSeedData GetDivinePower(string powerId) => divinePowers.TryGetValue(powerId, out DivinePowerSeedData power) ? power : null;
    public ShopOfferSeedData GetShopOffer(string offerId) => shopOffers.TryGetValue(offerId, out ShopOfferSeedData offer) ? offer : null;
    public RunResultSeedData GetRunResult(string resultId) => runResults.TryGetValue(resultId, out RunResultSeedData result) ? result : null;
    public IReadOnlyList<CardEnemyPoolSeedData> GetCardEnemyPool(string cardId) => cardEnemyPool.TryGetValue(cardId, out List<CardEnemyPoolSeedData> pool) ? pool : Array.Empty<CardEnemyPoolSeedData>();
    public IReadOnlyList<CardModifierPoolSeedData> GetCardModifierPool(string cardId) => cardModifierPool.TryGetValue(cardId, out List<CardModifierPoolSeedData> pool) ? pool : Array.Empty<CardModifierPoolSeedData>();
    public IReadOnlyList<CardRewardPoolSeedData> GetCardRewardPool(string cardId) => cardRewardPool.TryGetValue(cardId, out List<CardRewardPoolSeedData> pool) ? pool : Array.Empty<CardRewardPoolSeedData>();
}

public sealed class LocalFileProgressionRepository : IProgressionRepository
{
    private const string SaveFileName = "architectusfati_local_player_progress.json";

    private readonly string savePath;
    private readonly LocalPlayerSeed defaultTemplate;
    private LocalProgressionDatabaseSeed cachedDatabase;

    public LocalFileProgressionRepository(string resourcePath = "Seeds/local_player_seed")
    {
        savePath = Path.Combine(Application.persistentDataPath, SaveFileName);
        defaultTemplate = LoadTemplate(resourcePath);
        LoadOrCreate(resourcePath);
    }

    public string GetActivePlayerId()
    {
        EnsureActiveProfile();
        return cachedDatabase.activePlayerId;
    }

    public IReadOnlyList<PlayerProfileSummaryData> LoadProfileSummaries()
    {
        EnsureActiveProfile();
        return cachedDatabase.players
            .Where(item => item != null)
            .Select(profile =>
            {
                PlayerProgressData progress = cachedDatabase.playerProgress.FirstOrDefault(item => item != null && item.playerId == profile.playerId) ?? new PlayerProgressData();
                return new PlayerProfileSummaryData
                {
                    playerId = profile.playerId,
                    displayName = profile.displayName,
                    emeralds = progress.hardCurrency,
                    completedRuns = progress.completedRuns,
                    highestSegmentReached = progress.highestSegmentReached,
                    lastSeenAtUtc = profile.lastSeenAtUtc
                };
            })
            .OrderByDescending(item => item.playerId == cachedDatabase.activePlayerId)
            .ThenByDescending(item => ParseUtcTicks(item.lastSeenAtUtc))
            .ThenBy(item => item.displayName)
            .ToArray();
    }

    public PlayerProfileData LoadProfile(string playerId)
    {
        EnsureActiveProfile();
        return CloneProfile(cachedDatabase.players.FirstOrDefault(item => item != null && item.playerId == playerId));
    }

    public PlayerProgressData LoadProgress(string playerId)
    {
        EnsureActiveProfile();
        return CloneProgress(cachedDatabase.playerProgress.FirstOrDefault(item => item != null && item.playerId == playerId));
    }

    public IReadOnlyList<PlayerCardUnlockData> LoadCardUnlocks(string playerId)
    {
        EnsureActiveProfile();
        return cachedDatabase.playerCardUnlocks
            .Where(item => item != null && item.playerId == playerId)
            .Select(CloneCardUnlock)
            .ToArray();
    }

    public IReadOnlyList<PlayerDivinePowerUnlockData> LoadDivinePowerUnlocks(string playerId)
    {
        EnsureActiveProfile();
        return cachedDatabase.playerDivinePowerUnlocks
            .Where(item => item != null && item.playerId == playerId)
            .Select(CloneDivinePowerUnlock)
            .ToArray();
    }

    public IReadOnlyList<PlayerRelicData> LoadRelics(string playerId)
    {
        EnsureActiveProfile();
        return cachedDatabase.playerRelics
            .Where(item => item != null && item.playerId == playerId)
            .Select(CloneRelic)
            .ToArray();
    }

    public IReadOnlyList<PlayerConsumableStackData> LoadConsumableStacks(string playerId)
    {
        EnsureActiveProfile();
        return cachedDatabase.playerConsumables
            .Where(item => item != null && item.playerId == playerId)
            .Select(CloneConsumable)
            .ToArray();
    }

    public bool SetActiveProfile(string playerId)
    {
        EnsureActiveProfile();
        if (string.IsNullOrWhiteSpace(playerId) || !cachedDatabase.players.Any(item => item != null && item.playerId == playerId))
            return false;

        cachedDatabase.activePlayerId = playerId;
        TouchProfile(playerId);
        PersistDatabase();
        return true;
    }

    public PlayerProfileData CreateProfile(string displayName)
    {
        EnsureActiveProfile();

        string safeDisplayName = string.IsNullOrWhiteSpace(displayName)
            ? $"Perfil {cachedDatabase.players.Length + 1}"
            : displayName.Trim();

        string playerId = GenerateProfileId(safeDisplayName);
        string now = DateTime.UtcNow.ToString("o");

        PlayerProfileData profile = CloneProfile(defaultTemplate.profile) ?? new PlayerProfileData();
        profile.playerId = playerId;
        profile.displayName = safeDisplayName;
        profile.createdAtUtc = now;
        profile.lastSeenAtUtc = now;

        PlayerProgressData progress = CloneProgress(defaultTemplate.progress) ?? new PlayerProgressData();
        progress.playerId = playerId;

        PlayerCardUnlockData[] cardUnlocks = (defaultTemplate.cardUnlocks ?? Array.Empty<PlayerCardUnlockData>())
            .Select(CloneCardUnlock)
            .Where(item => item != null)
            .ToArray();
        for (int i = 0; i < cardUnlocks.Length; i++)
            cardUnlocks[i].playerId = playerId;

        PlayerDivinePowerUnlockData[] divinePowerUnlocks = (defaultTemplate.divinePowerUnlocks ?? Array.Empty<PlayerDivinePowerUnlockData>())
            .Select(CloneDivinePowerUnlock)
            .Where(item => item != null)
            .ToArray();
        for (int i = 0; i < divinePowerUnlocks.Length; i++)
            divinePowerUnlocks[i].playerId = playerId;

        PlayerRelicData[] relics = (defaultTemplate.relics ?? Array.Empty<PlayerRelicData>())
            .Select(CloneRelic)
            .Where(item => item != null)
            .ToArray();
        for (int i = 0; i < relics.Length; i++)
        {
            relics[i].playerId = playerId;
            if (string.IsNullOrWhiteSpace(relics[i].firstObtainedAtUtc))
                relics[i].firstObtainedAtUtc = now;
            if (string.IsNullOrWhiteSpace(relics[i].lastObtainedAtUtc))
                relics[i].lastObtainedAtUtc = now;
            relics[i].quantity = Mathf.Max(1, relics[i].quantity);
        }

        PlayerConsumableStackData[] consumables = (defaultTemplate.consumables ?? Array.Empty<PlayerConsumableStackData>())
            .Select(CloneConsumable)
            .Where(item => item != null)
            .ToArray();
        for (int i = 0; i < consumables.Length; i++)
            consumables[i].playerId = playerId;

        cachedDatabase.players = cachedDatabase.players.Concat(new[] { profile }).ToArray();
        cachedDatabase.playerProgress = cachedDatabase.playerProgress.Concat(new[] { progress }).ToArray();
        cachedDatabase.playerCardUnlocks = cachedDatabase.playerCardUnlocks.Concat(cardUnlocks).ToArray();
        cachedDatabase.playerDivinePowerUnlocks = cachedDatabase.playerDivinePowerUnlocks.Concat(divinePowerUnlocks).ToArray();
        cachedDatabase.playerRelics = cachedDatabase.playerRelics.Concat(relics).ToArray();
        cachedDatabase.playerConsumables = cachedDatabase.playerConsumables.Concat(consumables).ToArray();
        cachedDatabase.activePlayerId = playerId;

        PersistDatabase();
        return CloneProfile(profile);
    }

    public void Save(PlayerProfileData profile, PlayerProgressData progress, IReadOnlyList<PlayerCardUnlockData> cardUnlocks, IReadOnlyList<PlayerDivinePowerUnlockData> divinePowerUnlocks, IReadOnlyList<PlayerRelicData> relics, IReadOnlyList<PlayerConsumableStackData> consumables)
    {
        EnsureActiveProfile();

        PlayerProfileData safeProfile = CloneProfile(profile) ?? CreateFallbackProfile(defaultTemplate.profile != null ? defaultTemplate.profile.playerId : BalanceConfig.LocalPlayerId);
        PlayerProgressData safeProgress = CloneProgress(progress) ?? new PlayerProgressData();
        string playerId = string.IsNullOrWhiteSpace(safeProfile.playerId) ? BalanceConfig.LocalPlayerId : safeProfile.playerId;
        string now = DateTime.UtcNow.ToString("o");

        safeProfile.playerId = playerId;
        if (string.IsNullOrWhiteSpace(safeProfile.createdAtUtc))
            safeProfile.createdAtUtc = now;
        safeProfile.lastSeenAtUtc = now;

        safeProgress.playerId = playerId;

        cachedDatabase.players = UpsertByPlayerId(cachedDatabase.players, safeProfile, item => item.playerId);
        cachedDatabase.playerProgress = UpsertByPlayerId(cachedDatabase.playerProgress, safeProgress, item => item.playerId);
        cachedDatabase.playerCardUnlocks = cachedDatabase.playerCardUnlocks
            .Where(item => item != null && item.playerId != playerId)
            .Concat((cardUnlocks ?? Array.Empty<PlayerCardUnlockData>()).Select(CloneCardUnlock).Where(item => item != null).Select(item =>
            {
                item.playerId = playerId;
                return item;
            }))
            .ToArray();
        cachedDatabase.playerDivinePowerUnlocks = cachedDatabase.playerDivinePowerUnlocks
            .Where(item => item != null && item.playerId != playerId)
            .Concat((divinePowerUnlocks ?? Array.Empty<PlayerDivinePowerUnlockData>()).Select(CloneDivinePowerUnlock).Where(item => item != null).Select(item =>
            {
                item.playerId = playerId;
                return item;
            }))
            .ToArray();
        cachedDatabase.playerRelics = cachedDatabase.playerRelics
            .Where(item => item != null && item.playerId != playerId)
            .Concat((relics ?? Array.Empty<PlayerRelicData>()).Select(CloneRelic).Where(item => item != null).Select(item =>
            {
                item.playerId = playerId;
                item.quantity = Mathf.Max(1, item.quantity);
                if (string.IsNullOrWhiteSpace(item.firstObtainedAtUtc))
                    item.firstObtainedAtUtc = now;
                if (string.IsNullOrWhiteSpace(item.lastObtainedAtUtc))
                    item.lastObtainedAtUtc = now;
                return item;
            }))
            .ToArray();
        cachedDatabase.playerConsumables = cachedDatabase.playerConsumables
            .Where(item => item != null && item.playerId != playerId)
            .Concat((consumables ?? Array.Empty<PlayerConsumableStackData>()).Select(CloneConsumable).Where(item => item != null).Select(item =>
            {
                item.playerId = playerId;
                return item;
            }))
            .ToArray();
        cachedDatabase.activePlayerId = playerId;

        PersistDatabase();
    }

    private void LoadOrCreate(string resourcePath)
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            cachedDatabase = JsonUtility.FromJson<LocalProgressionDatabaseSeed>(json);
            if (!IsValidDatabase(cachedDatabase))
            {
                LocalPlayerSeed legacySeed = JsonUtility.FromJson<LocalPlayerSeed>(json);
                if (legacySeed != null && legacySeed.profile != null)
                    cachedDatabase = MigrateLegacySeed(legacySeed);
            }
        }

        if (IsValidDatabase(cachedDatabase))
        {
            EnsureActiveProfile();
            return;
        }

        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
        if (textAsset != null)
        {
            LocalPlayerSeed seed = JsonUtility.FromJson<LocalPlayerSeed>(textAsset.text) ?? defaultTemplate ?? new LocalPlayerSeed();
            cachedDatabase = MigrateLegacySeed(seed);
            PersistDatabase();
            return;
        }

        cachedDatabase = MigrateLegacySeed(defaultTemplate ?? new LocalPlayerSeed());
        PersistDatabase();
    }

    private static bool IsValidDatabase(LocalProgressionDatabaseSeed database)
    {
        return database != null && database.players != null && database.players.Length > 0;
    }

    private LocalPlayerSeed LoadTemplate(string resourcePath)
    {
        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
        if (textAsset == null)
            return new LocalPlayerSeed();

        return JsonUtility.FromJson<LocalPlayerSeed>(textAsset.text) ?? new LocalPlayerSeed();
    }

    private LocalProgressionDatabaseSeed MigrateLegacySeed(LocalPlayerSeed legacySeed)
    {
        LocalPlayerSeed safeSeed = legacySeed ?? new LocalPlayerSeed();
        PlayerProfileData profile = CloneProfile(safeSeed.profile) ?? CreateFallbackProfile(BalanceConfig.LocalPlayerId);
        PlayerProgressData progress = CloneProgress(safeSeed.progress) ?? new PlayerProgressData();
        string playerId = string.IsNullOrWhiteSpace(profile.playerId) ? BalanceConfig.LocalPlayerId : profile.playerId;
        string now = DateTime.UtcNow.ToString("o");

        profile.playerId = playerId;
        if (string.IsNullOrWhiteSpace(profile.createdAtUtc))
            profile.createdAtUtc = now;
        if (string.IsNullOrWhiteSpace(profile.lastSeenAtUtc))
            profile.lastSeenAtUtc = now;

        progress.playerId = playerId;

        PlayerCardUnlockData[] cardUnlocks = (safeSeed.cardUnlocks ?? Array.Empty<PlayerCardUnlockData>())
            .Select(CloneCardUnlock)
            .Where(item => item != null)
            .ToArray();
        for (int i = 0; i < cardUnlocks.Length; i++)
            cardUnlocks[i].playerId = playerId;

        PlayerDivinePowerUnlockData[] divinePowerUnlocks = (safeSeed.divinePowerUnlocks ?? Array.Empty<PlayerDivinePowerUnlockData>())
            .Select(CloneDivinePowerUnlock)
            .Where(item => item != null)
            .ToArray();
        for (int i = 0; i < divinePowerUnlocks.Length; i++)
            divinePowerUnlocks[i].playerId = playerId;

        PlayerRelicData[] relics = (safeSeed.relics != null && safeSeed.relics.Length > 0
                ? safeSeed.relics.Select(CloneRelic).Where(item => item != null)
                : BuildRelicsFromUnlockedIds(playerId, progress.unlockedRelicIds, now))
            .ToArray();
        for (int i = 0; i < relics.Length; i++)
        {
            relics[i].playerId = playerId;
            relics[i].quantity = Mathf.Max(1, relics[i].quantity);
            if (string.IsNullOrWhiteSpace(relics[i].firstObtainedAtUtc))
                relics[i].firstObtainedAtUtc = now;
            if (string.IsNullOrWhiteSpace(relics[i].lastObtainedAtUtc))
                relics[i].lastObtainedAtUtc = now;
        }

        PlayerConsumableStackData[] consumables = (safeSeed.consumables ?? Array.Empty<PlayerConsumableStackData>())
            .Select(CloneConsumable)
            .Where(item => item != null)
            .ToArray();
        for (int i = 0; i < consumables.Length; i++)
            consumables[i].playerId = playerId;

        return new LocalProgressionDatabaseSeed
        {
            activePlayerId = playerId,
            players = new[] { profile },
            playerProgress = new[] { progress },
            playerCardUnlocks = cardUnlocks,
            playerDivinePowerUnlocks = divinePowerUnlocks,
            playerRelics = relics,
            playerConsumables = consumables
        };
    }

    private void EnsureActiveProfile()
    {
        if (!IsValidDatabase(cachedDatabase))
            cachedDatabase = MigrateLegacySeed(defaultTemplate ?? new LocalPlayerSeed());

        if (string.IsNullOrWhiteSpace(cachedDatabase.activePlayerId) || !cachedDatabase.players.Any(item => item != null && item.playerId == cachedDatabase.activePlayerId))
            cachedDatabase.activePlayerId = cachedDatabase.players.First(item => item != null).playerId;
    }

    private void TouchProfile(string playerId)
    {
        PlayerProfileData profile = cachedDatabase.players.FirstOrDefault(item => item != null && item.playerId == playerId);
        if (profile == null)
            return;

        profile.lastSeenAtUtc = DateTime.UtcNow.ToString("o");
    }

    private void PersistDatabase()
    {
        string directory = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(savePath, JsonUtility.ToJson(cachedDatabase, true));
    }

    private string GenerateProfileId(string displayName)
    {
        string baseId = new string(displayName
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray())
            .Trim('-');

        if (string.IsNullOrWhiteSpace(baseId))
            baseId = "perfil";

        string candidate = baseId;
        int suffix = 2;
        while (cachedDatabase.players.Any(item => item != null && item.playerId == candidate))
            candidate = $"{baseId}-{suffix++}";

        return candidate;
    }

    private static T[] UpsertByPlayerId<T>(IEnumerable<T> source, T replacement, Func<T, string> playerIdSelector)
    {
        List<T> items = source?.Where(item => item != null && playerIdSelector(item) != playerIdSelector(replacement)).ToList() ?? new List<T>();
        items.Add(replacement);
        return items.ToArray();
    }

    private static PlayerProfileData CreateFallbackProfile(string playerId)
    {
        return new PlayerProfileData
        {
            playerId = string.IsNullOrWhiteSpace(playerId) ? BalanceConfig.LocalPlayerId : playerId,
            displayName = "Jugador Local",
            preferredLanguage = "ca",
            preferredRunLength = BalanceConfig.DefaultShortRunLength,
            selectedHeroMode = BalanceConfig.DefaultHeroMode,
            createdAtUtc = DateTime.UtcNow.ToString("o"),
            lastSeenAtUtc = DateTime.UtcNow.ToString("o")
        };
    }

    private static long ParseUtcTicks(string value)
    {
        return DateTime.TryParse(value, out DateTime parsed) ? parsed.Ticks : 0L;
    }

    private static PlayerProfileData CloneProfile(PlayerProfileData source)
    {
        if (source == null)
            return null;

        return new PlayerProfileData
        {
            playerId = source.playerId,
            displayName = source.displayName,
            preferredLanguage = source.preferredLanguage,
            preferredRunLength = source.preferredRunLength,
            selectedHeroMode = source.selectedHeroMode,
            createdAtUtc = source.createdAtUtc,
            lastSeenAtUtc = source.lastSeenAtUtc
        };
    }

    private static PlayerProgressData CloneProgress(PlayerProgressData source)
    {
        if (source == null)
            return null;

        return new PlayerProgressData
        {
            playerId = source.playerId,
            unlockedCardIds = source.unlockedCardIds?.ToArray() ?? Array.Empty<string>(),
            unlockedRelicIds = source.unlockedRelicIds?.ToArray() ?? Array.Empty<string>(),
            unlockedModifierIds = source.unlockedModifierIds?.ToArray() ?? Array.Empty<string>(),
            unlockedDivinePowerIds = source.unlockedDivinePowerIds?.ToArray() ?? Array.Empty<string>(),
            completedRuns = source.completedRuns,
            failedRuns = source.failedRuns,
            totalRunsStarted = source.totalRunsStarted,
            totalCardsUnlocked = source.totalCardsUnlocked,
            highestSegmentReached = source.highestSegmentReached,
            totalEnemiesDefeated = source.totalEnemiesDefeated,
            totalDamageDealt = source.totalDamageDealt,
            totalDamageTaken = source.totalDamageTaken,
            softCurrency = source.softCurrency,
            hardCurrency = source.hardCurrency,
            run5Unlocked = source.run5Unlocked,
            run7Unlocked = source.run7Unlocked,
            shopEnabled = source.shopEnabled,
            biomesUnlocked = source.biomesUnlocked?.ToArray() ?? Array.Empty<string>()
        };
    }

    private static PlayerCardUnlockData CloneCardUnlock(PlayerCardUnlockData source)
    {
        if (source == null)
            return null;

        return new PlayerCardUnlockData
        {
            playerId = source.playerId,
            cardId = source.cardId,
            unlockSource = source.unlockSource
        };
    }

    private static PlayerDivinePowerUnlockData CloneDivinePowerUnlock(PlayerDivinePowerUnlockData source)
    {
        if (source == null)
            return null;

        return new PlayerDivinePowerUnlockData
        {
            playerId = source.playerId,
            powerId = source.powerId,
            unlockSource = source.unlockSource
        };
    }

    private static PlayerRelicData CloneRelic(PlayerRelicData source)
    {
        if (source == null)
            return null;

        return new PlayerRelicData
        {
            playerId = source.playerId,
            relicId = source.relicId,
            quantity = Mathf.Max(1, source.quantity),
            firstObtainedAtUtc = source.firstObtainedAtUtc,
            lastObtainedAtUtc = source.lastObtainedAtUtc
        };
    }

    private static PlayerConsumableStackData CloneConsumable(PlayerConsumableStackData source)
    {
        if (source == null)
            return null;

        return new PlayerConsumableStackData
        {
            playerId = source.playerId,
            consumableId = source.consumableId,
            quantity = source.quantity
        };
    }

    private static IEnumerable<PlayerRelicData> BuildRelicsFromUnlockedIds(string playerId, IEnumerable<string> relicIds, string timestampUtc)
    {
        return (relicIds ?? Array.Empty<string>())
            .Where(relicId => !string.IsNullOrWhiteSpace(relicId))
            .Distinct()
            .Select(relicId => new PlayerRelicData
            {
                playerId = playerId,
                relicId = relicId,
                quantity = 1,
                firstObtainedAtUtc = timestampUtc,
                lastObtainedAtUtc = timestampUtc
            });
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
            segment.enemies.Add(enemy);

        return enemy;
    }

    public void MarkEnemyDefeated(long runSegmentEnemyId)
    {
        if (segmentEnemies.TryGetValue(runSegmentEnemyId, out RunSegmentEnemyData enemy))
            enemy.defeated = true;
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
