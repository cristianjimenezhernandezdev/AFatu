using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class RunManager : MonoBehaviour
{
    public enum RunState
    {
        None,
        Bootstrapping,
        Transitioning,
        ExploringSegment,
        AwaitingShopChoice,
        AwaitingCardChoice,
        Completed,
        Failed
    }

    public static RunManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private WorldGrid worldGrid;
    [SerializeField] private PlayerGridMovement player;
    [SerializeField] private HeroAIController heroAi;
    [SerializeField] private GameObject fallbackEnemyTemplate;

    [Header("Run Setup")]
    [SerializeField] private int targetRunLength = BalanceConfig.DefaultShortRunLength;
    [SerializeField] private string localPlayerId = BalanceConfig.LocalPlayerId;
    [SerializeField] private bool autoStartOnPlay = true;

    private IContentRepository contentRepository;
    private IProgressionRepository progressionRepository;
    private IRunRepository runRepository;

    private SegmentGenerator segmentGenerator;
    private EconomySystem economySystem;
    private ShopSystem shopSystem;
    private DivinePowerSystem divinePowerSystem;

    private PlayerProfileData playerProfile;
    private PlayerProgressData playerProgress;
    private List<PlayerCardUnlockData> playerCardUnlocks = new List<PlayerCardUnlockData>();
    private List<PlayerDivinePowerUnlockData> playerDivinePowerUnlocks = new List<PlayerDivinePowerUnlockData>();
    private List<PlayerConsumableStackData> playerConsumables = new List<PlayerConsumableStackData>();
    private List<PendingHeroBonusData> pendingHeroBonuses = new List<PendingHeroBonusData>();

    private readonly List<CardSeedData> currentCardChoices = new List<CardSeedData>();
    private readonly List<ShopOfferData> currentShopOffers = new List<ShopOfferData>();
    private readonly List<string> equippedPowerIds = new List<string>();

    private RunSessionData currentRun;
    private SegmentRuntimeData currentSegment;
    private RunState currentState = RunState.None;
    private string summaryTitle = string.Empty;
    private string summaryMessage = string.Empty;
    private string feedbackMessage = string.Empty;
    private GameObject runtimeEnemyTemplate;

    public RunState CurrentState => currentState;
    public bool AllowsActorTurns => currentState == RunState.ExploringSegment;
    public RunSessionData CurrentRun => currentRun;
    public SegmentRuntimeData CurrentSegment => currentSegment;
    public IReadOnlyList<CardSeedData> CurrentCardChoices => currentCardChoices;
    public IReadOnlyList<ShopOfferData> CurrentShopOffers => currentShopOffers;
    public IReadOnlyList<DivinePowerSeedData> EquippedDivinePowers => divinePowerSystem?.EquippedPowers ?? System.Array.Empty<DivinePowerSeedData>();
    public PlayerGridMovement Player => player;
    public int CurrentGold => economySystem?.CurrentRunGold ?? 0;
    public int CurrentEmeralds => economySystem?.CurrentEmeralds ?? 0;
    public string SummaryTitle => summaryTitle;
    public string SummaryMessage => summaryMessage;
    public string FeedbackMessage => feedbackMessage;
    public bool CanUseDivinePowers => currentState == RunState.ExploringSegment;

    public string GetCurrentRunResultId()
    {
        switch (currentState)
        {
            case RunState.Completed:
                return "run_completed";
            case RunState.Failed:
                return "run_failed";
            default:
                return string.Empty;
        }
    }

    public string GetCurrentRunResultArtKey()
    {
        if (contentRepository == null)
            return string.Empty;

        string resultId = GetCurrentRunResultId();
        if (string.IsNullOrWhiteSpace(resultId))
            return string.Empty;

        RunResultSeedData result = contentRepository.GetRunResult(resultId);
        return result != null ? result.artKey : string.Empty;
    }

    public float GetDivinePowerCooldownSeconds(int slotIndex)
    {
        if (divinePowerSystem == null || slotIndex < 0 || slotIndex >= EquippedDivinePowers.Count)
            return 0f;

        return divinePowerSystem.GetCooldownRemaining(EquippedDivinePowers[slotIndex].powerId);
    }

    public float GetDivinePowerCooldownNormalized(int slotIndex)
    {
        if (divinePowerSystem == null || slotIndex < 0 || slotIndex >= EquippedDivinePowers.Count)
            return 0f;

        return divinePowerSystem.GetCooldownNormalized(EquippedDivinePowers[slotIndex].powerId);
    }

    public int GetDivinePowerCharges(int slotIndex)
    {
        if (divinePowerSystem == null || slotIndex < 0 || slotIndex >= EquippedDivinePowers.Count)
            return 0;

        return divinePowerSystem.GetCurrentCharges(EquippedDivinePowers[slotIndex].powerId);
    }

    public int GetDivinePowerMaxCharges(int slotIndex)
    {
        if (divinePowerSystem == null || slotIndex < 0 || slotIndex >= EquippedDivinePowers.Count)
            return 0;

        return divinePowerSystem.GetMaxCharges(EquippedDivinePowers[slotIndex].powerId);
    }

    public float GetDivinePowerActiveSeconds(int slotIndex)
    {
        if (divinePowerSystem == null || slotIndex < 0 || slotIndex >= EquippedDivinePowers.Count)
            return 0f;

        return divinePowerSystem.GetActiveRemaining(EquippedDivinePowers[slotIndex].powerId);
    }

    public string EffectiveHeroMode => divinePowerSystem == null || currentRun == null
        ? BalanceConfig.DefaultHeroMode
        : divinePowerSystem.GetEffectiveHeroMode(currentRun.heroMode);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        Bootstrap();
        if (autoStartOnPlay)
            StartRun();
    }

    void Update()
    {
        if (divinePowerSystem != null && player != null && currentState == RunState.ExploringSegment)
            divinePowerSystem.Tick(Time.deltaTime, player);

        if (!CanUseDivinePowers)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            TryActivateDivinePowerSlot(0);
        if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
            TryActivateDivinePowerSlot(1);
    }

    public void Bootstrap()
    {
        if (currentState != RunState.None)
            return;

        currentState = RunState.Bootstrapping;

        if (worldGrid == null)
            worldGrid = FindFirstObjectByType<WorldGrid>();

        if (player == null)
            player = FindFirstObjectByType<PlayerGridMovement>();

        if (heroAi == null && player != null)
            heroAi = player.GetComponent<HeroAIController>();

        contentRepository = new LocalJsonContentRepository();
        progressionRepository = new LocalFileProgressionRepository();
        runRepository = new InMemoryRunRepository();

        playerProfile = progressionRepository.LoadProfile(localPlayerId) ?? new PlayerProfileData();
        playerProgress = progressionRepository.LoadProgress(localPlayerId) ?? new PlayerProgressData();
        playerCardUnlocks = progressionRepository.LoadCardUnlocks(localPlayerId)?.ToList() ?? new List<PlayerCardUnlockData>();
        playerDivinePowerUnlocks = progressionRepository.LoadDivinePowerUnlocks(localPlayerId)?.ToList() ?? new List<PlayerDivinePowerUnlockData>();
        playerConsumables = progressionRepository.LoadConsumableStacks(localPlayerId)?.ToList() ?? new List<PlayerConsumableStackData>();

        EnsureProgressConsistency();

        economySystem = new EconomySystem(playerProgress, playerConsumables);
        shopSystem = new ShopSystem(contentRepository);
        divinePowerSystem = new DivinePowerSystem(contentRepository);
        segmentGenerator = new SegmentGenerator(contentRepository, runRepository);

        EquipUnlockedDivinePowers();
        runtimeEnemyTemplate = ResolveEnemyTemplate();
        HookPlayerEvents();

        currentState = RunState.Transitioning;
    }

    public void StartRun()
    {
        if (contentRepository == null)
            Bootstrap();

        currentCardChoices.Clear();
        currentShopOffers.Clear();
        pendingHeroBonuses.Clear();
        summaryTitle = string.Empty;
        summaryMessage = string.Empty;
        feedbackMessage = string.Empty;

        HeroStatsData heroStats = BuildHeroStatsFromProgress();
        string startingCardId = GetStartingCardId();
        int runLength = ResolveRunLength();

        currentRun = runRepository.CreateRun(localPlayerId, runLength, startingCardId, playerProfile.selectedHeroMode, equippedPowerIds);
        currentRun.heroMaxHealth = heroStats.maxHealth;
        currentRun.heroCurrentHealth = heroStats.currentHealth;
        currentRun.heroAttack = heroStats.attack;
        currentRun.heroDefense = heroStats.defense;
        currentRun.heroSpeed = heroStats.speed;

        player.ResetForRun();
        player.ConfigureFromRun(heroStats);
        economySystem.AttachRun(currentRun);
        divinePowerSystem.EquipPowers(equippedPowerIds);
        divinePowerSystem.OnSegmentStarted(player);

        playerProgress.totalRunsStarted += 1;
        SaveProgression();

        EnterNextSegment(startingCardId);
    }

    public void SelectCard(int index)
    {
        if (currentState != RunState.AwaitingCardChoice || index < 0 || index >= currentCardChoices.Count)
            return;

        string selectedCardId = currentCardChoices[index].cardId;
        runRepository.SaveSegmentChoices(currentRun.runId, currentRun.currentSegmentIndex, currentCardChoices, selectedCardId);
        currentCardChoices.Clear();
        currentShopOffers.Clear();
        EnterNextSegment(selectedCardId);
    }

    public void BuyShopOffer(int index)
    {
        if (currentState != RunState.AwaitingShopChoice || index < 0 || index >= currentShopOffers.Count)
            return;

        ShopPurchaseResult result = shopSystem.TryPurchase(currentShopOffers[index], economySystem, player, pendingHeroBonuses);
        feedbackMessage = result.feedback;
        if (!result.success)
            return;

        runRepository.AddEvent(currentRun.runId, currentSegment.segment.runSegmentId, "shop_purchase",
            $"{{\"offerId\":\"{currentShopOffers[index].offerId}\",\"goldRemaining\":{economySystem.CurrentRunGold}}}");

        if (result.rerollCardChoices)
        {
            currentCardChoices.Clear();
            currentCardChoices.AddRange(DrawCardChoices(BalanceConfig.CardChoiceCount));
        }

        currentShopOffers.RemoveAt(index);
    }

    public void SkipShop()
    {
        if (currentState != RunState.AwaitingShopChoice)
            return;

        currentShopOffers.Clear();
        currentState = RunState.AwaitingCardChoice;
        feedbackMessage = "La botiga ha quedat enrere.";
    }

    public void TryActivateDivinePowerSlot(int slotIndex)
    {
        if (divinePowerSystem == null || player == null || currentRun == null)
            return;

        if (divinePowerSystem.TryActivate(slotIndex, player, out string feedback))
        {
            feedbackMessage = feedback;
            runRepository.AddEvent(currentRun.runId, currentSegment != null ? currentSegment.segment.runSegmentId : 0L, "divine_power_used",
                $"{{\"slot\":{slotIndex + 1},\"powerId\":\"{EquippedDivinePowers[slotIndex].powerId}\"}}");
            return;
        }

        feedbackMessage = feedback;
    }

    public void OnEnemyDefeated(Enemy enemy)
    {
        if (enemy == null || currentRun == null || currentSegment == null)
            return;

        runRepository.MarkEnemyDefeated(enemy.RunSegmentEnemyId);
        runRepository.AddEvent(currentRun.runId, currentSegment.segment.runSegmentId, "enemy_defeated",
            $"{{\"enemyId\":\"{enemy.EnemyId}\",\"gold\":{enemy.RewardGold}}}");

        if (enemy.RewardGold > 0)
        {
            economySystem.GrantRunGold(enemy.RewardGold);
            runRepository.AddReward(currentRun.runId, currentSegment.segment.runSegmentId, "gold", enemy.EnemyId, enemy.RewardGold, "{}");
        }

        playerProgress.totalEnemiesDefeated += 1;
        SaveProgression();
    }

    public void OnChestOpened(Chest chest)
    {
        if (chest == null || currentRun == null || currentSegment == null || chest.IsOpened)
            return;

        int gold = chest.GoldReward;
        int emeralds = chest.EmeraldReward;
        string chestTier = chest.ChestTier;
        chest.Open();

        if (gold > 0)
        {
            economySystem.GrantRunGold(gold);
            runRepository.AddReward(currentRun.runId, currentSegment.segment.runSegmentId, "gold", $"chest_{chestTier}", gold, "{}");
        }

        if (emeralds > 0)
        {
            economySystem.GrantEmeralds(emeralds);
            runRepository.AddReward(currentRun.runId, currentSegment.segment.runSegmentId, "emerald", $"chest_{chestTier}", emeralds, "{}");
        }

        feedbackMessage = emeralds > 0
            ? $"Cofre {chestTier} obert: +{gold} or i +{emeralds} esmeralda."
            : $"Cofre {chestTier} obert: +{gold} or.";

        runRepository.AddEvent(currentRun.runId, currentSegment.segment.runSegmentId, "chest_opened",
            $"{{\"tier\":\"{chestTier}\",\"gold\":{gold},\"emeralds\":{emeralds}}}");
        SaveProgression();
    }

    public void OnPlayerReachedSegmentGoal()
    {
        if (currentState != RunState.ExploringSegment || currentRun == null || currentSegment == null)
            return;

        player.PauseHero();
        currentRun.segmentsCleared += 1;
        currentSegment.segment.state = "cleared";
        currentSegment.segment.heroHealthOnExit = player.CurrentHealth;
        currentRun.heroCurrentHealth = player.CurrentHealth;
        currentRun.currentSegmentIndex += 1;

        ApplyCardRewards(currentSegment.card.cardId);
        divinePowerSystem.OnSegmentEnded(player);
        AdvancePendingBonuses();

        if (currentRun.segmentsCleared >= currentRun.targetSegmentCount)
        {
            CompleteRun();
            return;
        }

        currentCardChoices.Clear();
        currentCardChoices.AddRange(DrawCardChoices(BalanceConfig.CardChoiceCount));
        if (currentCardChoices.Count == 0)
        {
            CompleteRun();
            return;
        }

        bool shopShouldOpen = playerProgress.shopEnabled && BalanceConfig.ShouldOpenShop(currentRun.segmentsCleared, currentRun.targetSegmentCount);
        if (shopShouldOpen)
        {
            currentShopOffers.Clear();
            currentShopOffers.AddRange(shopSystem.GenerateOffers());
            currentState = RunState.AwaitingShopChoice;
            feedbackMessage = "Has trobat una botiga entre segments.";
            runRepository.AddEvent(currentRun.runId, currentSegment.segment.runSegmentId, "shop_opened",
                $"{{\"segmentCleared\":{currentRun.segmentsCleared}}}");
        }
        else
        {
            currentState = RunState.AwaitingCardChoice;
            feedbackMessage = "Tria la propera carta de bioma.";
        }

        runRepository.UpdateRun(currentRun);
    }

    public void FailRun()
    {
        if (currentRun == null || currentState == RunState.Failed || currentState == RunState.Completed)
            return;

        currentState = RunState.Failed;
        player.PauseHero();
        currentRun.status = "failed";
        currentRun.heroCurrentHealth = Mathf.Max(0, player.CurrentHealth);
        economySystem.GrantEmeralds(BalanceConfig.FailedRunEmeraldReward);
        playerProgress.failedRuns += 1;
        playerProgress.highestSegmentReached = Mathf.Max(playerProgress.highestSegmentReached, currentRun.segmentsCleared + 1);
        summaryTitle = "Run fallida";
        summaryMessage = $"Has caigut al segment {currentRun.segmentsCleared + 1}. Recompensa: {BalanceConfig.FailedRunEmeraldReward} esmeralda.";
        runRepository.AddEvent(currentRun.runId, currentSegment != null ? currentSegment.segment.runSegmentId : 0L, "run_failed", "{}");
        SaveProgression();
    }

    public void CompleteRun()
    {
        if (currentRun == null || currentState == RunState.Completed)
            return;

        currentState = RunState.Completed;
        player.PauseHero();
        currentRun.status = "completed";
        currentRun.heroCurrentHealth = Mathf.Max(0, player.CurrentHealth);
        economySystem.GrantEmeralds(BalanceConfig.CompleteRunEmeraldReward);
        playerProgress.completedRuns += 1;
        playerProgress.highestSegmentReached = Mathf.Max(playerProgress.highestSegmentReached, currentRun.segmentsCleared);
        summaryTitle = "Run completada";
        summaryMessage = $"Has superat {currentRun.segmentsCleared} segments. Recompensa: {BalanceConfig.CompleteRunEmeraldReward} esmeraldes.";
        runRepository.AddEvent(currentRun.runId, currentSegment != null ? currentSegment.segment.runSegmentId : 0L, "run_completed", "{}");
        SaveProgression();
    }

    private void EnterNextSegment(string cardId)
    {
        currentState = RunState.Transitioning;
        currentSegment = segmentGenerator.GenerateSegment(currentRun, cardId);
        if (currentSegment == null)
        {
            FailRun();
            return;
        }

        ApplySegmentBonuses(currentSegment);
        worldGrid.GenerateSegment(currentSegment, runtimeEnemyTemplate);
        player.SetGridPosition(worldGrid.EntryPosition, true);
        player.ResumeHero();
        heroAi?.ResetDecisionTimer();

        if (currentSegment.modifierRuntime.heroHealOnEnter > 0)
            player.Heal(currentSegment.modifierRuntime.heroHealOnEnter);

        currentRun.heroCurrentHealth = player.CurrentHealth;
        currentSegment.segment.state = "entered";
        currentState = RunState.ExploringSegment;
        feedbackMessage = $"Segment {currentSegment.segment.segmentIndex}: {currentSegment.card.displayName}.";
        runRepository.UpdateRun(currentRun);
    }
    private void ApplySegmentBonuses(SegmentRuntimeData segment)
    {
        int attackBonus = 0;
        int defenseBonus = 0;
        int maxHealthBonus = 0;
        float speedMultiplier = Mathf.Max(0.25f, segment.modifierRuntime.heroSpeedMultiplier);

        for (int i = 0; i < pendingHeroBonuses.Count; i++)
        {
            attackBonus += pendingHeroBonuses[i].attackBonus;
            defenseBonus += pendingHeroBonuses[i].defenseBonus;
            maxHealthBonus += pendingHeroBonuses[i].maxHealthBonus;
            speedMultiplier *= Mathf.Max(0.25f, pendingHeroBonuses[i].speedMultiplierBonus <= 0f ? 1f : pendingHeroBonuses[i].speedMultiplierBonus);
        }

        player.ConfigureFromRun(new HeroStatsData
        {
            maxHealth = currentRun.heroMaxHealth,
            currentHealth = currentRun.heroCurrentHealth,
            attack = currentRun.heroAttack,
            defense = currentRun.heroDefense,
            speed = currentRun.heroSpeed
        });

        player.SetSegmentBonuses(attackBonus, defenseBonus, maxHealthBonus, speedMultiplier);
        divinePowerSystem.OnSegmentStarted(player);
    }

    private void AdvancePendingBonuses()
    {
        for (int i = pendingHeroBonuses.Count - 1; i >= 0; i--)
        {
            pendingHeroBonuses[i].durationSegments -= 1;
            if (pendingHeroBonuses[i].durationSegments <= 0)
                pendingHeroBonuses.RemoveAt(i);
        }
    }

    private void ApplyCardRewards(string cardId)
    {
        IReadOnlyList<CardRewardPoolSeedData> rewards = contentRepository.GetCardRewardPool(cardId);
        if (rewards == null || rewards.Count == 0)
            return;

        CardRewardPoolSeedData reward = WeightedSelectionUtility.PickWeighted(rewards, item => item.weight);
        if (reward == null)
            return;

        switch (reward.rewardType)
        {
            case "gold":
                economySystem.GrantRunGold(reward.quantity);
                runRepository.AddReward(currentRun.runId, currentSegment.segment.runSegmentId, reward.rewardType, reward.rewardId, reward.quantity, "{}");
                feedbackMessage = $"Recompensa: +{reward.quantity} or.";
                break;
            case "heal":
                player.Heal(reward.quantity);
                runRepository.AddReward(currentRun.runId, currentSegment.segment.runSegmentId, reward.rewardType, reward.rewardId, reward.quantity, "{}");
                feedbackMessage = $"Recompensa: +{reward.quantity} vida.";
                break;
            case "consumable":
                economySystem.GrantConsumable(reward.rewardId, reward.quantity);
                runRepository.AddReward(currentRun.runId, currentSegment.segment.runSegmentId, reward.rewardType, reward.rewardId, reward.quantity, "{}");
                feedbackMessage = $"Consumible obtingut: {reward.rewardId}.";
                break;
            case "relic":
                UnlockRelic(reward.rewardId);
                ApplyRelicToCurrentRun(reward.rewardId);
                runRepository.AddReward(currentRun.runId, currentSegment.segment.runSegmentId, reward.rewardType, reward.rewardId, reward.quantity, "{}");
                feedbackMessage = $"Relic trobada: {reward.rewardId}.";
                break;
            case "card_unlock":
                UnlockCard(reward.rewardId);
                runRepository.AddReward(currentRun.runId, currentSegment.segment.runSegmentId, reward.rewardType, reward.rewardId, reward.quantity, "{}");
                feedbackMessage = $"Carta desbloquejada: {reward.rewardId}.";
                break;
            case "emerald":
                economySystem.GrantEmeralds(reward.quantity);
                runRepository.AddReward(currentRun.runId, currentSegment.segment.runSegmentId, reward.rewardType, reward.rewardId, reward.quantity, "{}");
                feedbackMessage = $"Esmeraldes guanyades: {reward.quantity}.";
                break;
        }

        currentRun.heroCurrentHealth = player.CurrentHealth;
        SaveProgression();
    }

    private void ApplyRelicToCurrentRun(string relicId)
    {
        RelicSeedData relic = contentRepository.GetRelics().FirstOrDefault(item => item.relicId == relicId);
        if (relic == null)
            return;

        RelicEffectConfigData effect = JsonSeedParser.ParseRelicEffect(relic.effectConfigJson);
        currentRun.heroMaxHealth += effect.heroMaxHealthBonus;
        currentRun.heroAttack += effect.heroAttackBonus;
        currentRun.heroSpeed += effect.heroSpeedBonus;
        currentRun.heroCurrentHealth = Mathf.Min(currentRun.heroCurrentHealth + effect.heroMaxHealthBonus, currentRun.heroMaxHealth);
    }

    private IReadOnlyList<CardSeedData> DrawCardChoices(int amount)
    {
        List<string> unlockedIds = GetUnlockedCardIds();
        List<CardSeedData> pool = contentRepository.GetCards()
            .Where(card => card.isActive && unlockedIds.Contains(card.cardId))
            .OrderBy(card => card.sortOrder)
            .ToList();

        List<CardSeedData> result = new List<CardSeedData>();
        while (result.Count < amount && pool.Count > 0)
        {
            int index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }

    private HeroStatsData BuildHeroStatsFromProgress()
    {
        HeroStatsData stats = new HeroStatsData();
        List<string> unlockedRelics = playerProgress.unlockedRelicIds?.ToList() ?? new List<string>();

        foreach (string relicId in unlockedRelics)
        {
            RelicSeedData relic = contentRepository.GetRelics().FirstOrDefault(item => item.relicId == relicId);
            if (relic == null)
                continue;

            RelicEffectConfigData effect = JsonSeedParser.ParseRelicEffect(relic.effectConfigJson);
            stats.maxHealth += effect.heroMaxHealthBonus;
            stats.attack += effect.heroAttackBonus;
            stats.speed += effect.heroSpeedBonus;
        }

        stats.currentHealth = stats.maxHealth;
        return stats;
    }

    private string GetStartingCardId()
    {
        List<string> unlockedIds = GetUnlockedCardIds();
        CardSeedData card = contentRepository.GetCards()
            .Where(item => item.isActive && unlockedIds.Contains(item.cardId))
            .OrderBy(item => item.startsUnlocked ? 0 : 1)
            .ThenBy(item => item.sortOrder)
            .FirstOrDefault();

        return card != null ? card.cardId : contentRepository.GetCards().First().cardId;
    }

    private int ResolveRunLength()
    {
        int preferred = targetRunLength > 0 ? targetRunLength : playerProfile.preferredRunLength;
        if (preferred >= BalanceConfig.DefaultLongRunLength && playerProgress.run7Unlocked)
            return BalanceConfig.DefaultLongRunLength;

        return BalanceConfig.DefaultShortRunLength;
    }

    private void HookPlayerEvents()
    {
        if (player == null)
            return;

        player.ArrivedAtCell -= HandlePlayerArrived;
        player.ArrivedAtCell += HandlePlayerArrived;
        player.Died -= FailRun;
        player.Died += FailRun;
        player.HealthChanged -= HandlePlayerHealthChanged;
        player.HealthChanged += HandlePlayerHealthChanged;
    }

    private void HandlePlayerArrived(Vector2Int position)
    {
        if (currentState != RunState.ExploringSegment)
            return;

        Chest chest = worldGrid.GetChestAt(position);
        if (chest != null)
            OnChestOpened(chest);

        Enemy enemy = worldGrid.GetEnemyAt(position);
        if (enemy != null)
        {
            CombatSystem.ResolveMelee(player, enemy);
            return;
        }

        if (position == worldGrid.ExitPosition)
            OnPlayerReachedSegmentGoal();
    }

    private void HandlePlayerHealthChanged(int currentHealth, int maxHealth)
    {
        if (currentRun == null)
            return;

        currentRun.heroCurrentHealth = currentHealth;
        currentRun.heroMaxHealth = maxHealth;
        runRepository.UpdateRun(currentRun);
    }

    private void EnsureProgressConsistency()
    {
        if (playerProgress.unlockedCardIds == null || playerProgress.unlockedCardIds.Length == 0)
        {
            playerProgress.unlockedCardIds = playerCardUnlocks.Select(item => item.cardId).ToArray();
        }

        if (playerCardUnlocks.Count == 0)
        {
            playerCardUnlocks = playerProgress.unlockedCardIds
                .Select(cardId => new PlayerCardUnlockData { playerId = localPlayerId, cardId = cardId, unlockSource = "seed" })
                .ToList();
        }

        if (playerProgress.unlockedDivinePowerIds == null || playerProgress.unlockedDivinePowerIds.Length == 0)
        {
            playerProgress.unlockedDivinePowerIds = playerDivinePowerUnlocks.Select(item => item.powerId).ToArray();
        }

        if (playerDivinePowerUnlocks.Count == 0)
        {
            playerDivinePowerUnlocks = playerProgress.unlockedDivinePowerIds
                .Select(powerId => new PlayerDivinePowerUnlockData { playerId = localPlayerId, powerId = powerId, unlockSource = "seed" })
                .ToList();
        }
    }

    private void EquipUnlockedDivinePowers()
    {
        equippedPowerIds.Clear();
        HashSet<string> unlockedPowerIds = new HashSet<string>(playerProgress.unlockedDivinePowerIds ?? System.Array.Empty<string>());
        foreach (DivinePowerSeedData power in contentRepository.GetDivinePowers().OrderBy(item => item.sortOrder))
        {
            if (!power.isActive || !unlockedPowerIds.Contains(power.powerId))
                continue;

            equippedPowerIds.Add(power.powerId);
            if (equippedPowerIds.Count >= BalanceConfig.MaxDivinePowerSlots)
                break;
        }
    }

    private void SaveProgression()
    {
        playerProgress.totalCardsUnlocked = GetUnlockedCardIds().Count;
        playerProgress.unlockedCardIds = GetUnlockedCardIds().ToArray();
        playerProgress.unlockedDivinePowerIds = playerDivinePowerUnlocks.Select(item => item.powerId).Distinct().ToArray();
        playerProgress.softCurrency = 0;
        progressionRepository.Save(playerProfile, playerProgress, playerCardUnlocks, playerDivinePowerUnlocks, playerConsumables);
    }

    private List<string> GetUnlockedCardIds()
    {
        HashSet<string> unlocked = new HashSet<string>(playerProgress.unlockedCardIds ?? System.Array.Empty<string>());
        for (int i = 0; i < playerCardUnlocks.Count; i++)
            unlocked.Add(playerCardUnlocks[i].cardId);
        return unlocked.ToList();
    }

    private void UnlockCard(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId) || GetUnlockedCardIds().Contains(cardId))
            return;

        playerCardUnlocks.Add(new PlayerCardUnlockData
        {
            playerId = localPlayerId,
            cardId = cardId,
            unlockSource = "run_reward"
        });
        if (currentRun != null) currentRun.cardsUnlockedThisRun += 1;
    }

    private void UnlockRelic(string relicId)
    {
        List<string> unlockedRelics = playerProgress.unlockedRelicIds?.ToList() ?? new List<string>();
        if (unlockedRelics.Contains(relicId))
            return;

        unlockedRelics.Add(relicId);
        playerProgress.unlockedRelicIds = unlockedRelics.ToArray();
    }

    private GameObject ResolveEnemyTemplate()
    {
        GameObject sourceTemplate = fallbackEnemyTemplate;
        if (sourceTemplate == null)
        {
            Enemy sceneEnemy = FindFirstObjectByType<Enemy>();
            if (sceneEnemy != null)
                sourceTemplate = sceneEnemy.gameObject;
        }

        GameObject template = sourceTemplate != null
            ? Instantiate(sourceTemplate, transform)
            : CreateRuntimeEnemyTemplate();

        template.name = "RuntimeEnemyTemplate";
        template.SetActive(false);

        if (template.GetComponent<Enemy>() == null)
            template.AddComponent<Enemy>();
        if (template.GetComponent<EnemyGridMovement>() == null)
            template.AddComponent<EnemyGridMovement>();
        if (template.GetComponent<ProceduralEnemyRenderer>() == null)
            template.AddComponent<ProceduralEnemyRenderer>();
        if (template.GetComponent<SpriteRenderer>() == null)
        {
            SpriteRenderer renderer = template.AddComponent<SpriteRenderer>();
            renderer.sprite = ProceduralPixelUtility.GetOrCreateSquareSprite();
            renderer.sortingOrder = 12;
        }

        return template;
    }

    private GameObject CreateRuntimeEnemyTemplate()
    {
        GameObject template = new GameObject("RuntimeEnemyTemplate");
        template.transform.SetParent(transform);
        SpriteRenderer spriteRenderer = template.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = ProceduralPixelUtility.GetOrCreateSquareSprite();
        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = 12;
        template.AddComponent<Enemy>();
        template.AddComponent<EnemyGridMovement>();
        template.AddComponent<ProceduralEnemyRenderer>();
        return template;
    }
}




