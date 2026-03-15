-- Full game schema for ArchitectusFati
-- Compatible with the existing lightweight backend tables while expanding to a fuller progression/run model.

begin;

create table if not exists biomes (
    biome_id text primary key,
    display_name text not null,
    description text not null default '',
    floor_color_hex text not null,
    wall_color_hex text not null,
    ambient_config jsonb not null default '{}'::jsonb,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists enemy_archetypes (
    enemy_id text primary key,
    display_name text not null,
    description text not null default '',
    max_health integer not null check (max_health > 0),
    attack integer not null check (attack >= 0),
    movement_pattern text not null default 'chase',
    rarity text not null default 'common',
    sprite_key text,
    behavior_config jsonb not null default '{}'::jsonb,
    reward_config jsonb not null default '{}'::jsonb,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists world_modifier_definitions (
    modifier_id text primary key,
    display_name text not null,
    description text not null default '',
    modifier_type text not null,
    rarity text not null default 'common',
    stack_mode text not null default 'refresh',
    effect_config jsonb not null default '{}'::jsonb,
    is_positive boolean not null default false,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists relic_definitions (
    relic_id text primary key,
    display_name text not null,
    description text not null default '',
    rarity text not null default 'common',
    effect_config jsonb not null default '{}'::jsonb,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists consumable_definitions (
    consumable_id text primary key,
    display_name text not null,
    description text not null default '',
    max_stack integer not null default 1 check (max_stack > 0),
    effect_config jsonb not null default '{}'::jsonb,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists cards (
    card_id text primary key,
    display_name text not null,
    description text not null default '',
    starts_unlocked boolean not null default false,
    biome_id text not null references biomes(biome_id),
    floor_color_hex text not null,
    wall_color_hex text not null,
    segment_width integer not null check (segment_width >= 5),
    segment_height integer not null check (segment_height >= 5),
    entry_x integer not null,
    exit_x integer not null,
    obstacle_chance real not null check (obstacle_chance >= 0 and obstacle_chance <= 1),
    enemy_chance real not null check (enemy_chance >= 0 and enemy_chance <= 1),
    enemy_ids jsonb not null default '[]'::jsonb,
    base_difficulty integer not null default 1 check (base_difficulty >= 1),
    card_type text not null default 'path',
    reward_tier integer not null default 1 check (reward_tier >= 1),
    generation_tags jsonb not null default '[]'::jsonb,
    sort_order integer not null default 0,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists card_enemy_pool (
    card_id text not null references cards(card_id) on delete cascade,
    enemy_id text not null references enemy_archetypes(enemy_id),
    weight integer not null default 1 check (weight > 0),
    min_count integer not null default 0 check (min_count >= 0),
    max_count integer not null default 3 check (max_count >= min_count),
    primary key (card_id, enemy_id)
);

create table if not exists card_modifier_pool (
    card_id text not null references cards(card_id) on delete cascade,
    modifier_id text not null references world_modifier_definitions(modifier_id),
    weight integer not null default 1 check (weight > 0),
    guaranteed boolean not null default false,
    primary key (card_id, modifier_id)
);

create table if not exists card_reward_pool (
    card_id text not null references cards(card_id) on delete cascade,
    reward_type text not null check (reward_type in ('relic', 'consumable', 'card_unlock', 'gold', 'heal')),
    reward_id text not null,
    weight integer not null default 1 check (weight > 0),
    quantity integer not null default 1 check (quantity > 0),
    primary key (card_id, reward_type, reward_id)
);

create table if not exists players (
    player_id text primary key,
    display_name text,
    preferred_language text not null default 'ca',
    created_at timestamptz not null default now(),
    last_seen_at timestamptz not null default now(),
    profile_data jsonb not null default '{}'::jsonb
);

create table if not exists player_progress (
    player_id text primary key,
    unlocked_card_ids jsonb not null default '[]'::jsonb,
    unlocked_relic_ids jsonb not null default '[]'::jsonb,
    unlocked_modifier_ids jsonb not null default '[]'::jsonb,
    completed_runs integer not null default 0,
    failed_runs integer not null default 0,
    total_runs_started integer not null default 0,
    total_cards_unlocked integer not null default 0,
    highest_segment_reached integer not null default 0,
    total_enemies_defeated integer not null default 0,
    total_damage_dealt integer not null default 0,
    total_damage_taken integer not null default 0,
    soft_currency integer not null default 0,
    progression_flags jsonb not null default '{}'::jsonb,
    updated_at timestamptz not null default now()
);

create table if not exists player_card_unlocks (
    player_id text not null,
    card_id text not null references cards(card_id),
    unlocked_at timestamptz not null default now(),
    unlock_source text not null default 'default',
    primary key (player_id, card_id)
);

create table if not exists player_relics (
    player_id text not null,
    relic_id text not null references relic_definitions(relic_id),
    quantity integer not null default 1 check (quantity > 0),
    first_obtained_at timestamptz not null default now(),
    last_obtained_at timestamptz not null default now(),
    primary key (player_id, relic_id)
);

create table if not exists player_consumables (
    player_id text not null,
    consumable_id text not null references consumable_definitions(consumable_id),
    quantity integer not null default 0 check (quantity >= 0),
    updated_at timestamptz not null default now(),
    primary key (player_id, consumable_id)
);

create table if not exists run_sessions (
    run_id bigint generated always as identity primary key,
    player_id text not null,
    run_seed text,
    status text not null check (status in ('active', 'completed', 'failed', 'abandoned')),
    starting_card_id text references cards(card_id),
    current_segment_index integer not null default 1 check (current_segment_index >= 1),
    segments_cleared integer not null default 0 check (segments_cleared >= 0),
    hero_max_health integer not null default 10 check (hero_max_health > 0),
    hero_current_health integer not null default 10 check (hero_current_health >= 0),
    hero_attack integer not null default 3 check (hero_attack >= 0),
    cards_unlocked_this_run integer not null default 0 check (cards_unlocked_this_run >= 0),
    gold_earned integer not null default 0 check (gold_earned >= 0),
    summary jsonb not null default '{}'::jsonb,
    started_at timestamptz not null default now(),
    ended_at timestamptz,
    updated_at timestamptz not null default now()
);

create table if not exists run_deck_state (
    run_id bigint not null references run_sessions(run_id) on delete cascade,
    card_id text not null references cards(card_id),
    is_unlocked boolean not null default false,
    unlocked_at_segment integer,
    primary key (run_id, card_id)
);

create table if not exists run_segments (
    run_segment_id bigint generated always as identity primary key,
    run_id bigint not null references run_sessions(run_id) on delete cascade,
    segment_index integer not null check (segment_index >= 1),
    card_id text not null references cards(card_id),
    biome_id text not null references biomes(biome_id),
    segment_width integer not null check (segment_width >= 5),
    segment_height integer not null check (segment_height >= 5),
    entry_x integer not null,
    exit_x integer not null,
    obstacle_chance real not null check (obstacle_chance >= 0 and obstacle_chance <= 1),
    enemy_chance real not null check (enemy_chance >= 0 and enemy_chance <= 1),
    generated_seed text,
    state text not null default 'generated' check (state in ('generated', 'entered', 'cleared', 'failed')),
    hero_health_on_enter integer,
    hero_health_on_exit integer,
    created_at timestamptz not null default now(),
    cleared_at timestamptz,
    unique (run_id, segment_index)
);

create table if not exists run_segment_choices (
    run_id bigint not null references run_sessions(run_id) on delete cascade,
    segment_index integer not null check (segment_index >= 1),
    choice_slot integer not null check (choice_slot >= 1),
    offered_card_id text not null references cards(card_id),
    was_selected boolean not null default false,
    offered_at timestamptz not null default now(),
    selected_at timestamptz,
    primary key (run_id, segment_index, choice_slot)
);

create table if not exists run_segment_modifiers (
    run_segment_id bigint not null references run_segments(run_segment_id) on delete cascade,
    modifier_id text not null references world_modifier_definitions(modifier_id),
    stack_count integer not null default 1 check (stack_count > 0),
    applied_config jsonb not null default '{}'::jsonb,
    primary key (run_segment_id, modifier_id)
);

create table if not exists run_segment_enemies (
    run_segment_enemy_id bigint generated always as identity primary key,
    run_segment_id bigint not null references run_segments(run_segment_id) on delete cascade,
    enemy_id text not null references enemy_archetypes(enemy_id),
    spawn_x integer not null,
    spawn_y integer not null,
    spawned_max_health integer not null check (spawned_max_health > 0),
    spawned_attack integer not null check (spawned_attack >= 0),
    defeated boolean not null default false,
    defeated_at timestamptz,
    metadata jsonb not null default '{}'::jsonb
);

create table if not exists run_rewards (
    run_reward_id bigint generated always as identity primary key,
    run_id bigint not null references run_sessions(run_id) on delete cascade,
    run_segment_id bigint references run_segments(run_segment_id) on delete set null,
    reward_type text not null check (reward_type in ('relic', 'consumable', 'card_unlock', 'gold', 'heal')),
    reward_id text,
    quantity integer not null default 1 check (quantity > 0),
    granted_at timestamptz not null default now(),
    metadata jsonb not null default '{}'::jsonb
);

create table if not exists run_events (
    run_event_id bigint generated always as identity primary key,
    run_id bigint not null references run_sessions(run_id) on delete cascade,
    run_segment_id bigint references run_segments(run_segment_id) on delete set null,
    event_type text not null,
    event_order integer not null default 0,
    payload jsonb not null default '{}'::jsonb,
    created_at timestamptz not null default now()
);

create index if not exists idx_cards_biome_id on cards (biome_id);
create index if not exists idx_cards_active_sort on cards (is_active, sort_order, display_name);
create index if not exists idx_run_sessions_player_status on run_sessions (player_id, status, started_at desc);
create index if not exists idx_run_segments_run on run_segments (run_id, segment_index);
create index if not exists idx_run_events_run_order on run_events (run_id, event_order, created_at);
create index if not exists idx_player_card_unlocks_player on player_card_unlocks (player_id, unlocked_at desc);
create index if not exists idx_player_relics_player on player_relics (player_id);
create index if not exists idx_player_consumables_player on player_consumables (player_id);

insert into biomes (biome_id, display_name, description, floor_color_hex, wall_color_hex, ambient_config)
values
    ('forest', 'Bosc', 'Boscos oberts amb vegetacio viva i camins amples.', '#7EAD66', '#385C36', '{"music":"forest_theme","fog":0.05}'::jsonb),
    ('ruins', 'Ruines', 'Muralles caigudes, emboscades i passadissos irregulars.', '#938E85', '#443E3B', '{"music":"ruins_theme","fog":0.08}'::jsonb),
    ('swamp', 'Aiguamoll', 'Terreny dens, humit i de visibilitat irregular.', '#628371', '#274237', '{"music":"swamp_theme","fog":0.12}'::jsonb),
    ('desert', 'Desert', 'Mapes oberts amb menys cobertures i enemics dispersos.', '#D7B870', '#855C31', '{"music":"desert_theme","fog":0.02}'::jsonb),
    ('citadel', 'Ciutadella', 'Segments exigents amb rutes estretes i defenses fortes.', '#C7CBCF', '#4F5462', '{"music":"citadel_theme","fog":0.04}'::jsonb),
    ('cavern', 'Caverna', 'Coves calentes amb pressio constant i combats curts.', '#AF6746', '#572A1F', '{"music":"cavern_theme","fog":0.09}'::jsonb)
on conflict (biome_id) do update
set
    display_name = excluded.display_name,
    description = excluded.description,
    floor_color_hex = excluded.floor_color_hex,
    wall_color_hex = excluded.wall_color_hex,
    ambient_config = excluded.ambient_config,
    is_active = true,
    updated_at = now();

insert into enemy_archetypes (enemy_id, display_name, description, max_health, attack, movement_pattern, rarity, sprite_key, behavior_config, reward_config)
values
    ('grunt', 'Rastrejador', 'Enemic base que persegueix l''heroi de manera directa.', 5, 2, 'chase', 'common', 'enemy_grunt', '{"aggression":1.0}'::jsonb, '{"xp":1}'::jsonb),
    ('brute', 'Colossus', 'Molt resistent, lent i amb cops mes durs.', 10, 4, 'slow_chase', 'uncommon', 'enemy_brute', '{"aggression":0.8}'::jsonb, '{"xp":3}'::jsonb),
    ('stalker', 'Acechador', 'Fragil pero perillos si et talla el pas.', 4, 3, 'ambush', 'uncommon', 'enemy_stalker', '{"aggression":1.3}'::jsonb, '{"xp":2}'::jsonb),
    ('sentinel', 'Sentinella', 'Controla passadissos i defensa sortides.', 8, 2, 'guard', 'rare', 'enemy_sentinel', '{"aggression":0.6}'::jsonb, '{"xp":4}'::jsonb),
    ('skirmisher', 'Escaramussador', 'Es mou rapid i desgasta l''heroi.', 6, 2, 'flank', 'common', 'enemy_skirmisher', '{"aggression":1.1}'::jsonb, '{"xp":2}'::jsonb)
on conflict (enemy_id) do update
set
    display_name = excluded.display_name,
    description = excluded.description,
    max_health = excluded.max_health,
    attack = excluded.attack,
    movement_pattern = excluded.movement_pattern,
    rarity = excluded.rarity,
    sprite_key = excluded.sprite_key,
    behavior_config = excluded.behavior_config,
    reward_config = excluded.reward_config,
    is_active = true,
    updated_at = now();

insert into world_modifier_definitions (modifier_id, display_name, description, modifier_type, rarity, stack_mode, effect_config, is_positive)
values
    ('narrow_passages', 'Passadissos Estrets', 'Redueix espai util i augmenta el risc de bloqueig.', 'terrain', 'common', 'refresh', '{"extraObstacleChance":0.06}'::jsonb, false),
    ('blood_mist', 'Boira Sagnant', 'Els enemics fan una mica mes de mal.', 'combat', 'uncommon', 'stack', '{"enemyAttackBonus":1}'::jsonb, false),
    ('healing_springs', 'Fonts Curatives', 'En entrar al segment recuperes vida.', 'blessing', 'rare', 'refresh', '{"healOnEnter":2}'::jsonb, true),
    ('fragile_walls', 'Murs Fragils', 'Hi ha menys obstacles, pero enemics mes agressius.', 'terrain', 'uncommon', 'refresh', '{"extraObstacleChance":-0.05,"enemyChanceBonus":0.04}'::jsonb, true)
on conflict (modifier_id) do update
set
    display_name = excluded.display_name,
    description = excluded.description,
    modifier_type = excluded.modifier_type,
    rarity = excluded.rarity,
    stack_mode = excluded.stack_mode,
    effect_config = excluded.effect_config,
    is_positive = excluded.is_positive,
    is_active = true,
    updated_at = now();

insert into relic_definitions (relic_id, display_name, description, rarity, effect_config)
values
    ('iron_talisman', 'Talisma de Ferro', 'Augmenta la vida maxima de l''heroi.', 'common', '{"heroMaxHealthBonus":2}'::jsonb),
    ('ember_core', 'Nucli de Brasa', 'Augmenta l''atac base.', 'uncommon', '{"heroAttackBonus":1}'::jsonb),
    ('cartographer_seal', 'Segell del Cartograf', 'Ofereix una carta extra en cada eleccio.', 'rare', '{"extraCardChoice":1}'::jsonb)
on conflict (relic_id) do update
set
    display_name = excluded.display_name,
    description = excluded.description,
    rarity = excluded.rarity,
    effect_config = excluded.effect_config,
    is_active = true,
    updated_at = now();

insert into consumable_definitions (consumable_id, display_name, description, max_stack, effect_config)
values
    ('healing_vial', 'Vial Curatiu', 'Recupera vida durant o entre segments.', 10, '{"heal":3}'::jsonb),
    ('smoke_bomb', 'Bomba de Fum', 'Evita un combat o redueix enemics d''un segment.', 5, '{"skipEncounter":true}'::jsonb)
on conflict (consumable_id) do update
set
    display_name = excluded.display_name,
    description = excluded.description,
    max_stack = excluded.max_stack,
    effect_config = excluded.effect_config,
    is_active = true,
    updated_at = now();

insert into cards (
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
    enemy_ids,
    base_difficulty,
    card_type,
    reward_tier,
    generation_tags,
    sort_order
)
values
    ('verdant_path', 'Sender Verdant', 'Boscos oberts amb pocs obstacles i amenaces moderades.', true, 'forest', '#7EAD66', '#385C36', 16, 10, 1, 14, 0.10, 0.12, '["grunt","skirmisher"]'::jsonb, 1, 'path', 1, '["starter","balanced"]'::jsonb, 10),
    ('ashen_corridor', 'Corredor Cendros', 'Ruines estretes amb molts murs i emboscades frequents.', true, 'ruins', '#938E85', '#443E3B', 17, 10, 1, 15, 0.18, 0.18, '["grunt","stalker"]'::jsonb, 2, 'path', 1, '["starter","tight"]'::jsonb, 20),
    ('moonlit_bog', 'Aiguamoll Lunar', 'Bioma dens amb camins irregulars i enemics puntuals.', true, 'swamp', '#628371', '#274237', 16, 11, 1, 14, 0.16, 0.14, '["grunt","stalker"]'::jsonb, 2, 'path', 1, '["starter","control"]'::jsonb, 30),
    ('saffron_dunes', 'Dunes Safra', 'Zona mes oberta on el perill arriba en onades disperses.', false, 'desert', '#D7B870', '#855C31', 18, 10, 1, 16, 0.08, 0.20, '["skirmisher","brute"]'::jsonb, 3, 'path', 2, '["open","aggressive"]'::jsonb, 40),
    ('ivory_citadel', 'Ciutadella d''Ivori', 'Segment llarg i perillos on l''heroi ha de forcar rutes alternatives.', false, 'citadel', '#C7CBCF', '#4F5462', 19, 11, 1, 17, 0.20, 0.16, '["sentinel","brute"]'::jsonb, 4, 'elite', 2, '["elite","fortified"]'::jsonb, 50),
    ('ember_depths', 'Fondaries Brasa', 'Coves calentes amb passadissos trencats i pressio constant.', false, 'cavern', '#AF6746', '#572A1F', 17, 11, 1, 15, 0.22, 0.22, '["grunt","brute","stalker"]'::jsonb, 4, 'elite', 3, '["elite","pressure"]'::jsonb, 60)
on conflict (card_id) do update
set
    display_name = excluded.display_name,
    description = excluded.description,
    starts_unlocked = excluded.starts_unlocked,
    biome_id = excluded.biome_id,
    floor_color_hex = excluded.floor_color_hex,
    wall_color_hex = excluded.wall_color_hex,
    segment_width = excluded.segment_width,
    segment_height = excluded.segment_height,
    entry_x = excluded.entry_x,
    exit_x = excluded.exit_x,
    obstacle_chance = excluded.obstacle_chance,
    enemy_chance = excluded.enemy_chance,
    enemy_ids = excluded.enemy_ids,
    base_difficulty = excluded.base_difficulty,
    card_type = excluded.card_type,
    reward_tier = excluded.reward_tier,
    generation_tags = excluded.generation_tags,
    sort_order = excluded.sort_order,
    is_active = true,
    updated_at = now();

insert into card_enemy_pool (card_id, enemy_id, weight, min_count, max_count)
values
    ('verdant_path', 'grunt', 6, 1, 3),
    ('verdant_path', 'skirmisher', 2, 0, 2),
    ('ashen_corridor', 'grunt', 4, 1, 3),
    ('ashen_corridor', 'stalker', 4, 1, 3),
    ('moonlit_bog', 'grunt', 4, 1, 2),
    ('moonlit_bog', 'stalker', 3, 0, 2),
    ('saffron_dunes', 'skirmisher', 5, 1, 3),
    ('saffron_dunes', 'brute', 2, 0, 1),
    ('ivory_citadel', 'sentinel', 5, 1, 2),
    ('ivory_citadel', 'brute', 3, 0, 2),
    ('ember_depths', 'grunt', 3, 1, 2),
    ('ember_depths', 'brute', 3, 0, 2),
    ('ember_depths', 'stalker', 4, 1, 3)
on conflict (card_id, enemy_id) do update
set
    weight = excluded.weight,
    min_count = excluded.min_count,
    max_count = excluded.max_count;

insert into card_modifier_pool (card_id, modifier_id, weight, guaranteed)
values
    ('verdant_path', 'healing_springs', 1, false),
    ('ashen_corridor', 'narrow_passages', 4, true),
    ('moonlit_bog', 'blood_mist', 2, false),
    ('saffron_dunes', 'fragile_walls', 3, false),
    ('ivory_citadel', 'narrow_passages', 2, true),
    ('ember_depths', 'blood_mist', 3, true)
on conflict (card_id, modifier_id) do update
set
    weight = excluded.weight,
    guaranteed = excluded.guaranteed;

insert into card_reward_pool (card_id, reward_type, reward_id, weight, quantity)
values
    ('verdant_path', 'heal', 'heal_small', 2, 2),
    ('ashen_corridor', 'consumable', 'smoke_bomb', 1, 1),
    ('moonlit_bog', 'consumable', 'healing_vial', 2, 1),
    ('saffron_dunes', 'relic', 'iron_talisman', 1, 1),
    ('ivory_citadel', 'relic', 'ember_core', 1, 1),
    ('ember_depths', 'relic', 'cartographer_seal', 1, 1)
on conflict (card_id, reward_type, reward_id) do update
set
    weight = excluded.weight,
    quantity = excluded.quantity;

commit;
