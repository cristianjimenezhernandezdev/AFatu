namespace ArchitectusFati.Api.Contracts;

public sealed record CardDefinition(
    string CardId,
    string DisplayName,
    string Description,
    bool StartsUnlocked,
    string BiomeId,
    string FloorColorHex,
    string WallColorHex,
    int SegmentWidth,
    int SegmentHeight,
    int EntryX,
    int ExitX,
    float ObstacleChance,
    float EnemyChance,
    IReadOnlyList<string> EnemyIds);

public sealed record BiomeDefinition(
    string BiomeId,
    string DisplayName,
    string Description,
    string FloorColorHex,
    string WallColorHex,
    string AmbientConfigJson);

public sealed record EnemyArchetypeDefinition(
    string EnemyId,
    string DisplayName,
    string Description,
    int MaxHealth,
    int Attack,
    string MovementPattern,
    string Rarity,
    string? SpriteKey,
    string BehaviorConfigJson,
    string RewardConfigJson);

public sealed record WorldModifierDefinition(
    string ModifierId,
    string DisplayName,
    string Description,
    string ModifierType,
    string Rarity,
    string StackMode,
    string EffectConfigJson,
    bool IsPositive);

public sealed record RelicDefinition(
    string RelicId,
    string DisplayName,
    string Description,
    string Rarity,
    string EffectConfigJson);

public sealed record ConsumableDefinition(
    string ConsumableId,
    string DisplayName,
    string Description,
    int MaxStack,
    string EffectConfigJson);

public sealed record BootstrapContentResponse(
    IReadOnlyList<BiomeDefinition> Biomes,
    IReadOnlyList<EnemyArchetypeDefinition> Enemies,
    IReadOnlyList<WorldModifierDefinition> Modifiers,
    IReadOnlyList<RelicDefinition> Relics,
    IReadOnlyList<ConsumableDefinition> Consumables,
    IReadOnlyList<CardDefinition> Cards);

public sealed record CardsResponse(IReadOnlyList<CardDefinition> Cards);

public sealed record PlayerProgressDto(
    IReadOnlyList<string> UnlockedCardIds,
    int CompletedRuns,
    int FailedRuns,
    int TotalRunsStarted,
    int TotalCardsUnlocked);

public sealed record PlayerProgressResponse(string PlayerId, PlayerProgressDto Progress);

public sealed record UpsertPlayerProgressRequest(PlayerProgressDto Progress);

public sealed record RunSessionDto(
    long RunId,
    string PlayerId,
    string Status,
    string? StartingCardId,
    int CurrentSegmentIndex,
    int SegmentsCleared,
    int HeroMaxHealth,
    int HeroCurrentHealth,
    int HeroAttack,
    int CardsUnlockedThisRun,
    int GoldEarned,
    string SummaryJson,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    DateTimeOffset UpdatedAt);

public sealed record RunSessionResponse(RunSessionDto Run);

public sealed record StartRunRequest(
    string? StartingCardId,
    string? RunSeed,
    int HeroMaxHealth,
    int HeroCurrentHealth,
    int HeroAttack);

public sealed record UpsertRunSegmentRequest(
    int SegmentIndex,
    string CardId,
    string BiomeId,
    int SegmentWidth,
    int SegmentHeight,
    int EntryX,
    int ExitX,
    float ObstacleChance,
    float EnemyChance,
    string? GeneratedSeed,
    string State,
    int? HeroHealthOnEnter,
    int? HeroHealthOnExit,
    IReadOnlyList<string>? OfferedCardIds,
    string? SelectedCardId);

public sealed record RunSegmentDto(
    long RunSegmentId,
    long RunId,
    int SegmentIndex,
    string CardId,
    string BiomeId,
    int SegmentWidth,
    int SegmentHeight,
    int EntryX,
    int ExitX,
    float ObstacleChance,
    float EnemyChance,
    string? GeneratedSeed,
    string State,
    int? HeroHealthOnEnter,
    int? HeroHealthOnExit,
    IReadOnlyList<string> OfferedCardIds,
    string? SelectedCardId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ClearedAt);

public sealed record RunSegmentResponse(RunSegmentDto Segment);

public sealed record FinishRunRequest(
    string Status,
    int SegmentsCleared,
    int CurrentSegmentIndex,
    int HeroCurrentHealth,
    int CardsUnlockedThisRun,
    int GoldEarned,
    string? SummaryJson);

public sealed record HealthResponse(string Status, DateTimeOffset UtcNow, bool DatabaseReachable);

public sealed record ErrorResponse(string Code, string Message);
