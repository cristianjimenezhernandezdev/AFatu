begin;

-- ---------------------------------------------------------------------------
-- Reset complet del model de dades
-- ---------------------------------------------------------------------------

drop table if exists run_shop_offers cascade;
drop table if exists run_events cascade;
drop table if exists run_rewards cascade;
drop table if exists run_segment_chests cascade;
drop table if exists run_segment_enemies cascade;
drop table if exists run_segment_modifiers cascade;
drop table if exists run_segment_choices cascade;
drop table if exists run_segments cascade;
drop table if exists run_deck_state cascade;
drop table if exists run_equipped_divine_powers cascade;
drop table if exists run_sessions cascade;

drop table if exists player_consumables cascade;
drop table if exists player_relics cascade;
drop table if exists player_divine_power_unlocks cascade;
drop table if exists player_card_unlocks cascade;
drop table if exists player_progress cascade;
drop table if exists players cascade;

drop table if exists chest_reward_pool cascade;
drop table if exists run_result_definitions cascade;
drop table if exists card_reward_pool cascade;
drop table if exists card_modifier_pool cascade;
drop table if exists card_enemy_pool cascade;
drop table if exists cards cascade;
drop table if exists shop_offer_definitions cascade;
drop table if exists divine_power_definitions cascade;
drop table if exists consumable_definitions cascade;
drop table if exists relic_definitions cascade;
drop table if exists world_modifier_definitions cascade;
drop table if exists enemy_archetypes cascade;
drop table if exists biomes cascade;

-- ---------------------------------------------------------------------------
-- Contingut mestre
-- ---------------------------------------------------------------------------

