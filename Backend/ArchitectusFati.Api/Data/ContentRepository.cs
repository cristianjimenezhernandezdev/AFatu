using ArchitectusFati.Api.Contracts;
using Npgsql;

namespace ArchitectusFati.Api.Data;

public sealed class ContentRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public ContentRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<BootstrapContentResponse> GetBootstrapContentAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<BiomeDefinition> biomes = await GetActiveBiomesAsync(cancellationToken);
        IReadOnlyList<EnemyArchetypeDefinition> enemies = await GetActiveEnemiesAsync(cancellationToken);
        IReadOnlyList<WorldModifierDefinition> modifiers = await GetActiveModifiersAsync(cancellationToken);
        IReadOnlyList<RelicDefinition> relics = await GetActiveRelicsAsync(cancellationToken);
        IReadOnlyList<ConsumableDefinition> consumables = await GetActiveConsumablesAsync(cancellationToken);
        IReadOnlyList<CardDefinition> cards = await GetActiveCardsAsync(cancellationToken);

        return new BootstrapContentResponse(biomes, enemies, modifiers, relics, consumables, cards);
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

        List<CardDefinition> items = new();
        await using NpgsqlCommand command = _dataSource.CreateCommand(sql);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new CardDefinition(
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

        return items;
    }

    private async Task<IReadOnlyList<BiomeDefinition>> GetActiveBiomesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            select biome_id, display_name, description, floor_color_hex, wall_color_hex, ambient_config::text
            from biomes
            where is_active = true
            order by display_name asc;
            """;

        List<BiomeDefinition> items = new();
        await using NpgsqlCommand command = _dataSource.CreateCommand(sql);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new BiomeDefinition(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5)));
        }

        return items;
    }

    private async Task<IReadOnlyList<EnemyArchetypeDefinition>> GetActiveEnemiesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            select enemy_id, display_name, description, max_health, attack, movement_pattern, rarity, sprite_key, behavior_config::text, reward_config::text
            from enemy_archetypes
            where is_active = true
            order by rarity asc, display_name asc;
            """;

        List<EnemyArchetypeDefinition> items = new();
        await using NpgsqlCommand command = _dataSource.CreateCommand(sql);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new EnemyArchetypeDefinition(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3),
                reader.GetInt32(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.GetString(8),
                reader.GetString(9)));
        }

        return items;
    }

    private async Task<IReadOnlyList<WorldModifierDefinition>> GetActiveModifiersAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            select modifier_id, display_name, description, modifier_type, rarity, stack_mode, effect_config::text, is_positive
            from world_modifier_definitions
            where is_active = true
            order by rarity asc, display_name asc;
            """;

        List<WorldModifierDefinition> items = new();
        await using NpgsqlCommand command = _dataSource.CreateCommand(sql);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new WorldModifierDefinition(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetBoolean(7)));
        }

        return items;
    }

    private async Task<IReadOnlyList<RelicDefinition>> GetActiveRelicsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            select relic_id, display_name, description, rarity, effect_config::text
            from relic_definitions
            where is_active = true
            order by rarity asc, display_name asc;
            """;

        List<RelicDefinition> items = new();
        await using NpgsqlCommand command = _dataSource.CreateCommand(sql);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new RelicDefinition(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4)));
        }

        return items;
    }

    private async Task<IReadOnlyList<ConsumableDefinition>> GetActiveConsumablesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            select consumable_id, display_name, description, max_stack, effect_config::text
            from consumable_definitions
            where is_active = true
            order by display_name asc;
            """;

        List<ConsumableDefinition> items = new();
        await using NpgsqlCommand command = _dataSource.CreateCommand(sql);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new ConsumableDefinition(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3),
                reader.GetString(4)));
        }

        return items;
    }

    private static IReadOnlyList<string> ParseStringList(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<string>();

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>();
        }
        catch (System.Text.Json.JsonException)
        {
            return Array.Empty<string>();
        }
    }
}
