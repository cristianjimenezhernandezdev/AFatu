using System.Text.Json;
using ArchitectusFati.Api.Contracts;
using Npgsql;

namespace ArchitectusFati.Api.Data;

public sealed class GameRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly NpgsqlDataSource _dataSource;

    public GameRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        await using NpgsqlCommand command = _dataSource.CreateCommand("select 1;");
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    public async Task<IReadOnlyList<CardDefinition>> GetActiveCardsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            select
                card_id,
                display_name,
                description,
                starts_unlocked,
                biome_id,
                floor_color_hex,
                wall_color_hex,
                segment_width,
                segment_height,
                entry_x,
                exit_x,
                obstacle_chance,
                enemy_chance,
                enemy_ids::text
            from cards
            where is_active = true
            order by sort_order asc, display_name asc;
            """;

        List<CardDefinition> cards = new();

        await using NpgsqlCommand command = _dataSource.CreateCommand(sql);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            cards.Add(new CardDefinition(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetBoolean(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetInt32(7),
                reader.GetInt32(8),
                reader.GetInt32(9),
                reader.GetInt32(10),
                reader.GetFloat(11),
                reader.GetFloat(12),
                ParseStringList(reader.GetString(13))));
        }

        return cards;
    }

    public async Task<PlayerProgressDto?> GetProgressAsync(string playerId, CancellationToken cancellationToken)
    {
        const string sql = """
            select
                unlocked_card_ids::text,
                completed_runs,
                failed_runs,
                total_runs_started,
                total_cards_unlocked
            from player_progress
            where player_id = @player_id;
            """;

        await using NpgsqlCommand command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("player_id", NormalizePlayerId(playerId));

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new PlayerProgressDto(
            ParseStringList(reader.GetString(0)),
            reader.GetInt32(1),
            reader.GetInt32(2),
            reader.GetInt32(3),
            reader.GetInt32(4));
    }

    public async Task<PlayerProgressDto> UpsertProgressAsync(string playerId, PlayerProgressDto progress, CancellationToken cancellationToken)
    {
        PlayerProgressDto normalizedProgress = NormalizeProgress(progress);

        const string sql = """
            insert into player_progress (
                player_id,
                unlocked_card_ids,
                completed_runs,
                failed_runs,
                total_runs_started,
                total_cards_unlocked,
                updated_at
            )
            values (
                @player_id,
                cast(@unlocked_card_ids as jsonb),
                @completed_runs,
                @failed_runs,
                @total_runs_started,
                @total_cards_unlocked,
                now()
            )
            on conflict (player_id) do update
            set
                unlocked_card_ids = excluded.unlocked_card_ids,
                completed_runs = excluded.completed_runs,
                failed_runs = excluded.failed_runs,
                total_runs_started = excluded.total_runs_started,
                total_cards_unlocked = excluded.total_cards_unlocked,
                updated_at = now()
            returning
                unlocked_card_ids::text,
                completed_runs,
                failed_runs,
                total_runs_started,
                total_cards_unlocked;
            """;

        await using NpgsqlCommand command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("player_id", NormalizePlayerId(playerId));
        command.Parameters.AddWithValue("unlocked_card_ids", JsonSerializer.Serialize(normalizedProgress.UnlockedCardIds, JsonOptions));
        command.Parameters.AddWithValue("completed_runs", Math.Max(0, normalizedProgress.CompletedRuns));
        command.Parameters.AddWithValue("failed_runs", Math.Max(0, normalizedProgress.FailedRuns));
        command.Parameters.AddWithValue("total_runs_started", Math.Max(0, normalizedProgress.TotalRunsStarted));
        command.Parameters.AddWithValue("total_cards_unlocked", Math.Max(0, normalizedProgress.TotalCardsUnlocked));

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        return new PlayerProgressDto(
            ParseStringList(reader.GetString(0)),
            reader.GetInt32(1),
            reader.GetInt32(2),
            reader.GetInt32(3),
            reader.GetInt32(4));
    }

    private static string NormalizePlayerId(string playerId)
    {
        return playerId.Trim().ToLowerInvariant();
    }

    private static PlayerProgressDto NormalizeProgress(PlayerProgressDto progress)
    {
        List<string> uniqueUnlockedCardIds = new();
        HashSet<string> seenCardIds = new(StringComparer.OrdinalIgnoreCase);

        foreach (string? rawCardId in progress.UnlockedCardIds ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(rawCardId))
                continue;

            string normalizedCardId = rawCardId.Trim().ToLowerInvariant();
            if (!seenCardIds.Add(normalizedCardId))
                continue;

            uniqueUnlockedCardIds.Add(normalizedCardId);
        }

        return new PlayerProgressDto(
            uniqueUnlockedCardIds,
            Math.Max(0, progress.CompletedRuns),
            Math.Max(0, progress.FailedRuns),
            Math.Max(0, progress.TotalRunsStarted),
            uniqueUnlockedCardIds.Count);
    }

    private static IReadOnlyList<string> ParseStringList(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<string>();

        try
        {
            return JsonSerializer.Deserialize<string[]>(json, JsonOptions) ?? Array.Empty<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }
}
