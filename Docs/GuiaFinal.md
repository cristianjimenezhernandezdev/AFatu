# Guia final de seguiment

Aquest document recull la prioritat recomanada per continuar `Architectus Fati` i s'usara com a referencia de treball en les seguents iteracions.

## Estat actual

El projecte ja te:

- menu principal;
- selector de perfil i guardat multi-perfil;
- arbre de millores;
- loop de run funcional;
- i una base de dades/documentacio que modela mes sistemes dels que el client explota ara mateix.

## Que falta aprofitar de la BDD

Les peces amb millor retorn actual son:

1. `player_consumables` i consumibles jugables reals.
2. `player_relics` i inventari/meta-progres de reliquies.
3. persistencia real de `run_sessions` i estat de run per reprendre partida.
4. metriques i analytics com:
   - `total_damage_dealt`
   - `total_damage_taken`
   - `progression_flags`
   - `profile_data`

## Ordre recomanat

### Iteracio 1

Consumibles jugables:

- usar-los dins la run;
- mostrar quantitat i disponibilitat;
- persistir-los correctament per perfil;
- i connectar-los amb el HUD.

### Iteracio 2

Reliquies i inventari persistent:

- col.leccio de reliquies;
- beneficis visibles;
- i coherencia amb `player_relics`.

### Iteracio 3

Persistencia de run:

- guardar `run_sessions`;
- reprendre partida;
- i donar al menu principal una opcio real de `Continuar`.

## Prioritat confirmada

La primera implementacio que s'ha decidit abordar es:

- consumibles;
- en segments assumibles;
- abans de continuar amb reliquies o persistencia completa de runs.
- s'ha de guardar registre de progres al document makingof 
- si el sistema necessita de noves implementacions i canvis a fer des de unity s'ha de fer un document apart on s'expliqui com fer-los.
