# ArchitectusFati API

API ASP.NET Core per servir el contingut i el progres del joc a Unity sense exposar la connection string de Neon al client.

## Configuracio

1. Rota la password de Neon si la connection string antiga s'ha compartit fora del teu entorn privat.
2. Defineix la variable d'entorn `ARCHITECTUSFATI_NEON_CONNECTION_STRING` amb la nova connection string.
3. Executa:

```powershell
$env:ARCHITECTUSFATI_NEON_CONNECTION_STRING = "postgresql://USER:PASSWORD@HOST/DB?sslmode=require&channel_binding=require"
$env:DOTNET_CLI_HOME = "e:\Dev\AFati\ArchitectusFati\.dotnet"
dotnet run --project Backend\ArchitectusFati.Api\ArchitectusFati.Api.csproj
```

L'API arrenca per defecte a `http://localhost:5123` en el perfil `http`.

## Esquema SQL

Per defecte el backend aplica `Database/002_full_game_schema.sql`, que crea:

- contingut global: biomes, enemics, modificadors, reliquies, consumibles
- cartes i pools: cards, card_enemy_pool, card_modifier_pool, card_reward_pool
- progres jugador: players, player_progress, player_card_unlocks, player_relics, player_consumables
- runs: run_sessions, run_deck_state, run_segments, run_segment_choices, run_segment_modifiers, run_segment_enemies, run_rewards, run_events

## Endpoints

- `GET /api/health`
- `GET /api/content/bootstrap`
- `GET /api/cards`
- `GET /api/players/{playerId}/progress`
- `PUT /api/players/{playerId}/progress`
- `POST /api/players/{playerId}/runs`
- `GET /api/players/{playerId}/runs/active`
- `GET /api/runs/{runId}`
- `POST /api/runs/{runId}/segments`
- `POST /api/runs/{runId}/finish`

## Notes

- `RunManager` a Unity ja pot consumir cartes i progres remot si actives `useRemoteApi` i poses `apiBaseUrl` i `remotePlayerId`.
- `GET /api/content/bootstrap` et serveix per carregar el contingut mestre del joc des de Neon.
- Si l'API no respon, el joc continua amb fallback local per a la part que ja estava integrada.
