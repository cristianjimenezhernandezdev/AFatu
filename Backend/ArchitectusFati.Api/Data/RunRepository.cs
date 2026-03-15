using System.Text.Json;
using ArchitectusFati.Api.Contracts;
using Npgsql;

namespace ArchitectusFati.Api.Data;

public sealed class RunRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public RunRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<RunSessionDto?> GetRunAsync(long runId, CancellationToken cancellationToken)
    {
        const string sql = """
            select
                run_id, player_id, status, starting_card_id, current_segment_index,
                segments_cleared, hero_max_health, hero_current_health, hero_attack,
                cards_unlocked_this_run, gold_earned, summary::text,
                started_at, ended_at, updated_at
            from run_sessions
            where run_id = @run_id;
            """;

        await using NpgsqlCommand command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("run_id", runId);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapRunSession(reader) : null;
    }

    public async Task<RunSessionDto?> GetActiveRunAsync(string playerId, CancellationToken cancellationToken)
    {
        const string sql = """
            select
                run_id, player_id, status, starting_card_id, current_segment_index,
                segments_cleared, hero_max_health, hero_current_health, hero_attack,
                cards_unlocked_this_run, gold_earned, summary::text,
                started_at, ended_at, updated_at
            from run_sessions
            where player_id = @player_id and status = 'active'
            order by started_at desc
            limit 1;
            """;

        await using NpgsqlCommand command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("player_id", NormalizePlayerId(playerId));
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapRunSession(reader) : null;
    }

    public async Task<RunSessionDto> StartRunAsync(string playerId, StartRunRequest request, CancellationToken cancellationToken)
    {
        string normalizedPlayerId = NormalizePlayerId(playerId);

        await using NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        await EnsurePlayerExistsAsync(connection, transaction, normalizedPlayerId, cancellationToken);
        await AbandonActiveRunsAsync(connection, transaction, normalizedPlayerId, cancellationToken);
        await IncrementRunsStartedAsync(connection, transaction, normalizedPlayerId, cancellationToken);

        const string sql = """
            insert into run_sessions (
                player_id, run_seed, status, starting_card_id, current_segment_index,
                segments_cleared, hero_max_health, hero_current_health, hero_attack,
                cards_unlocked_this_run, gold_earned, summary, started_at, updated_at
            )
            values (
                @player_id, @run_seed, 'active', @starting_card_id, 1,
                0, @hero_max_health, @hero_current_health, @hero_attack,
                0, 0, '{}'::jsonb, now(), now()
            )
            returning
                run_id, player_id, status, starting_card_id, current_segment_index,
                segments_cleared, hero_max_health, hero_current_health, hero_attack,
                cards_unlocked_this_run, gold_earned, summary::text,
                started_at, ended_at, updated_at;
            """;

        await using NpgsqlCommand command = new(sql, connection, transaction);
        command.Parameters.AddWithValue("player_id", normalizedPlayerId);
        command.Parameters.AddWithValue("run_seed", (object?)request.RunSeed ?? DBNull.Value);
        command.Parameters.AddWithValue("starting_card_id", NormalizeNullableId(request.StartingCardId));
        command.Parameters.AddWithValue("hero_max_health", Math.Max(1, request.HeroMaxHealth));
        command.Parameters.AddWithValue("hero_current_health", Math.Max(0, request.HeroCurrentHealth));
        command.Parameters.AddWithValue("hero_attack", Math.Max(0, request.HeroAttack));

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        RunSessionDto run = MapRunSession(reader);

        await transaction.CommitAsync(cancellationToken);
        return run;
    }

    public async Task<RunSegmentDto?> UpsertRunSegmentAsync(long runId, UpsertRunSegmentRequest request, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string segmentSql = """
            insert into run_segments (
                run_id, segment_index, card_id, biome_id, segment_width, segment_height,
                entry_x, exit_x, obstacle_chance, enemy_chance, generated_seed, state,
                hero_health_on_enter, hero_health_on_exit, created_at, cleared_at
            )
            values (
                @run_id, @segment_index, @card_id, @biome_id, @segment_width, @segment_height,
                @entry_x, @exit_x, @obstacle_chance, @enemy_chance, @generated_seed, @state,
                @hero_health_on_enter, @hero_health_on_exit, now(),
                case when @state = 'cleared' then now() else null end
            )
            on conflict (run_id, segment_index) do update
            set
                card_id = excluded.card_id,
                biome_id = excluded.biome_id,
                segment_width = excluded.segment_width,
                segment_height = excluded.segment_height,
                entry_x = excluded.entry_x,
                exit_x = excluded.exit_x,
                obstacle_chance = excluded.obstacle_chance,
                enemy_chance = excluded.enemy_chance,
                generated_seed = excluded.generated_seed,
                state = excluded.state,
                hero_health_on_enter = excluded.hero_health_on_enter,
                hero_health_on_exit = excluded.hero_health_on_exit,
                cleared_at = case when excluded.state = 'cleared' then now() else run_segments.cleared_at end
            returning
                run_segment_id, run_id, segment_index, card_id, biome_id, segment_width,
                segment_height, entry_x, exit_x, obstacle_chance, enemy_chance,
                generated_seed, state, hero_health_on_enter, hero_health_on_exit,
                created_at, cleared_at;
            """;

        await using NpgsqlCommand segmentCommand = new(segmentSql, connection, transaction);
        segmentCommand.Parameters.AddWithValue("run_id", runId);
        segmentCommand.Parameters.AddWithValue("segment_index", Math.Max(1, request.SegmentIndex));
        segmentCommand.Parameters.AddWithValue("card_id", NormalizeRequiredId(request.CardId));
        segmentCommand.Parameters.AddWithValue("biome_id", NormalizeRequiredId(request.BiomeId));
        segmentCommand.Parameters.AddWithValue("segment_width", Math.Max(5, request.SegmentWidth));
        segmentCommand.Parameters.AddWithValue("segment_height", Math.Max(5, request.SegmentHeight));
        segmentCommand.Parameters.AddWithValue("entry_x", request.EntryX);
        segmentCommand.Parameters.AddWithValue("exit_x", request.ExitX);
        segmentCommand.Parameters.AddWithValue("obstacle_chance", Math.Clamp(request.ObstacleChance, 0f, 1f));
        segmentCommand.Parameters.AddWithValue("enemy_chance", Math.Clamp(request.EnemyChance, 0f, 1f));
        segmentCommand.Parameters.AddWithValue("generated_seed", (object?)request.GeneratedSeed ?? DBNull.Value);
        segmentCommand.Parameters.AddWithValue("state", NormalizeSegmentState(request.State));
        segmentCommand.Parameters.AddWithValue("hero_health_on_enter", (object?)request.HeroHealthOnEnter ?? DBNull.Value);
        segmentCommand.Parameters.AddWithValue("hero_health_on_exit", (object?)request.HeroHealthOnExit ?? DBNull.Value);

        await using NpgsqlDataReader segmentReader = await segmentCommand.ExecuteReaderAsync(cancellationToken);
        if (!await segmentReader.ReadAsync(cancellationToken))
            return null;

        RunSegmentDto segment = MapRunSegment(segmentReader, NormalizeStringList(request.OfferedCardIds), NormalizeNullableId(request.SelectedCardId) as string);
        await segmentReader.CloseAsync();

        const string deleteChoicesSql = "delete from run_segment_choices where run_id = @run_id and segment_index = @segment_index;";
        await using NpgsqlCommand deleteChoicesCommand = new(deleteChoicesSql, connection, transaction);
        deleteChoicesCommand.Parameters.AddWithValue("run_id", runId);
        deleteChoicesCommand.Parameters.AddWithValue("segment_index", Math.Max(1, request.SegmentIndex));
        await deleteChoicesCommand.ExecuteNonQueryAsync(cancellationToken);

        for (int i = 0; i < segment.OfferedCardIds.Count; i++)
        {
            const string choiceSql = """
                insert into run_segment_choices (
                    run_id, segment_index, choice_slot, offered_card_id, was_selected, offered_at, selected_at
                )
                values (
                    @run_id, @segment_index, @choice_slot, @offered_card_id, @was_selected, now(),
                    case when @was_selected then now() else null end
                );
                """;

            await using NpgsqlCommand choiceCommand = new(choiceSql, connection, transaction);
            choiceCommand.Parameters.AddWithValue("run_id", runId);
            choiceCommand.Parameters.AddWithValue("segment_index", segment.SegmentIndex);
            choiceCommand.Parameters.AddWithValue("choice_slot", i + 1);
            choiceCommand.Parameters.AddWithValue("offered_card_id", segment.OfferedCardIds[i]);
            choiceCommand.Parameters.AddWithValue("was_selected", string.Equals(segment.OfferedCardIds[i], segment.SelectedCardId, StringComparison.OrdinalIgnoreCase));
            await choiceCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        const string updateRunSql = """
            update run_sessions
            set current_segment_index = greatest(current_segment_index, @segment_index), updated_at = now()
            where run_id = @run_id;
            """;

        await using NpgsqlCommand updateRunCommand = new(updateRunSql, connection, transaction);
        updateRunCommand.Parameters.AddWithValue("run_id", runId);
        updateRunCommand.Parameters.AddWithValue("segment_index", segment.SegmentIndex);
        await updateRunCommand.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return segment;
    }

    public async Task<RunSessionDto?> FinishRunAsync(long runId, FinishRunRequest request, CancellationToken cancellationToken)
    {
        string normalizedStatus = NormalizeFinishStatus(request.Status);

        await using NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string lockSql = "select player_id from run_sessions where run_id = @run_id for update;";
        await using NpgsqlCommand lockCommand = new(lockSql, connection, transaction);
        lockCommand.Parameters.AddWithValue("run_id", runId);
        object? playerIdResult = await lockCommand.ExecuteScalarAsync(cancellationToken);
        if (playerIdResult is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        string playerId = playerIdResult.ToString() ?? string.Empty;

        const string sql = """
            update run_sessions
            set
                status = @status,
                segments_cleared = @segments_cleared,
                current_segment_index = @current_segment_index,
                hero_current_health = @hero_current_health,
                cards_unlocked_this_run = @cards_unlocked_this_run,
                gold_earned = @gold_earned,
                summary = cast(@summary_json as jsonb),
                ended_at = coalesce(ended_at, now()),
                updated_at = now()
            where run_id = @run_id
            returning
                run_id, player_id, status, starting_card_id, current_segment_index,
                segments_cleared, hero_max_health, hero_current_health, hero_attack,
                cards_unlocked_this_run, gold_earned, summary::text,
                started_at, ended_at, updated_at;
            """;

        await using NpgsqlCommand command = new(sql, connection, transaction);
        command.Parameters.AddWithValue("run_id", runId);
        command.Parameters.AddWithValue("status", normalizedStatus);
        command.Parameters.AddWithValue("segments_cleared", Math.Max(0, request.SegmentsCleared));
        command.Parameters.AddWithValue("current_segment_index", Math.Max(1, request.CurrentSegmentIndex));
        command.Parameters.AddWithValue("hero_current_health", Math.Max(0, request.HeroCurrentHealth));
        command.Parameters.AddWithValue("cards_unlocked_this_run", Math.Max(0, request.CardsUnlockedThisRun));
        command.Parameters.AddWithValue("gold_earned", Math.Max(0, request.GoldEarned));
        command.Parameters.AddWithValue("summary_json", NormalizeJsonObject(request.SummaryJson));

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        RunSessionDto run = MapRunSession(reader);
        await reader.CloseAsync();

        const string progressSql = """
            insert into player_progress (
                player_id, unlocked_card_ids, unlocked_relic_ids, unlocked_modifier_ids,
                completed_runs, failed_runs, total_runs_started, total_cards_unlocked,
                highest_segment_reached, total_enemies_defeated, total_damage_dealt,
                total_damage_taken, soft_currency, progression_flags, updated_at
            )
            values (
                @player_id, '[]'::jsonb, '[]'::jsonb, '[]'::jsonb,
                case when @status = 'completed' then 1 else 0 end,
                case when @status = 'failed' then 1 else 0 end,
                0, @cards_unlocked_this_run, @highest_segment_reached, 0, 0, 0,
                @gold_earned, '{}'::jsonb, now()
            )
            on conflict (player_id) do update
            set
                completed_runs = player_progress.completed_runs + case when @status = 'completed' then 1 else 0 end,
                failed_runs = player_progress.failed_runs + case when @status = 'failed' then 1 else 0 end,
                total_cards_unlocked = player_progress.total_cards_unlocked + @cards_unlocked_this_run,
                highest_segment_reached = greatest(player_progress.highest_segment_reached, @highest_segment_reached),
                soft_currency = player_progress.soft_currency + @gold_earned,
                updated_at = now();
            """;

        await using NpgsqlCommand progressCommand = new(progressSql, connection, transaction);
        progressCommand.Parameters.AddWithValue("player_id", playerId);
        progressCommand.Parameters.AddWithValue("status", normalizedStatus);
        progressCommand.Parameters.AddWithValue("cards_unlocked_this_run", Math.Max(0, request.CardsUnlockedThisRun));
        progressCommand.Parameters.AddWithValue("highest_segment_reached", Math.Max(0, request.SegmentsCleared));
        progressCommand.Parameters.AddWithValue("gold_earned", Math.Max(0, request.GoldEarned));
        await progressCommand.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return run;
    }

    private static RunSessionDto MapRunSession(NpgsqlDataReader reader)
    {
        return new RunSessionDto(
            reader.GetInt64(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.GetInt32(4),
            reader.GetInt32(5),
            reader.GetInt32(6),
            reader.GetInt32(7),
            reader.GetInt32(8),
            reader.GetInt32(9),
            reader.GetInt32(10),
            reader.GetString(11),
            reader.GetFieldValue<DateTimeOffset>(12),
            reader.IsDBNull(13) ? null : reader.GetFieldValue<DateTimeOffset>(13),
            reader.GetFieldValue<DateTimeOffset>(14));
    }

    private static RunSegmentDto MapRunSegment(NpgsqlDataReader reader, IReadOnlyList<string> offeredCardIds, string? selectedCardId)
    {
        return new RunSegmentDto(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetInt32(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetInt32(5),
            reader.GetInt32(6),
            reader.GetInt32(7),
            reader.GetInt32(8),
            reader.GetFloat(9),
            reader.GetFloat(10),
            reader.IsDBNull(11) ? null : reader.GetString(11),
            reader.GetString(12),
            reader.IsDBNull(13) ? null : reader.GetInt32(13),
            reader.IsDBNull(14) ? null : reader.GetInt32(14),
            offeredCardIds,
            selectedCardId,
            reader.GetFieldValue<DateTimeOffset>(15),
            reader.IsDBNull(16) ? null : reader.GetFieldValue<DateTimeOffset>(16));
    }

    private static IReadOnlyList<string> NormalizeStringList(IEnumerable<string>? values)
    {
        List<string> normalized = new();
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

        foreach (string? value in values ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;

            string item = value.Trim().ToLowerInvariant();
            if (!seen.Add(item))
                continue;

            normalized.Add(item);
        }

        return normalized;
    }

    private static string NormalizePlayerId(string playerId) => playerId.Trim().ToLowerInvariant();

    private static object NormalizeNullableId(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim().ToLowerInvariant();
    }

    private static string NormalizeRequiredId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }

    private static string NormalizeSegmentState(string state)
    {
        string normalized = string.IsNullOrWhiteSpace(state) ? string.Empty : state.Trim().ToLowerInvariant();
        return normalized is "entered" or "cleared" or "failed" ? normalized : "generated";
    }

    private static string NormalizeFinishStatus(string status)
    {
        string normalized = string.IsNullOrWhiteSpace(status) ? string.Empty : status.Trim().ToLowerInvariant();
        return normalized is "completed" or "failed" or "abandoned" ? normalized : "completed";
    }

    private static string NormalizeJsonObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return "{}";

        try
        {
            JsonElement element = JsonSerializer.Deserialize<JsonElement>(json);
            return element.ValueKind == JsonValueKind.Object ? element.GetRawText() : "{}";
        }
        catch (JsonException)
        {
            return "{}";
        }
    }

    private static async Task EnsurePlayerExistsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string playerId, CancellationToken cancellationToken)
    {
        const string sql = """
            insert into players (player_id, last_seen_at, profile_data)
            values (@player_id, now(), '{}'::jsonb)
            on conflict (player_id) do update
            set last_seen_at = now();
            """;

        await using NpgsqlCommand command = new(sql, connection, transaction);
        command.Parameters.AddWithValue("player_id", playerId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task AbandonActiveRunsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string playerId, CancellationToken cancellationToken)
    {
        const string sql = """
            update run_sessions
            set status = 'abandoned', ended_at = coalesce(ended_at, now()), updated_at = now()
            where player_id = @player_id and status = 'active';
            """;

        await using NpgsqlCommand command = new(sql, connection, transaction);
        command.Parameters.AddWithValue("player_id", playerId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task IncrementRunsStartedAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string playerId, CancellationToken cancellationToken)
    {
        const string sql = """
            insert into player_progress (
                player_id, unlocked_card_ids, unlocked_relic_ids, unlocked_modifier_ids,
                completed_runs, failed_runs, total_runs_started, total_cards_unlocked,
                highest_segment_reached, total_enemies_defeated, total_damage_dealt,
                total_damage_taken, soft_currency, progression_flags, updated_at
            )
            values (
                @player_id, '[]'::jsonb, '[]'::jsonb, '[]'::jsonb,
                0, 0, 1, 0, 0, 0, 0, 0, 0, '{}'::jsonb, now()
            )
            on conflict (player_id) do update
            set total_runs_started = player_progress.total_runs_started + 1, updated_at = now();
            """;

        await using NpgsqlCommand command = new(sql, connection, transaction);
        command.Parameters.AddWithValue("player_id", playerId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