create table biomes (
    biome_id text primary key,
    display_name text not null,
    description text not null default '',
    floor_color_hex text not null default '#000000' check (floor_color_hex ~ '^#[0-9A-Fa-f]{6}$'),
    wall_color_hex text not null default '#000000' check (wall_color_hex ~ '^#[0-9A-Fa-f]{6}$'),
    ambient_config jsonb not null default '{}'::jsonb,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table enemy_archetypes (
    enemy_id text primary key,
    display_name text not null,
    description text not null default '',
    max_health integer not null check (max_health > 0),
    attack integer not null check (attack >= 0),
    defense integer not null default 0 check (defense >= 0),
    speed real not null default 1.0 check (speed > 0),
    movement_pattern text not null default 'chase',
    rarity text not null default 'common' check (rarity in ('common', 'uncommon', 'rare', 'elite', 'boss')),
    sprite_key text not null default '',
    behavior_config jsonb not null default '{}'::jsonb,
    reward_config jsonb not null default '{}'::jsonb,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table world_modifier_definitions (
    modifier_id text primary key,
    display_name text not null,
    description text not null default '',
    art_key text not null default '',
    modifier_type text not null check (modifier_type in ('terrain', 'blessing', 'combat', 'encounter', 'economy')),
    rarity text not null default 'common' check (rarity in ('common', 'uncommon', 'rare', 'elite')),
    stack_mode text not null default 'refresh' check (stack_mode in ('refresh', 'stack', 'override')),
    effect_config jsonb not null default '{}'::jsonb,
    is_positive boolean not null default false,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table relic_definitions (
    relic_id text primary key,
    display_name text not null,
    description text not null default '',
    art_key text not null default '',
    rarity text not null default 'common' check (rarity in ('common', 'uncommon', 'rare', 'legendary')),
    effect_config jsonb not null default '{}'::jsonb,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table consumable_definitions (
    consumable_id text primary key,
    display_name text not null,
    description text not null default '',
    art_key text not null default '',
    max_stack integer not null default 1 check (max_stack > 0),
    effect_config jsonb not null default '{}'::jsonb,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table divine_power_definitions (
    power_id text primary key,
    display_name text not null,
    description text not null default '',
    art_key text not null default '',
    power_type text not null check (power_type in ('buff', 'behavior', 'summon', 'defense')),
    cooldown_seconds integer not null default 30 check (cooldown_seconds >= 0),
    duration_seconds integer not null default 0 check (duration_seconds >= 0),
    max_charges integer not null default 2 check (max_charges > 0),
    unlock_cost integer not null default 0 check (unlock_cost >= 0),
    starts_unlocked boolean not null default false,
    sort_order integer not null default 0,
    effect_config jsonb not null default '{}'::jsonb,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table shop_offer_definitions (
    offer_id text primary key,
    display_name text not null,
    description text not null default '',
    art_key text not null default '',
    offer_type text not null check (offer_type in ('heal', 'buff', 'utility', 'summon', 'equipment')),
    cost_gold integer not null check (cost_gold >= 0),
    reward_type text not null default '',
    reward_id text not null default '',
    reward_quantity integer not null default 1 check (reward_quantity > 0),
    duration_segments integer not null default 0 check (duration_segments >= 0),
    effect_config jsonb not null default '{}'::jsonb,
    sort_order integer not null default 0,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table run_result_definitions (
    result_id text primary key,
    display_name text not null,
    description text not null default '',
    art_key text not null default '',
    sort_order integer not null default 0,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);
create table cards (
    card_id text primary key,
    display_name text not null,
    description text not null default '',
    art_key text not null default '',
    starts_unlocked boolean not null default false,
    biome_id text not null references biomes(biome_id),
    floor_color_hex text not null check (floor_color_hex ~ '^#[0-9A-Fa-f]{6}$'),
    wall_color_hex text not null check (wall_color_hex ~ '^#[0-9A-Fa-f]{6}$'),
    segment_width integer not null check (segment_width >= 5),
    segment_height integer not null check (segment_height >= 5),
    entry_x integer not null check (entry_x >= 0),
    exit_x integer not null check (exit_x >= 0),
    obstacle_chance real not null check (obstacle_chance >= 0 and obstacle_chance <= 1),
    enemy_chance real not null check (enemy_chance >= 0 and enemy_chance <= 1),
    chest_chance real not null default 0.10 check (chest_chance >= 0 and chest_chance <= 1),
    elite_chance real not null default 0.00 check (elite_chance >= 0 and elite_chance <= 1),
    enemy_ids jsonb not null default '[]'::jsonb,
    base_difficulty integer not null default 1 check (base_difficulty >= 1),
    card_type text not null default 'balanced' check (card_type in ('safe', 'balanced', 'risky', 'elite')),
    reward_tier integer not null default 1 check (reward_tier >= 1),
    generation_tags jsonb not null default '[]'::jsonb,
    shop_unlock_segment integer not null default 0 check (shop_unlock_segment >= 0),
    sort_order integer not null default 0,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    check (entry_x < segment_width),
    check (exit_x < segment_width)
);

create table card_enemy_pool (
    card_id text not null references cards(card_id) on delete cascade,
    enemy_id text not null references enemy_archetypes(enemy_id),
    weight integer not null default 1 check (weight > 0),
    min_count integer not null default 0 check (min_count >= 0),
    max_count integer not null default 3 check (max_count >= min_count),
    primary key (card_id, enemy_id)
);

create table card_modifier_pool (
    card_id text not null references cards(card_id) on delete cascade,
    modifier_id text not null references world_modifier_definitions(modifier_id),
    weight integer not null default 1 check (weight > 0),
    guaranteed boolean not null default false,
    primary key (card_id, modifier_id)
);

create table card_reward_pool (
    card_id text not null references cards(card_id) on delete cascade,
    reward_type text not null check (reward_type in ('relic', 'consumable', 'card_unlock', 'gold', 'heal', 'emerald')),
    reward_id text not null,
    weight integer not null default 1 check (weight > 0),
    quantity integer not null default 1 check (quantity > 0),
    primary key (card_id, reward_type, reward_id)
);

create table chest_reward_pool (
    chest_tier text not null check (chest_tier in ('small', 'medium', 'rare')),
    reward_type text not null check (reward_type in ('gold', 'emerald')),
    reward_id text not null,
    weight integer not null default 1 check (weight > 0),
    quantity_min integer not null default 1 check (quantity_min >= 0),
    quantity_max integer not null default 1 check (quantity_max >= quantity_min),
    primary key (chest_tier, reward_type, reward_id)
);

-- ---------------------------------------------------------------------------
-- Perfil i progres persistent
-- ---------------------------------------------------------------------------

create table players (
    player_id text primary key,
    display_name text not null,
    preferred_language text not null default 'ca',
    preferred_run_length integer not null default 5 check (preferred_run_length in (5, 7)),
    selected_hero_mode text not null default 'prudent' check (selected_hero_mode in ('prudent', 'aggressive', 'escape')),
    profile_data jsonb not null default '{}'::jsonb,
    created_at timestamptz not null default now(),
    last_seen_at timestamptz not null default now()
);

create table player_progress (
    player_id text primary key references players(player_id) on delete cascade,
    unlocked_card_ids jsonb not null default '[]'::jsonb,
    unlocked_relic_ids jsonb not null default '[]'::jsonb,
    unlocked_modifier_ids jsonb not null default '[]'::jsonb,
    unlocked_divine_power_ids jsonb not null default '[]'::jsonb,
    completed_runs integer not null default 0,
    failed_runs integer not null default 0,
    total_runs_started integer not null default 0,
    total_cards_unlocked integer not null default 0,
    highest_segment_reached integer not null default 0,
    total_enemies_defeated integer not null default 0,
    total_damage_dealt integer not null default 0,
    total_damage_taken integer not null default 0,
    soft_currency integer not null default 0,
    hard_currency integer not null default 0,
    run5_unlocked boolean not null default true,
    run7_unlocked boolean not null default false,
    shop_enabled boolean not null default true,
    biomes_unlocked jsonb not null default '[]'::jsonb,
    progression_flags jsonb not null default '{}'::jsonb,
    updated_at timestamptz not null default now()
);

create table player_card_unlocks (
    player_id text not null references players(player_id) on delete cascade,
    card_id text not null references cards(card_id),
    unlocked_at timestamptz not null default now(),
    unlock_source text not null default 'default',
    primary key (player_id, card_id)
);

create table player_divine_power_unlocks (
    player_id text not null references players(player_id) on delete cascade,
    power_id text not null references divine_power_definitions(power_id),
    unlocked_at timestamptz not null default now(),
    unlock_source text not null default 'default',
    primary key (player_id, power_id)
);

create table player_relics (
    player_id text not null references players(player_id) on delete cascade,
    relic_id text not null references relic_definitions(relic_id),
    quantity integer not null default 1 check (quantity > 0),
    first_obtained_at timestamptz not null default now(),
    last_obtained_at timestamptz not null default now(),
    primary key (player_id, relic_id)
);

create table player_consumables (
    player_id text not null references players(player_id) on delete cascade,
    consumable_id text not null references consumable_definitions(consumable_id),
    quantity integer not null default 0 check (quantity >= 0),
    updated_at timestamptz not null default now(),
    primary key (player_id, consumable_id)
);

-- ---------------------------------------------------------------------------
-- Execucio de runs
-- ---------------------------------------------------------------------------
-- Aquest bloc guarda nomes historial de runs finalitzades.
-- Si el joc es tanca o s'interromp a mitja run, no s'ha de persistir res.

create table run_sessions (
    run_id bigint generated always as identity primary key,
    player_id text not null references players(player_id),
    run_seed text not null,
    status text not null check (status in ('completed', 'failed')),
    starting_card_id text not null references cards(card_id),
    target_segment_count integer not null default 5 check (target_segment_count in (5, 7)),
    segments_cleared integer not null default 0 check (segments_cleared >= 0),
    hero_mode text not null default 'prudent' check (hero_mode in ('prudent', 'aggressive', 'escape')),
    hero_max_health integer not null default 30 check (hero_max_health > 0),
    hero_current_health integer not null default 30 check (hero_current_health >= 0),
    hero_attack integer not null default 5 check (hero_attack >= 0),
    hero_defense integer not null default 1 check (hero_defense >= 0),
    hero_speed real not null default 1.0 check (hero_speed > 0),
    cards_unlocked_this_run integer not null default 0 check (cards_unlocked_this_run >= 0),
    gold_earned integer not null default 0 check (gold_earned >= 0),
    gold_spent integer not null default 0 check (gold_spent >= 0),
    emeralds_earned integer not null default 0 check (emeralds_earned >= 0),
    started_at timestamptz not null,
    ended_at timestamptz not null,
    run_notes jsonb not null default '{}'::jsonb,
    check (ended_at >= started_at)
);

create table run_equipped_divine_powers (
    run_id bigint not null references run_sessions(run_id) on delete cascade,
    slot_index integer not null check (slot_index between 1 and 2),
    power_id text not null references divine_power_definitions(power_id),
    activated_count integer not null default 0 check (activated_count >= 0),
    primary key (run_id, slot_index),
    unique (run_id, power_id)
);

create table run_deck_state (
    run_id bigint not null references run_sessions(run_id) on delete cascade,
    card_id text not null references cards(card_id),
    is_unlocked boolean not null default false,
    unlock_source text not null default 'starting_deck',
    unlocked_at timestamptz,
    primary key (run_id, card_id)
);

create table run_segments (
    run_segment_id bigint generated always as identity primary key,
    run_id bigint not null references run_sessions(run_id) on delete cascade,
    segment_index integer not null check (segment_index > 0),
    card_id text not null references cards(card_id),
    biome_id text not null references biomes(biome_id),
    floor_color_hex text not null check (floor_color_hex ~ '^#[0-9A-Fa-f]{6}$'),
    wall_color_hex text not null check (wall_color_hex ~ '^#[0-9A-Fa-f]{6}$'),
    segment_width integer not null check (segment_width >= 5),
    segment_height integer not null check (segment_height >= 5),
    entry_x integer not null check (entry_x >= 0),
    exit_x integer not null check (exit_x >= 0),
    obstacle_chance real not null check (obstacle_chance >= 0 and obstacle_chance <= 1),
    enemy_chance real not null check (enemy_chance >= 0 and enemy_chance <= 1),
    chest_chance real not null default 0.10 check (chest_chance >= 0 and chest_chance <= 1),
    elite_chance real not null default 0.00 check (elite_chance >= 0 and elite_chance <= 1),
    generated_seed text not null,
    difficulty_multiplier real not null default 1.0 check (difficulty_multiplier > 0),
    state text not null check (state in ('cleared', 'failed')),
    hero_health_on_enter integer not null default 30 check (hero_health_on_enter >= 0),
    hero_health_on_exit integer check (hero_health_on_exit >= 0),
    card_type text not null default 'balanced',
    reward_tier integer not null default 1 check (reward_tier >= 1),
    created_at timestamptz not null default now(),
    completed_at timestamptz,
    unique (run_id, segment_index)
);

create table run_segment_choices (
    run_id bigint not null references run_sessions(run_id) on delete cascade,
    segment_index integer not null check (segment_index > 0),
    choice_slot integer not null check (choice_slot between 1 and 3),
    offered_card_id text not null references cards(card_id),
    was_selected boolean not null default false,
    created_at timestamptz not null default now(),
    primary key (run_id, segment_index, choice_slot)
);

create table run_segment_modifiers (
    run_segment_modifier_id bigint generated always as identity primary key,
    run_segment_id bigint not null references run_segments(run_segment_id) on delete cascade,
    modifier_id text not null references world_modifier_definitions(modifier_id),
    source_card_id text references cards(card_id),
    is_guaranteed boolean not null default false,
    applied_order integer not null default 0,
    effect_snapshot jsonb not null default '{}'::jsonb,
    created_at timestamptz not null default now()
);

create table run_segment_enemies (
    run_segment_enemy_id bigint generated always as identity primary key,
    run_segment_id bigint not null references run_segments(run_segment_id) on delete cascade,
    enemy_id text not null references enemy_archetypes(enemy_id),
    spawn_x integer not null check (spawn_x >= 0),
    spawn_y integer not null check (spawn_y >= 0),
    spawned_max_health integer not null check (spawned_max_health > 0),
    spawned_attack integer not null check (spawned_attack >= 0),
    spawned_defense integer not null check (spawned_defense >= 0),
    spawned_speed real not null check (spawned_speed > 0),
    reward_gold integer not null default 0 check (reward_gold >= 0),
    defeated boolean not null default false,
    defeated_at timestamptz,
    metadata_json jsonb not null default '{}'::jsonb,
    created_at timestamptz not null default now()
);

create table run_segment_chests (
    run_segment_chest_id bigint generated always as identity primary key,
    run_segment_id bigint not null references run_segments(run_segment_id) on delete cascade,
    chest_tier text not null check (chest_tier in ('small', 'medium', 'rare')),
    spawn_x integer not null check (spawn_x >= 0),
    spawn_y integer not null check (spawn_y >= 0),
    is_opened boolean not null default false,
    gold_reward integer not null default 0 check (gold_reward >= 0),
    emerald_reward integer not null default 0 check (emerald_reward >= 0),
    reward_payload jsonb not null default '{}'::jsonb,
    opened_at timestamptz,
    created_at timestamptz not null default now()
);

create table run_rewards (
    run_reward_id bigint generated always as identity primary key,
    run_id bigint not null references run_sessions(run_id) on delete cascade,
    run_segment_id bigint references run_segments(run_segment_id) on delete set null,
    reward_type text not null,
    reward_id text not null,
    quantity integer not null default 1 check (quantity >= 0),
    metadata_json jsonb not null default '{}'::jsonb,
    created_at timestamptz not null default now()
);

create table run_events (
    run_event_id bigint generated always as identity primary key,
    run_id bigint not null references run_sessions(run_id) on delete cascade,
    run_segment_id bigint references run_segments(run_segment_id) on delete set null,
    event_type text not null,
    event_order integer not null default 0,
    payload_json jsonb not null default '{}'::jsonb,
    created_at timestamptz not null default now()
);

create table run_shop_offers (
    run_shop_offer_id bigint generated always as identity primary key,
    run_id bigint not null references run_sessions(run_id) on delete cascade,
    segment_index integer not null check (segment_index > 0),
    offer_slot integer not null check (offer_slot between 1 and 3),
    offer_id text not null references shop_offer_definitions(offer_id),
    cost_gold_snapshot integer not null check (cost_gold_snapshot >= 0),
    state text not null default 'offered' check (state in ('offered', 'purchased', 'skipped')),
    purchased_at timestamptz,
    created_at timestamptz not null default now(),
    unique (run_id, segment_index, offer_slot)
);

-- ---------------------------------------------------------------------------
-- Indexos
-- ---------------------------------------------------------------------------

create index idx_cards_biome on cards (biome_id, sort_order);
create index idx_cards_type on cards (card_type, is_active);
create index idx_card_enemy_pool_card on card_enemy_pool (card_id);
create index idx_card_modifier_pool_card on card_modifier_pool (card_id);
create index idx_card_reward_pool_card on card_reward_pool (card_id);
create index idx_player_card_unlocks_player on player_card_unlocks (player_id);
create index idx_player_divine_unlocks_player on player_divine_power_unlocks (player_id);
create index idx_run_sessions_player on run_sessions (player_id, ended_at desc);
create index idx_run_segments_run on run_segments (run_id, segment_index);
create index idx_run_segment_choices_run on run_segment_choices (run_id, segment_index);
create index idx_run_segment_enemies_segment on run_segment_enemies (run_segment_id, defeated);
create index idx_run_segment_chests_segment on run_segment_chests (run_segment_id, is_opened);
create index idx_run_rewards_run on run_rewards (run_id, created_at);
create index idx_run_events_run on run_events (run_id, event_order);
create index idx_run_shop_offers_run on run_shop_offers (run_id, segment_index);

-- ---------------------------------------------------------------------------
-- Seed de biomes
-- ---------------------------------------------------------------------------

insert into biomes (biome_id, display_name, description, floor_color_hex, wall_color_hex, ambient_config) values
('ruins', 'Ruines', 'Muralles caigudes, temples antics i recorreguts irregulars.', '#8E857D', '#3F3936', '{"music":"ruins_theme","fog":0.08,"mood":"ancient"}'::jsonb),
('dark_forest', 'Bosc Fosc', 'Vegetacio densa, ombres ritualistes i camins naturals.', '#5D7A52', '#243721', '{"music":"dark_forest_theme","fog":0.10,"mood":"wild"}'::jsonb),
('swamp', 'Panta', 'Terreny humit, zones lentes i recorreguts tortuosos.', '#667C63', '#243C2B', '{"music":"swamp_theme","fog":0.13,"mood":"decay"}'::jsonb),
('crypt', 'Cripta', 'Passadissos estrets, altars antics i pressio constant.', '#A3A3B1', '#41404F', '{"music":"crypt_theme","fog":0.06,"mood":"ritual"}'::jsonb);

-- ---------------------------------------------------------------------------
-- Seed d enemics
-- ---------------------------------------------------------------------------

insert into enemy_archetypes (
    enemy_id, display_name, description, max_health, attack, defense, speed,
    movement_pattern, rarity, sprite_key, behavior_config, reward_config
) values
('skeleton', 'Esquelet', 'Enemic estandard del joc, fiable i persistent.', 10, 4, 0, 1.00, 'chase', 'common', 'enemy_skeleton', '{"aggression":1.0,"avoidance":0.0,"ranged":false,"range":1}'::jsonb, '{"gold":2}'::jsonb),
('bat', 'Ratpenat', 'Enemic rapid amb poca vida i molt molest.', 6, 3, 0, 1.50, 'flank', 'common', 'enemy_bat', '{"aggression":1.15,"avoidance":0.0,"ranged":false,"range":1}'::jsonb, '{"gold":2}'::jsonb),
('zombie', 'Zombi', 'Enemic lent, resistent i bloquejador de camins.', 18, 5, 1, 0.65, 'slow_chase', 'uncommon', 'enemy_zombie', '{"aggression":0.9,"avoidance":0.0,"ranged":false,"range":1}'::jsonb, '{"gold":4}'::jsonb),
('ghost_elite', 'Fantasma Elite', 'Enemic elite a distancia amb alta pressio tactica.', 14, 6, 1, 1.10, 'ranged_guard', 'elite', 'enemy_ghost', '{"aggression":1.2,"avoidance":0.1,"ranged":true,"range":4}'::jsonb, '{"gold":7}'::jsonb);

-- ---------------------------------------------------------------------------
-- Seed de modificadors
-- ---------------------------------------------------------------------------

insert into world_modifier_definitions (
    modifier_id, display_name, description, art_key, modifier_type, rarity, stack_mode, effect_config, is_positive
) values
('safe_paths', 'Camins Clars', 'Menys obstacles i recorregut mes net per a l heroi.', 'mod_safe_paths', 'terrain', 'common', 'refresh', '{"extraObstacleChance":-0.05}'::jsonb, true),
('dense_growth', 'Vegetacio Densa', 'Incrementa obstacles i estreny alguns passos.', 'mod_dense_growth', 'terrain', 'common', 'refresh', '{"extraObstacleChance":0.06}'::jsonb, false),
('stagnant_mud', 'Fang Estancat', 'Algunes zones redueixen la mobilitat del segment.', 'mod_stagnant_mud', 'terrain', 'uncommon', 'refresh', '{"slowZones":true,"heroSpeedMultiplier":0.9}'::jsonb, false),
('healing_shrine', 'Santuari Curatiu', 'En entrar al segment l heroi recupera una mica de vida.', 'mod_healing_shrine', 'blessing', 'rare', 'refresh', '{"healOnEnter":3}'::jsonb, true),
('blood_mist', 'Boira Sagnant', 'Els enemics del segment son lleugerament mes perillosos.', 'mod_blood_mist', 'combat', 'uncommon', 'stack', '{"enemyAttackBonus":1}'::jsonb, false),
('elite_presence', 'Presencia Elite', 'Augmenta la probabilitat d enemic elite.', 'mod_elite_presence', 'encounter', 'rare', 'refresh', '{"eliteChanceBonus":0.10}'::jsonb, false);

-- ---------------------------------------------------------------------------
-- Seed de reliquies
-- ---------------------------------------------------------------------------

insert into relic_definitions (relic_id, display_name, description, art_key, rarity, effect_config) values
('relic_iron_heart', 'Cor de Ferro', 'Augmenta la vida maxima de l heroi.', 'relic_iron_heart', 'common', '{"heroMaxHealthBonus":3}'::jsonb),
('relic_warrior_spirit', 'Esperit del Guerrer', 'Augmenta l atac base de l heroi.', 'relic_warrior_spirit', 'common', '{"heroAttackBonus":1}'::jsonb),
('relic_swift_boots', 'Botes Agils', 'Augmenta lleugerament la velocitat base.', 'relic_swift_boots', 'uncommon', '{"heroSpeedBonus":0.10}'::jsonb),
('relic_golden_touch', 'Toc Daurat', 'Els cofres donen mes or.', 'relic_golden_touch', 'rare', '{"chestGoldBonusMultiplier":1.25,"goldBonus":0.25}'::jsonb),
('relic_shadow_instinct', 'Instint de l Ombra', 'Augmenta la probabilitat d evitar combats desfavorables.', 'relic_shadow_instinct', 'rare', '{"avoidCombatChance":0.20,"escapeChance":0.20}'::jsonb);

-- ---------------------------------------------------------------------------
-- Seed de consumibles
-- ---------------------------------------------------------------------------

insert into consumable_definitions (consumable_id, display_name, description, art_key, max_stack, effect_config) values
('healing_potion', 'Pocio de Curacio', 'Recupera vida durant o entre segments.', 'consumable_healing_potion', 10, '{"heal":8}'::jsonb),
('speed_dose', 'Dosi de Velocitat', 'Augment temporal de velocitat.', 'consumable_speed_dose', 5, '{"speedMultiplier":1.25,"durationSeconds":8}'::jsonb),
('smoke_bomb', 'Bomba de Fum', 'Ajuda a evitar un combat desfavorable.', 'consumable_smoke_bomb', 5, '{"skipEncounter":true}'::jsonb),
('guard_token', 'Segell de Proteccio', 'Escut temporal de defensa.', 'consumable_guard_token', 5, '{"defenseBonus":2,"durationSeconds":8}'::jsonb);

-- ---------------------------------------------------------------------------
-- Seed de poders divins
-- ---------------------------------------------------------------------------

insert into divine_power_definitions (
    power_id, display_name, description, art_key, power_type, cooldown_seconds,
    duration_seconds, max_charges, unlock_cost, starts_unlocked, sort_order, effect_config
) values
('speed_blessing', 'Benediccio de Velocitat', 'Augmenta temporalment la velocitat de l heroi.', 'divine_speed_blessing', 'buff', 30, 8, 2, 0, true, 10, '{"speedMultiplier":1.35}'::jsonb),
('strength_blessing', 'Benediccio de Forca', 'Augmenta temporalment l atac de l heroi.', 'divine_strength_blessing', 'buff', 35, 8, 2, 8, false, 20, '{"attackBonus":2}'::jsonb),
('shield_blessing', 'Benediccio d Escut', 'Augmenta temporalment la defensa de l heroi.', 'divine_shield_blessing', 'defense', 35, 8, 2, 8, false, 30, '{"defenseBonus":2}'::jsonb),
('command_attack', 'Ordre d Atac', 'Forca temporalment un comportament mes agressiu.', 'divine_command_attack', 'behavior', 40, 6, 2, 10, false, 40, '{"heroMode":"aggressive"}'::jsonb),
('command_escape', 'Ordre de Fugida', 'Forca temporalment un comportament de prudencia o fugida.', 'divine_command_escape', 'behavior', 40, 6, 2, 10, false, 50, '{"heroMode":"escape"}'::jsonb),
('summon_companion', 'Invocar Company', 'Invoca un company temporal que ajuda durant el segment.', 'divine_summon_companion', 'summon', 60, 0, 2, 14, false, 60, '{"spawnCompanion":true,"durationSegments":1}'::jsonb);

-- ---------------------------------------------------------------------------
-- Seed de botiga
-- ---------------------------------------------------------------------------

insert into shop_offer_definitions (
    offer_id, display_name, description, art_key, offer_type, cost_gold,
    reward_type, reward_id, reward_quantity, duration_segments, effect_config, sort_order
) values
('heal_small', 'Curacio Petita', 'Recupera 8 punts de vida.', 'shop_heal_small', 'heal', 6, 'heal', 'heal_small', 8, 0, '{"heal":8}'::jsonb, 10),
('heal_large', 'Curacio Gran', 'Recupera 15 punts de vida.', 'shop_heal_large', 'heal', 10, 'heal', 'heal_large', 15, 0, '{"heal":15}'::jsonb, 20),
('buff_strength', 'Forca Temporal', 'Afegeix +2 atac durant el proxim segment.', 'shop_buff_strength', 'buff', 7, 'buff', 'buff_strength', 1, 1, '{"attackBonus":2,"durationSegments":1}'::jsonb, 30),
('buff_speed', 'Velocitat Temporal', 'Afegeix +25% de velocitat durant el proxim segment.', 'shop_buff_speed', 'buff', 6, 'buff', 'buff_speed', 1, 1, '{"speedMultiplierBonus":0.25,"durationSegments":1}'::jsonb, 40),
('buff_shield', 'Escut Temporal', 'Afegeix +2 defensa durant el proxim segment.', 'shop_buff_shield', 'buff', 7, 'buff', 'buff_shield', 1, 1, '{"defenseBonus":2,"durationSegments":1}'::jsonb, 50),
('reroll_cards', 'Reroll Cartes', 'Renova les opcions de cartes del final de segment.', 'shop_reroll_cards', 'utility', 5, 'utility', 'reroll_cards', 1, 0, '{"rerollCards":true}'::jsonb, 60),
('summon_companion_offer', 'Invocar Company', 'Invoca un company temporal per al proxim segment.', 'shop_summon_companion', 'summon', 12, 'summon', 'summon_companion', 1, 1, '{"spawnCompanion":true,"durationSegments":1}'::jsonb, 70),
('temporary_equipment', 'Equip Temporal', 'Afegeix +1 atac i +1 defensa durant 2 segments.', 'shop_temporary_equipment', 'equipment', 9, 'buff', 'temporary_equipment', 1, 2, '{"attackBonus":1,"defenseBonus":1,"durationSegments":2}'::jsonb, 80);

-- ---------------------------------------------------------------------------
-- Seed de resultats de run
-- ---------------------------------------------------------------------------

insert into run_result_definitions (result_id, display_name, description, art_key, sort_order) values
('run_completed', 'Run completada', 'Imatge per al resum de victoria al final de la run.', 'run_result_victory', 10),
('run_failed', 'Run fallida', 'Imatge per al resum de derrota al final de la run.', 'run_result_failure', 20);
-- ---------------------------------------------------------------------------
-- Seed de cartes
-- ---------------------------------------------------------------------------

insert into cards (
    card_id, display_name, description, art_key, starts_unlocked, biome_id,
    floor_color_hex, wall_color_hex, segment_width, segment_height, entry_x, exit_x,
    obstacle_chance, enemy_chance, chest_chance, elite_chance, enemy_ids,
    base_difficulty, card_type, reward_tier, generation_tags, shop_unlock_segment, sort_order
) values
('abandoned_path', 'Cami Abandonat', 'Carta segura de ruines amb poca pressio i recorregut relativament net.', 'card_ruins_abandoned', true, 'ruins', '#8E857D', '#3F3936', 16, 10, 1, 14, 0.09, 0.11, 0.11, 0.00, '["skeleton","bat"]'::jsonb, 1, 'safe', 1, '["safe","starter","ruins"]'::jsonb, 0, 10),
('unstable_ruins', 'Ruines Inestables', 'Carta equilibrada de ruines amb recorregut irregular i risc moderat.', 'card_ruins_unstable', true, 'ruins', '#8E857D', '#3F3936', 17, 10, 1, 15, 0.15, 0.16, 0.13, 0.02, '["skeleton","bat","zombie"]'::jsonb, 2, 'balanced', 2, '["balanced","ruins"]'::jsonb, 0, 20),
('fallen_fortress', 'Fortalesa Caiguda', 'Carta arriscada de ruines amb mes enemics i millors recompenses.', 'card_ruins_fallen_fortress', false, 'ruins', '#8E857D', '#3F3936', 18, 11, 1, 16, 0.20, 0.22, 0.16, 0.10, '["skeleton","zombie","ghost_elite"]'::jsonb, 3, 'risky', 3, '["risky","elite_chance","ruins"]'::jsonb, 3, 30),
('dark_clearing', 'Clariana Fosca', 'Carta segura de bosc fosc amb camins mes oberts.', 'card_forest_dark_clearing', true, 'dark_forest', '#5D7A52', '#243721', 16, 10, 1, 14, 0.10, 0.12, 0.11, 0.00, '["skeleton","bat"]'::jsonb, 1, 'safe', 1, '["safe","starter","forest"]'::jsonb, 0, 40),
('dense_forest', 'Bosc Espes', 'Carta equilibrada de bosc amb obstacles naturals i combats normals.', 'card_forest_dense', true, 'dark_forest', '#5D7A52', '#243721', 17, 11, 1, 15, 0.17, 0.16, 0.13, 0.02, '["skeleton","bat","zombie"]'::jsonb, 2, 'balanced', 2, '["balanced","forest"]'::jsonb, 0, 50),
('cursed_forest', 'Bosc Maleit', 'Carta arriscada de bosc amb mes enemics rapids i mes recompensa.', 'card_forest_cursed', false, 'dark_forest', '#5D7A52', '#243721', 18, 11, 1, 16, 0.20, 0.22, 0.16, 0.10, '["bat","zombie","ghost_elite"]'::jsonb, 3, 'risky', 3, '["risky","elite_chance","forest"]'::jsonb, 3, 60),
('calm_marsh', 'Panta Tranquil', 'Carta segura de panta amb poca pressio pero recorregut lent.', 'card_swamp_calm', true, 'swamp', '#667C63', '#243C2B', 16, 11, 1, 14, 0.12, 0.11, 0.12, 0.00, '["skeleton","zombie"]'::jsonb, 1, 'safe', 1, '["safe","starter","swamp"]'::jsonb, 0, 70),
('stagnant_waters', 'Aigues Estancades', 'Carta equilibrada de panta amb rutes alternatives i risc moderat.', 'card_swamp_stagnant', true, 'swamp', '#667C63', '#243C2B', 17, 11, 1, 15, 0.16, 0.16, 0.14, 0.02, '["skeleton","bat","zombie"]'::jsonb, 2, 'balanced', 2, '["balanced","swamp"]'::jsonb, 0, 80),
('deep_swamp', 'Panta Profund', 'Carta arriscada de panta amb mes pressio i millors cofres.', 'card_swamp_deep', false, 'swamp', '#667C63', '#243C2B', 18, 12, 1, 16, 0.21, 0.22, 0.18, 0.10, '["bat","zombie","ghost_elite"]'::jsonb, 3, 'risky', 3, '["risky","elite_chance","swamp"]'::jsonb, 3, 90),
('silent_crypt', 'Cripta Silenciosa', 'Carta segura de cripta amb passadissos relativament clars.', 'card_crypt_silent', true, 'crypt', '#A3A3B1', '#41404F', 16, 10, 1, 14, 0.13, 0.12, 0.11, 0.00, '["skeleton","zombie"]'::jsonb, 1, 'safe', 1, '["safe","starter","crypt"]'::jsonb, 0, 100),
('ancient_corridors', 'Passadissos Antics', 'Carta equilibrada de cripta amb lluita probable i rutes estretes.', 'card_crypt_ancient_corridors', true, 'crypt', '#A3A3B1', '#41404F', 17, 10, 1, 15, 0.18, 0.17, 0.13, 0.03, '["skeleton","zombie","bat"]'::jsonb, 2, 'balanced', 2, '["balanced","crypt"]'::jsonb, 0, 110),
('profaned_crypt', 'Cripta Profanada', 'Carta arriscada de cripta amb mes elites i mes recompensa.', 'card_crypt_profaned', false, 'crypt', '#A3A3B1', '#41404F', 18, 11, 1, 16, 0.22, 0.23, 0.17, 0.12, '["zombie","ghost_elite","bat"]'::jsonb, 3, 'risky', 3, '["risky","elite_chance","crypt"]'::jsonb, 3, 120);

-- ---------------------------------------------------------------------------
-- Pools de cartes
-- ---------------------------------------------------------------------------

insert into card_enemy_pool (card_id, enemy_id, weight, min_count, max_count) values
('abandoned_path', 'skeleton', 6, 1, 3),
('abandoned_path', 'bat', 2, 0, 1),
('unstable_ruins', 'skeleton', 5, 1, 3),
('unstable_ruins', 'bat', 3, 0, 2),
('unstable_ruins', 'zombie', 2, 0, 1),
('fallen_fortress', 'skeleton', 4, 1, 3),
('fallen_fortress', 'zombie', 4, 1, 2),
('fallen_fortress', 'ghost_elite', 1, 0, 1),
('dark_clearing', 'skeleton', 5, 1, 2),
('dark_clearing', 'bat', 3, 0, 2),
('dense_forest', 'skeleton', 4, 1, 3),
('dense_forest', 'bat', 4, 1, 3),
('dense_forest', 'zombie', 2, 0, 1),
('cursed_forest', 'bat', 5, 1, 3),
('cursed_forest', 'zombie', 3, 0, 2),
('cursed_forest', 'ghost_elite', 1, 0, 1),
('calm_marsh', 'skeleton', 3, 1, 2),
('calm_marsh', 'zombie', 4, 0, 1),
('stagnant_waters', 'skeleton', 3, 1, 2),
('stagnant_waters', 'bat', 2, 0, 1),
('stagnant_waters', 'zombie', 4, 1, 2),
('deep_swamp', 'bat', 3, 0, 2),
('deep_swamp', 'zombie', 5, 1, 2),
('deep_swamp', 'ghost_elite', 1, 0, 1),
('silent_crypt', 'skeleton', 4, 1, 2),
('silent_crypt', 'zombie', 3, 0, 1),
('ancient_corridors', 'skeleton', 4, 1, 3),
('ancient_corridors', 'zombie', 4, 1, 2),
('ancient_corridors', 'bat', 2, 0, 1),
('profaned_crypt', 'zombie', 5, 1, 2),
('profaned_crypt', 'ghost_elite', 2, 0, 1),
('profaned_crypt', 'bat', 2, 0, 2);

insert into card_modifier_pool (card_id, modifier_id, weight, guaranteed) values
('abandoned_path', 'safe_paths', 3, true),
('unstable_ruins', 'dense_growth', 2, false),
('fallen_fortress', 'elite_presence', 3, true),
('dark_clearing', 'safe_paths', 2, true),
('dense_forest', 'dense_growth', 3, true),
('cursed_forest', 'blood_mist', 3, false),
('calm_marsh', 'healing_shrine', 1, false),
('stagnant_waters', 'stagnant_mud', 3, true),
('deep_swamp', 'stagnant_mud', 3, true),
('silent_crypt', 'safe_paths', 1, false),
('ancient_corridors', 'dense_growth', 2, true),
('profaned_crypt', 'elite_presence', 3, true);

insert into card_reward_pool (card_id, reward_type, reward_id, weight, quantity) values
('abandoned_path', 'heal', 'heal_small', 2, 2),
('abandoned_path', 'consumable', 'healing_potion', 1, 1),
('unstable_ruins', 'gold', 'gold_small', 2, 4),
('unstable_ruins', 'card_unlock', 'fallen_fortress', 1, 1),
('dark_clearing', 'heal', 'heal_small', 2, 2),
('dark_clearing', 'consumable', 'speed_dose', 1, 1),
('dense_forest', 'consumable', 'healing_potion', 2, 1),
('dense_forest', 'card_unlock', 'cursed_forest', 1, 1),
('calm_marsh', 'heal', 'heal_small', 2, 2),
('calm_marsh', 'consumable', 'guard_token', 1, 1),
('stagnant_waters', 'consumable', 'smoke_bomb', 1, 1),
('stagnant_waters', 'card_unlock', 'deep_swamp', 1, 1),
('silent_crypt', 'heal', 'heal_small', 2, 2),
('silent_crypt', 'consumable', 'smoke_bomb', 1, 1),
('ancient_corridors', 'relic', 'relic_iron_heart', 1, 1),
('ancient_corridors', 'card_unlock', 'profaned_crypt', 1, 1),
('fallen_fortress', 'gold', 'gold_large', 3, 8),
('fallen_fortress', 'relic', 'relic_warrior_spirit', 1, 1),
('cursed_forest', 'gold', 'gold_large', 3, 8),
('cursed_forest', 'relic', 'relic_shadow_instinct', 1, 1),
('deep_swamp', 'gold', 'gold_large', 3, 8),
('deep_swamp', 'relic', 'relic_golden_touch', 1, 1),
('profaned_crypt', 'relic', 'relic_swift_boots', 1, 1),
('profaned_crypt', 'emerald', 'emerald_small', 1, 1);

-- ---------------------------------------------------------------------------
-- Recompenses de cofres
-- ---------------------------------------------------------------------------

insert into chest_reward_pool (chest_tier, reward_type, reward_id, weight, quantity_min, quantity_max) values
('small', 'gold', 'gold_small', 12, 4, 5),
('small', 'emerald', 'emerald_tiny', 1, 1, 1),
('medium', 'gold', 'gold_medium', 12, 6, 7),
('medium', 'emerald', 'emerald_small', 2, 1, 1),
('rare', 'gold', 'gold_large', 10, 9, 12),
('rare', 'emerald', 'emerald_small', 3, 1, 1),
('rare', 'emerald', 'emerald_medium', 1, 2, 2);

-- ---------------------------------------------------------------------------
-- Perfil local-player i progres inicial
-- ---------------------------------------------------------------------------

insert into players (
    player_id, display_name, preferred_language, preferred_run_length, selected_hero_mode, profile_data
) values
('local-player', 'Jugador Local', 'ca', 5, 'prudent', '{"preferredRunLength":5,"selectedHeroMode":"prudent"}'::jsonb);

insert into player_progress (
    player_id,
    unlocked_card_ids,
    unlocked_relic_ids,
    unlocked_modifier_ids,
    unlocked_divine_power_ids,
    completed_runs,
    failed_runs,
    total_runs_started,
    total_cards_unlocked,
    highest_segment_reached,
    total_enemies_defeated,
    total_damage_dealt,
    total_damage_taken,
    soft_currency,
    hard_currency,
    run5_unlocked,
    run7_unlocked,
    shop_enabled,
    biomes_unlocked,
    progression_flags
) values (
    'local-player',
    '["abandoned_path","unstable_ruins","dark_clearing","dense_forest","calm_marsh","stagnant_waters","silent_crypt","ancient_corridors"]'::jsonb,
    '[]'::jsonb,
    '["safe_paths","dense_growth","stagnant_mud","healing_shrine"]'::jsonb,
    '["speed_blessing"]'::jsonb,
    0,
    0,
    0,
    8,
    0,
    0,
    0,
    0,
    0,
    0,
    true,
    false,
    true,
    '["ruins","dark_forest","swamp","crypt"]'::jsonb,
    '{"run5Unlocked":true,"run7Unlocked":false,"shopEnabled":true}'::jsonb
);

insert into player_card_unlocks (player_id, card_id, unlock_source) values
('local-player', 'abandoned_path', 'default'),
('local-player', 'unstable_ruins', 'default'),
('local-player', 'dark_clearing', 'default'),
('local-player', 'dense_forest', 'default'),
('local-player', 'calm_marsh', 'default'),
('local-player', 'stagnant_waters', 'default'),
('local-player', 'silent_crypt', 'default'),
('local-player', 'ancient_corridors', 'default');

insert into player_divine_power_unlocks (player_id, power_id, unlock_source) values
('local-player', 'speed_blessing', 'default');

insert into player_consumables (player_id, consumable_id, quantity) values
('local-player', 'healing_potion', 0),
('local-player', 'speed_dose', 0),
('local-player', 'smoke_bomb', 0),
('local-player', 'guard_token', 0);

commit;


