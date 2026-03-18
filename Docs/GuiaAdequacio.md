# Guia d'adequacio BDD-codi

Aquest document fixa el pla per deixar `Architectus Fati` coherent amb la BDD final, pero amb una decisio important ja tancada:

- no es guardara cap run a mitges;
- no hi haurà opcio de reprendre partida;
- i si el joc es tanca durant una run, no s'ha de persistir res ni en local ni a la BDD.

Per tant, el model de dades s'ha d'entendre com un contracte per a:

- contingut mestre;
- progres persistent de perfil;
- i historial final de runs acabades.

## Objectiu

L'objectiu no es nomes "tindre taules creades", sino que cada bloc de la BDD que es mantinga a `BddFinal.sql` tinga:

- un significat clar;
- una correspondencia real al runtime o al progres;
- i una logica d'ús consistent entre Unity, repositoris locals i backend.

## Contracte funcional

Despres de la simplificacio de `BddFinal.sql`, la logica esperada queda aixi:

### Progres persistent

Aquest bloc si que s'ha de guardar sempre:

- `players`
- `player_progress`
- `player_card_unlocks`
- `player_divine_power_unlocks`
- `player_relics`
- `player_consumables`

### Runs finalitzades

Aquest bloc nomes s'ha de guardar quan una run acaba en `completed` o `failed`:

- `run_sessions`
- `run_equipped_divine_powers`
- `run_deck_state`
- `run_segments`
- `run_segment_choices`
- `run_segment_modifiers`
- `run_segment_enemies`
- `run_segment_chests`
- `run_rewards`
- `run_events`
- `run_shop_offers`

### Que no s'ha de fer

- no s'ha de guardar una run activa;
- no s'ha de fer autosave de segment;
- no s'ha de restaurar una run en tornar a obrir el joc;
- i el menu principal ha d'arrencar sempre com si no hi haguera cap run pendent.

## Desajustos actuals detectats

Ara mateix el projecte encara te aquests buits respecte a la BDD:

1. `profile_data` i `progression_flags` existeixen a la BDD pero no tenen una representacio real al model local.
2. `total_damage_dealt` i `total_damage_taken` existeixen al model de progres pero no s'estan incrementant des del combat.
3. `chest_reward_pool` existeix a la BDD, pero el runtime de cofres continua resolent recompenses amb regles fixes de `BalanceConfig`.
4. La capa local de runs nomes registra una part del model i ho fa en memoria, sense cap flush final estructurat de totes les taules de run.
5. El client remot legacy encara treballa amb models antics i no representa el progres real actual.

## Pla d'implementacio

### Fase 1. Tancar el contracte de dades

Objectiu:

- usar `Docs/BddFinal.sql` com a font de veritat;
- i deixar clar que la persistencia de runs a mitges queda descartada.

Tasques:

- revisar i sincronitzar la resta de documentacio que encara parle de reprendre runs;
- adaptar l'esquema SQL del backend perque copie aquest contracte;
- i eliminar qualsevol suposicio de `active run` de la capa de dades remota.

Resultat esperat:

- una sola definicio valida del model;
- i zero ambiguitat sobre el comportament d'una run interrompuda.

### Fase 2. Alinear progres de perfil amb la BDD

Objectiu:

- fer que el bloc persistent del jugador use tots els camps que la BDD manté.

Tasques:

- afegir representacio local de `profile_data`;
- afegir representacio local de `progression_flags`;
- decidir format de serialitzacio estable per a Unity.

Recomanacio tecnica:

- com `JsonUtility` no gestiona be diccionaris arbitraris, convé usar JSON pla en camps `string` o estructures serialitzables simples de clau-valor;
- i convertir-les a JSON de BDD nomes al repositori.

Resultat esperat:

- `players` i `player_progress` deixen de tindre camps "decoratius";
- i qualsevol metadada persistent te una ubicacio clara.

### Fase 3. Connectar les metriques de combat

Objectiu:

- omplir realment `total_damage_dealt` i `total_damage_taken`.

Tasques:

- actualitzar `RunManager` o una capa de telemetria per sumar el dany de `CombatSystem`;
- comptar tant combat cos a cos com atac a distancia;
- i validar que els valors es desen per perfil al final de cada combat o segment.

Resultat esperat:

- les metriques de `player_progress` passen de ser camps morts a analytics reals.

### Fase 4. Fer servir totes les fonts de contingut de la BDD

Objectiu:

- que el runtime es nodreixi de la BDD i no de regles duplicades quan ja existeix una taula pensada per aixo.

Tasques:

- substituir la logica fixa de recompenses de cofres per `chest_reward_pool`;
- validar que els seeds locals reflecteixen totes les definitions i pools actius;
- i eliminar duplicacions entre `BalanceConfig` i el contingut de dades quan el model ja existeixi a la BDD.

Resultat esperat:

- menys logica hardcodejada;
- i mes coherencia entre seed, backend i runtime.

### Fase 5. Registrar runs nomes al final, pero amb cobertura completa

Objectiu:

- mantenir la decisio de no persistir runs a mitges;
- pero guardar correctament una run completa o fallida quan acaba.

Tasques:

- mantindre tota la run en memoria mentre es juga;
- crear un `final run snapshot` que es construeixi nomes en `CompleteRun()` o `FailRun()`;
- mapar aquest snapshot a totes les taules de run que continuen existint a la BDD.

Aquest snapshot hauria d'incloure:

- sessio general de run;
- poders divins equipats i activacions;
- estat final del deck desbloquejat a la run;
- segments recorreguts;
- modifiers aplicats;
- enemics generats i derrotats;
- cofres generats i oberts;
- recompenses atorgades;
- esdeveniments;
- i ofertes de botiga mostrades, comprades o ignorades.

Resultat esperat:

- la BDD conserva historial complet de runs acabades;
- i continua sense guardar res si la partida queda tallada abans del final.

### Fase 6. Adequar backend i client remot

Objectiu:

- evitar que la capa remota quede desfasada respecte al model nou.

Tasques:

- actualitzar l'API per al nou contracte de `run_sessions` finalistes;
- revisar `RemoteGameApiClient` i els models legacy;
- i decidir si el client remot s'actualitza ara o es desactiva formalment fins que la nova capa estiga preparada.

Resultat esperat:

- cap model remot antic que puga donar una imatge falsa del progres real.

### Fase 7. Verificacio funcional

Objectiu:

- assegurar que cada taula mantinguda a la BDD te una ruta real d'ús.

Checklist minima:

- crear perfil nou;
- canviar perfil actiu;
- desbloquejar cartes, poders, relíquies i consumibles;
- jugar runs completes i fallides;
- verificar metriques de dany;
- verificar recompenses de cofres des de `chest_reward_pool`;
- verificar que en tancar el joc a mitja run no queda res persistent de la run;
- i verificar que el menu principal sempre arrenca sense estat pendent de run.

## Prioritat recomanada

Per ordre de valor i risc, la sequencia recomanada es:

1. alinear `profile_data`, `progression_flags` i metriques de combat;
2. fer servir `chest_reward_pool` i eliminar duplicacions de contingut;
3. preparar el `final run snapshot`;
4. adequar backend i client remot;
5. tancar la verificacio amb una matriu BDD versus codi.

## Resultat final buscat

Quan aquesta guia estiga implementada, el projecte hauria de quedar aixi:

- la BDD nomes contindra taules que el joc realment entén i pot omplir;
- el codi fara servir tots els blocs de dades que la BDD manté;
- les runs interrompudes no deixaran rastre persistent;
- i el menu principal sempre començara net, mentre que el progres meta i l'historial final de runs quedaran correctament registrats.
