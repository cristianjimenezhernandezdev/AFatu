# MakingOf

## Objectiu del document

Aquest document recull la progresio del desenvolupament d'Architectus Fati fins a l'estat actual del MVP vertical slice. Serveix per entendre com s'ha anat construint el joc, quines decisions s'han pres, quina documentacio s'ha generat, quins sistemes existeixen ara mateix, quins fitxers formen part del joc i quins passos cal fer dins Unity per mantenir l'escena principal correctament connectada.

Aquest document no substitueix la documentacio de balanc ni l'SQL de la base de dades. El seu objectiu es deixar rastre del proces de produccio i fer seguiment del projecte.

## Resum del projecte

Architectus Fati es un roguelite de control indirecte. El jugador no controla directament l'heroi. En lloc d'aixo, influeix en la run mitjancant cartes de bioma, poders divins i decisions estrategiques entre segments. L'heroi es mou automaticament, avalua perill, combat, evita o fuig segons la situacio i intenta arribar al Goal de cada segment.

L'evolucio del projecte s'ha fet en capes:

- primer, una base jugable de grid, moviment, enemics i objectiu;
- despres, una capa visual procedural per abandonar la representacio minima d'un sol pixel;
- mes endavant, una definicio formal del balanc del vertical slice;
- finalment, una refactoritzacio orientada a dades per ajustar el joc al model definitiu de la base de dades.

## Cronologia del desenvolupament

## Fase 1. Prototip base jugable

En la primera fase es va muntar una base de joc 2D per grid amb una estructura simple pero funcional.

Es van definir aquests conceptes inicials:

- un `WorldGrid` per generar i gestionar el segment;
- un `PlayerGridMovement` per moure automaticament l'heroi;
- un `GoalTile` com a objectiu de cada segment;
- un `Enemy` i `EnemyGridMovement` per als enemics basics;
- un `RunManager` inicial per governar la progressio de la run;
- un sistema de cartes de segment basat en `MapCardData`.

Objectiu d'aquesta fase:

- validar que el loop base existia;
- comprovar que es podia generar un segment, moure l'heroi i completar-lo;
- tenir una base sobre la qual iterar visualment i sistemicament.

Limitacions d'aquesta fase:

- dades molt locals i poc estructurades;
- visuals molt simples;
- poca separacio entre dades, logica i presentacio;
- balanc encara no formalitzat.

## Fase 2. Evolucio visual del player

En aquest punt el `Player` estava representat per un unic pixel. Es va decidir substituir aquesta representacio per una figura procedural construida per codi, sense sprites externs.

Canvis introduits:

- es va crear `ProceduralPlayerRenderer`;
- el player va passar a representar-se amb una mini-graella interna de pixels;
- es van definir formes diferents per a `idle`, `pas esquerra` i `pas dreta`;
- els subpixels es generen com a fills del GameObject del `Player`;
- es va preparar la base per futures animacions i variants del personatge.

Objectiu d'aquesta fase:

- donar identitat visual al personatge;
- mantenir la logica del moviment sense tocar-la;
- tenir una estica procedural coherent amb el to del joc.

## Fase 3. Millora visual d'enemics i entorn

Despres del player, es va ampliar l'estetica procedural a la resta del joc.

Canvis introduits:

- es van definir visuals procedurals per als enemics `skeleton` i `bat`;
- es va crear una fabrica procedural per al terra i els obstacles de l'entorn;
- el terra va passar a tenir subgraelles i variacions de color;
- els obstacles van adoptar formes de ruina o arbre segons el bioma;
- el player es va enriquir amb varis colors;
- es va augmentar la sensacio de detall i de densitat visual del mapa.

Objectiu d'aquesta fase:

- fer el joc mes llegible;
- donar personalitat als biomes i als enemics;
- reforcar la direccio artistica de pixel procedural sense assets externs.

## Fase 4. Formalitzacio del balanc del vertical slice

Quan el joc ja tenia una base jugable i un primer llenguatge visual, es va formalitzar el balanc del vertical slice en un document especific.

Canvis introduits:

- es va definir la vida, atac, defensa i velocitat de l'heroi;
- es va definir la formula de dany minima;
- es va descriure la logica de risc del comportament de l'heroi;
- es van definir els quatre enemics del MVP: `skeleton`, `bat`, `zombie`, `ghost_elite`;
- es va definir l'economia d'or i esmeraldes;
- es va establir la botiga, els poders divins i la corba de dificultat;
- es va descriure la diferencia entre runs curtes de 5 segments i llargues de 7.

Objectiu d'aquesta fase:

- tenir un document de referencia per al vertical slice;
- alinear disseny i implementacio;
- preparar el projecte per a proves i iteracio posterior.

## Fase 5. SQL definitiu com a font de veritat

La seguent gran passa va ser l'arribada de la base de dades definitiva del projecte, en forma d'un SQL complet amb taules i seed.

Canvis introduits:

- el contingut mestre va passar a estar definit per la BDD;
- es van fixar els ids canonicament per biomes, enemics, cartes, modificadors i poders;
- es va definir l'estat persistent del `local-player`;
- es van definir les taules de run i tracking de progressio;
- es va establir una font de veritat unica per a contingut i seed del vertical slice.

Objectiu d'aquesta fase:

- evitar valors hardcoded dispersos;
- garantir coherencia entre contingut, backend i joc local;
- fer possible una capa local temporal i una futura substitucio per API.

## Fase 6. Refactoritzacio data-driven local-first

Amb el balanc i l'SQL definits, es va fer la refactoritzacio mes gran del projecte. La idea va ser adaptar el joc local a l'estructura real de dades, mantenint un mode funcional sense dependencia inmediata d'un backend viu.

Canvis introduits:

- es van crear DTOs i models per representar el seed necessari dins Unity;
- es van crear repositoris locals per contingut, progressio i run;
- es va crear un `SegmentGenerator` basat en cartes, pools i modificadors;
- es va separar la IA de l'heroi amb `HeroAIController`;
- es va centralitzar el combat a `CombatSystem`;
- es va separar economia, botiga i poders divins en sistemes independents;
- es va reescriure `RunManager` per orquestrar la run en base a repositoris i serveis;
- es van crear seeds JSON locals equivalents al contingut del seed SQL;
- es va crear una UI minima desacoblada de la logica principal.

Objectiu d'aquesta fase:

- deixar el joc preparat per treballar per sistemes;
- mantenir el vertical slice jugable en local;
- fer que el pas a backend real sigui una substitucio de repositori, no una reescriptura de la logica.

## Documents del projecte i per a que serveixen

### Documents principals

- `Docs/BddFinal.sql`
  - SQL definitiu de la base de dades.
  - Defineix l'estructura de dades, els ids i el seed inicial del vertical slice.
  - Es la font de veritat del contingut.

- `Docs/MVP_VERTICAL_SLICE_BALANCE.md`
  - Document de balanc del MVP vertical slice.
  - Defineix stats, economia, botiga, poders, enemics, corba de dificultat i metaprogrés.
  - Serveix per guiar la implementacio del joc i les proves de playtesting.

- `Docs/MakingOf.md`
  - Aquest document.
  - Serveix com a cronologia del desenvolupament, mapa de fitxers, seguiment documental i guia de connexio a Unity.

### Documents de suport presents a `Docs`

- `Docs/UNITY_SETUP_AND_RUN_GUIDE.md`
  - Guia de configuracio i execucio del projecte.
  - Serveix per arrencar el joc, revisar dependències i entendre el flux basic de posada en marxa.

- `Docs/PROJECT_DEFENSE_GUIDE.md`
  - Document de suport per a explicacio del projecte.
  - Serveix per defensar les decisions del projecte i entendre'n el marc general.

## Documents i dades generades per alimentar el joc

A mes de la documentacio de `Docs`, hi ha fitxers de dades que el joc carrega per funcionar en mode local.

- `Assets/Resources/Seeds/vertical_slice_content.json`
  - Seed local del contingut mestre.
  - Conté biomes, enemics, modificadors, relíquies, consumibles, poders divins, cartes i pools.
  - Respecta els ids de la BDD.

- `Assets/Resources/Seeds/local_player_seed.json`
  - Seed local del jugador persistent `local-player`.
  - Conté el progrés inicial, cartes desbloquejades, poders desbloquejats i consumibles.

## Arquitectura actual del codi

L'arquitectura actual s'ha organitzat per responsabilitats, per evitar scripts monolitics i per separar dades, logica de joc i presentacio.

### Estructura general dins `Assets/Scripts`

- `Assets/Scripts/Core`
  - Constants de balanc i models base.

- `Assets/Scripts/Data`
  - Repositoris i interfícies per carregar contingut, progressio i dades de run.

- `Assets/Scripts/Systems`
  - Sistemes de joc desacoblats: combat, IA, economia, botiga, poders, generacio de segment.

- `Assets/Scripts/UI`
  - UI minima del vertical slice basada en panells IMGUI.

- `Assets/Scripts`
  - Actors principals i scripts de runtime vinculats directament a escena.

## Llista de fitxers de codi i dades que aporten al joc

### Arrel de `Assets/Scripts`

- `Assets/Scripts/RunManager.cs`
  - Orquestrador global de la run.
  - Coordina repositoris, sistemes, generacio de segment, recompenses, botiga i estat general.

- `Assets/Scripts/WorldGrid.cs`
  - Gestiona el grid del segment actiu.
  - Renderitza les cel·les i col·loca murs, enemics i el goal.

- `Assets/Scripts/PlayerGridMovement.cs`
  - Actor del player.
  - Gestiona posicio, moviment, vida, stats i buffs aplicats.

- `Assets/Scripts/Enemy.cs`
  - Actor base dels enemics.
  - Guarda stats runtime, recompensa i comportament dany/mort.

- `Assets/Scripts/EnemyGridMovement.cs`
  - IA de moviment dels enemics.
  - Dona suport al melee i al comportament a distancia del `ghost_elite`.

- `Assets/Scripts/GoalTile.cs`
  - Objectiu del segment.

- `Assets/Scripts/ProceduralPlayerRenderer.cs`
  - Renderer procedural del player.
  - Genera la figura multicolor per punts i l'anima quan es mou.

- `Assets/Scripts/ProceduralEnemyRenderer.cs`
  - Renderer procedural dels enemics.
  - Dona suport a `skeleton`, `bat`, `zombie` i `ghost_elite`.

- `Assets/Scripts/ProceduralEnvironmentFactory.cs`
  - Fabrica visual procedural del terra i els obstacles.
  - Defineix paletes per bioma i la construccio de mosaics, ruines i arbres.

- `Assets/Scripts/ProceduralPixelUtility.cs`
  - Utilitats comunes per crear pixels i colors procedurals.

- `Assets/Scripts/MapCardData.cs`
  - Estructura antiga basada en `ScriptableObject`.
  - Es manté per compatibilitat i utilitats de transicio.

- `Assets/Scripts/RemoteGameApiClient.cs`
  - Client remot existent per a integracio futura amb API.
  - No es la via principal del mode local actual.

- `Assets/Scripts/WorldModifier.cs`
  - Script auxiliar per proves i modificacio directa del grid.

### `Assets/Scripts/Core/Balance`

- `Assets/Scripts/Core/Balance/BalanceConfig.cs`
  - Constants centrals de balanc.
  - Defineix stats base, corba de dificultat, llindars de combat i regles de botiga.

### `Assets/Scripts/Core/Models`

- `Assets/Scripts/Core/Models/ContentModels.cs`
  - DTOs serialitzables per biomes, enemics, modificadors, relíquies, consumibles, poders i cartes.

- `Assets/Scripts/Core/Models/ProgressionModels.cs`
  - DTOs de perfil i progressio persistent del jugador.

- `Assets/Scripts/Core/Models/RunModels.cs`
  - DTOs runtime de run, segment, eleccions, enemics, recompenses i esdeveniments.

- `Assets/Scripts/Core/Models/RuntimeContentModels.cs`
  - Models runtime per configuracions parsejades, resultats de generacio i ofertes de botiga.

- `Assets/Scripts/Core/Models/LegacyRemoteCompatibility.cs`
  - Capa de compatibilitat amb l'antic client remot.

### `Assets/Scripts/Data`

- `Assets/Scripts/Data/RepositoryInterfaces.cs`
  - Interfícies de repositori.
  - Defineix els contractes per contingut, progressio i run.

- `Assets/Scripts/Data/LocalRepositories.cs`
  - Implementacions locals dels repositoris.
  - Carrega seeds JSON, desa progressio persistent i guarda runs en memoria.

### `Assets/Scripts/Systems`

- `Assets/Scripts/Systems/SegmentGenerator.cs`
  - Genera segments a partir de la carta seleccionada.
  - Aplica modificadors, construeix murs i decideix la pool d'enemics.

- `Assets/Scripts/Systems/HeroAIController.cs`
  - IA de l'heroi.
  - Avalua risc, decideix si combatre, evitar o fugir i avanca cap al Goal.

- `Assets/Scripts/Systems/CombatSystem.cs`
  - Sistema de combat centralitzat.
  - Calcula dany minim, simulacions i resolucio de combats.

- `Assets/Scripts/Systems/EconomySystem.cs`
  - Gestiona or de run, esmeraldes i consumibles.

- `Assets/Scripts/Systems/ShopSystem.cs`
  - Genera ofertes de botiga i aplica compres.

- `Assets/Scripts/Systems/DivinePowerSystem.cs`
  - Gestiona poders divins equipats, cooldowns i efectes temporals.

- `Assets/Scripts/Systems/JsonSeedParser.cs`
  - Parser dels fragments JSON dins els seeds locals.

- `Assets/Scripts/Systems/WeightedSelectionUtility.cs`
  - Utilitat per seleccio ponderada de contingut.

### `Assets/Scripts/UI`

- `Assets/Scripts/UI/RunHudController.cs`
  - Controlador general de la UI minima.

- `Assets/Scripts/UI/HeroHudPanel.cs`
  - Panell HUD amb vida, or, estat i poders divins.

- `Assets/Scripts/UI/CardChoiceOverlayPanel.cs`
  - Panell de seleccio de cartes.

- `Assets/Scripts/UI/ShopOverlayPanel.cs`
  - Panell de botiga.

- `Assets/Scripts/UI/RunSummaryOverlayPanel.cs`
  - Panell final de run.

### `Assets/Resources/Seeds`

- `Assets/Resources/Seeds/vertical_slice_content.json`
  - Contingut mestre local alineat amb la BDD.

- `Assets/Resources/Seeds/local_player_seed.json`
  - Estat inicial del jugador local.

## Canvis importants que s'han introduit al joc

### Canvis visuals

- el player ha deixat de ser un unic pixel i ara es una figura procedural multicolor;
- els enemics tenen identitat visual propia;
- el terra i els obstacles tenen paletes i patrons per bioma;
- el joc segueix una estetica per punts, pero mes definida i amb mes lectura visual.

### Canvis de contingut

- s'han consolidat els biomes `ruins`, `dark_forest`, `swamp` i `crypt`;
- s'han consolidat els enemics `skeleton`, `bat`, `zombie` i `ghost_elite`;
- el contingut de cartes, pools i recompenses ve ara del seed local alineat amb la BDD;
- `local-player` te inicialment nomes `speed_blessing` desbloquejat.

### Canvis de gameplay

- l'heroi te 30 de vida base, 5 d'atac, 1 de defensa i velocitat 1.0;
- el combat utilitza la formula de dany minim definida al document de balanc;
- la run treballa per segments i cartes de bioma;
- l'heroi ja no pren decisions dins del mateix script de moviment, sino mitjancant un sistema d'IA separat;
- la botiga apareix segons la longitud de run;
- les recompenses i el tracking de run queden preparats per al model de dades real.

## Que s'ha de fer a Unity perque tot funcioni correctament

Aquest punt es important. Part del codi ja esta preparat, pero per deixar l'escena totalment alineada amb l'arquitectura nova cal revisar o afegir alguns components.

### GameObjects necessaris a `MainScene`

- `GameManager`
  - Ha de tenir `RunManager`.
  - Ha de tenir assignades les referencies a `WorldGrid`, `PlayerGridMovement` i `fallbackEnemyTemplate`.
  - Es recomanable assignar tambe la referencia a `HeroAIController` si el camp apareix a l'Inspector.

- `Player`
  - Ha de tenir `PlayerGridMovement`.
  - Ha de tenir `ProceduralPlayerRenderer`.
  - Ha de tenir `HeroAIController`.

- objecte del mon
  - Ha de tenir `WorldGrid`.

- objecte del goal
  - Ha de tenir `GoalTile`.

- `RunHud`
  - Es recomana crear un GameObject buit amb `RunHudController`.
  - No cal `Canvas` per a la versio actual, perque la UI es IMGUI.
  - S'hi ha d'assignar la referencia al `RunManager`.

### Assignacions recomanades a Inspector

- a `RunManager`
  - `worldGrid` -> objecte amb `WorldGrid`;
  - `player` -> objecte del player amb `PlayerGridMovement`;
  - `heroAi` -> component `HeroAIController` del player;
  - `fallbackEnemyTemplate` -> prefab o plantilla base per instanciar enemics procedurals.

- a `RunHudController`
  - `runManager` -> objecte `GameManager` amb `RunManager`.

### Observacions importants sobre la migracio de scripts

Despres de recompilar a Unity, alguns camps antics poden desapareixer del Inspector perque els scripts s'han redissenyat.

Aixo es esperat en casos com:

- `PlayerGridMovement`, que abans exposava camps locals de vida, atac i velocitat i ara treballa des de dades de run i balanc centralitzat;
- `RunManager`, que abans tenia camps antics de biblioteca de cartes i UI embeguda i ara treballa amb repositoris i sistemes;
- `WorldGrid`, que ja no genera segments des de `MapCardData` antic, sino des del `SegmentGenerator`.

La recomanacio es deixar que Unity recompili, tornar a entrar a l'escena i revisar els components nous o els camps reasignables.

## Relacio entre documentacio i implementacio

Per fer seguiment del projecte, aquesta es la relacio recomanada entre documents i codi:

- `BddFinal.sql`
  - defineix els ids i la forma de les dades;
- `MVP_VERTICAL_SLICE_BALANCE.md`
  - defineix valors, intencio de disseny i corba de dificultat;
- `vertical_slice_content.json` i `local_player_seed.json`
  - tradueixen la part necessaria del seed SQL a una forma carregable per Unity en local;
- scripts de `Core/Models`
  - representen aquestes dades dins el runtime de Unity;
- scripts de `Data`
  - carreguen i guarden aquestes dades;
- scripts de `Systems`
  - apliquen les regles de joc a aquestes dades;
- scripts principals de runtime
  - connecten el resultat a escena i a presentacio.

## Estat actual i seguents passos recomanats

Estat actual:

- el projecte te una base visual procedural consolidada;
- existeix document de balanc del vertical slice;
- existeix SQL definitiu del model de dades;
- el joc local ja te arquitectura modular orientada a repositoris i serveis;
- el seed local i el progres inicial estan alineats amb la BDD;
- la UI minima existeix i permet jugar el loop principal localment.

Seguents passos recomanats:

- revisar `MainScene` i afegir `HeroAIController` i `RunHudController` si encara no hi son;
- provar el loop complet dins Unity i validar les transicions de segment, la botiga i els poders;
- decidir si el client remot s'ha d'adaptar al model nou o es deixa com a fase posterior;
- afegir persistencia mes completa de runs si es vol reflectir encara mes el model `run_sessions` i taules relacionades;
- continuar refinant la IA de l'heroi i el comportament dels enemics segons playtesting.

## Conclusio

El desenvolupament d'Architectus Fati ha passat d'un prototip funcional molt simple a una base molt mes solida, modular i alineada amb una font de dades real. La clau de l'evolucio ha estat passar de prototip local i visuals minims a un sistema orientat a dades, documentat i preparat per creixer.

Aquest `MakingOf` s'ha de mantenir viu. Cada cop que s'introdueixi una nova capa important al projecte, la recomanacio es actualitzar aquest document amb:

- que s'ha fet;
- per que s'ha fet;
- quins fitxers o documents s'han afegit o modificat;
- com impacta a Unity i a l'arquitectura general.

## Actualitzacio posterior. Sistema de cofres al mapa

S'ha afegit una nova capa de contingut al segment: els cofres generats sobre el mapa. Aquesta funcionalitat respon a la necessitat de fer que el recorregut del player tingui punts d'interes intermedis, afegir microdecisions al moviment automatic i reforcar l'economia de run amb una font visible de recompensa.

Objectiu d'aquesta iteracio:

- fer que el mapa no sigui nomes un cami entre entrada, enemics i sortida;
- afegir recompenses visibles dins el segment;
- lligar la recompensa de cofres a la corba de dificultat del vertical slice;
- mantenir una implementacio coherent amb l'estetica procedural per punts.

### Canvis de disseny introduits

Els cofres segueixen aquestes regles de disseny:

- apareixen directament dins del segment com a objectes del mapa;
- la frequencia augmenta a mesura que avancen els segments;
- les cartes `risky` i `elite` tenen tendencia a oferir una mica mes d'oportunitat de cofre;
- la recompensa principal dels cofres es l'or;
- les esmeraldes poden apareixer, pero de manera rara;
- el player no obre cofres des d'un boto ni per UI, sino automaticament quan arriba a la seva casella.

Aquesta decisio encaixa amb la fantasia del joc: el jugador segueix sense controlar directament el personatge, pero pot influir indirectament en el tipus de segment que genera mes oportunitats o mes risc.

### Canvis de gameplay introduits

Els canvis funcionals que s'han afegit son aquests:

- el `SegmentGenerator` ja no genera nomes murs i enemics, sino tambe cofres;
- el nombre de cofres es calcula en funcio del segment actual i del tipus de carta;
- el valor del cofre s'ajusta al tram de dificultat;
- el player pot considerar un cofre com a objectiu secundari abans del `Goal`;
- la IA del player nomes es desvia si el context sembla relativament segur;
- quan el player entra a la casella del cofre, aquest s'obre automaticament;
- l'obertura dona la recompensa immediatament i queda registrada a la run.

A nivell de comportament, aquesta capa introdueix una petita tensio interessant:

- en segments tranquils, l'heroi pot recollir cofres sense massa risc;
- en segments amb pressio o enemies elites propers, la IA prioritzara la supervivencia i la sortida;
- d'aquesta manera, el cofre es converteix en oportunitat, no en recompensa garantida.

### Relacio amb la corba de dificultat

La generacio de cofres s'ha lligat explicitament a la corba de dificultat:

- segments inicials: probabilitat mes baixa i cofres mes modestos;
- segments intermedis: augmenta la frequencia i el valor mitja de l'or;
- segments avancats: es possible trobar mes cofres o cofres mes valuosos;
- cartes arriscades: augment lleu de frequencia i recompensa potencial;
- probabilitat d'esmeralda: baixa en general i una mica superior en trams i cartes de mes risc.

Aquesta logica no substitueix les recompenses abstractes de carta, sino que les complementa amb una capa fisica dins del mapa.

### Arquitectura i implementacio tecnica

La funcionalitat s'ha integrat seguint la mateixa arquitectura modular del projecte.

#### Capa de models runtime

S'han afegit nous models a `RuntimeContentModels` per representar cofres generats dins del segment:

- `ChestRewardRuntimeData`
- `GeneratedChestSpawnData`
- ampliacio de `SegmentRuntimeData` amb `chestSpawns`

Aixo permet que la generacio del segment entregui no nomes murs i enemics, sino tambe punts de recompensa del mapa.

#### Capa de balanc

El fitxer `BalanceConfig` s'ha ampliat amb regles especifiques per a cofres:

- calcul del nombre de cofres per segment;
- probabilitat de recompensa rara amb esmeraldes;
- quantitat d'or escalada segons segment i tipus de carta.

D'aquesta manera, els valors no queden dispersos i es poden ajustar des d'un unic lloc.

#### Capa de generacio de segment

`SegmentGenerator` ara fa tres grans passos de contingut:

- genera murs;
- genera enemics;
- genera cofres.

La generacio dels cofres:

- evita posicions ocupades per murs;
- evita posicions ocupades per enemics;
- evita l'entrada i la sortida del segment;
- classifica el cofre com `small`, `medium` o `rare` segons el valor de la recompensa.

#### Capa de runtime de mapa

`WorldGrid` ara manté tambe el registre de cofres vius del segment:

- registra cofres;
- els elimina quan s'obren;
- permet consultar cofres per posicio;
- permet consultar cofres propers per a la IA del player.

#### Capa de IA de l'heroi

`HeroAIController` s'ha ampliat per introduir un nou objectiu tactit:

- si hi ha un cofre proper i la situacio no sembla gaire perillosa, l'heroi el pot prioritzar temporalment;
- si hi ha massa pressio o un elite a prop, el sistema torna a prioritzar supervivencia i objectiu principal.

Aixo permet una conducta mes rica sense perdre la filosofia de control indirecte.

#### Capa de recompenses i run tracking

`RunManager` ara processa l'obertura dels cofres:

- atorga or immediat;
- atorga esmeraldes en els casos rars;
- escriu l'esdeveniment d'obertura del cofre al tracking de run;
- afegeix les recompenses corresponents al repositori de run;
- mostra missatge de feedback al HUD.

### Estetica visual introduida

Els cofres no utilitzen assets externs. S'ha creat una representacio visual procedural propia:

- `Chest.cs` com a entitat runtime;
- `ProceduralChestRenderer.cs` com a renderer per punts;
- variants visuals per estat tancat i obert;
- color lleugerament diferenciat segons tier del cofre.

A nivell artistic, aquesta capa manté coherencia amb la resta del projecte:

- pixel art procedural;
- sense sprites externs;
- lectura clara dins del grid;
- objectes petits pero recognoscibles.

### Fitxers afegits o modificats per aquesta iteracio

#### Fitxers nous

- `Assets/Scripts/Chest.cs`
  - defineix l'entitat cofre al runtime.

- `Assets/Scripts/ProceduralChestRenderer.cs`
  - genera la representacio visual del cofre en estat tancat o obert.

#### Fitxers ampliats

- `Assets/Scripts/Core/Models/RuntimeContentModels.cs`
  - afegeix estructures runtime per cofres i les vincula al segment.

- `Assets/Scripts/Core/Balance/BalanceConfig.cs`
  - incorpora funcions per calcular frequencia, valor i probabilitat rara dels cofres.

- `Assets/Scripts/Systems/SegmentGenerator.cs`
  - genera cofres i les seves recompenses dins del segment.

- `Assets/Scripts/Systems/HeroAIController.cs`
  - permet al player prioritzar cofres propers si el risc es assumible.

- `Assets/Scripts/WorldGrid.cs`
  - registra, instancia i exposa cofres del segment actiu.

- `Assets/Scripts/RunManager.cs`
  - processa l'obertura i aplica les recompenses.

### Impacte dins Unity

L'impacte a escena es lleuger, perque la implementacio continua sent procedural i runtime.

No cal afegir manualment:

- prefabs de cofres;
- objectes de cofre dins `MainScene`;
- triggers o UI especifica per obrir cofres.

El que si passa automaticament es:

- el segment genera cofres al runtime;
- `WorldGrid` els instancia;
- el player els pot obrir quan hi arriba;
- el renderer procedural els dibuixa sense assets externs.

A futur, si es vol, es podria afegir:

- un prefab base opcional de cofre;
- animacio d'obertura mes rica;
- efectes de particules o feedback visual extra;
- variants de cofres per bioma.

Pero per al MVP actual no es necessari.

### Valor afegit d'aquesta funcionalitat

La introduccio dels cofres aporta diverses millores al vertical slice:

- fa que el segment tingui mes lectura i mes vida;
- reforca la fantasia de run amb risc-recompensa;
- dona una font visible d'or dins del mapa;
- introdueix una font secundaria i rara d'esmeraldes;
- afegeix comportament nou a la IA del player sense complexificar els controls del jugador.

En resum, aquesta iteracio amplia el loop del joc amb un element petit pero important: el mapa ja no nomes es travessar i sobreviure, sino tambe detectar i aprofitar oportunitats.

## Revisio i ampliacio del document

Aquesta seccio complementa el `MakingOf` anterior amb elements que s'han afegit en iteracions posteriors i que convé deixar reflectits de manera explicita per mantenir la traçabilitat del projecte.

### Elements visuals i funcionals que s'han d'entendre com a part de l'estat actual

A mes del que ja s'ha descrit a les fases anteriors, l'estat actual del projecte inclou aquestes millores importants:

- la meta de cada segment ja no es nomes una marca funcional, sino una porta procedural que canvia de caràcter segons el bioma;
- els cofres tenen dues representacions visuals diferenciades, estat tancat i estat obert;
- el `Player` ha augmentat la seva definicio visual amb una figura de mes pixels i una paleta multicolor mes rica;
- els enemics s'han tornat a dibuixar per evitar silhouettes massa semblants i reforçar la lectura de `skeleton`, `bat`, `zombie` i `ghost_elite`;
- la UI de run ja no es nomes una capa tecnica minima, sino una primera passada visual amb panells mes grans, badges i millor jerarquia de lectura.

### Fase afegida. Refinament visual del vertical slice

Aquesta fase se situa despres de la consolidacio del loop data-driven i despres de la incorporacio dels cofres al mapa.

Objectiu principal:

- augmentar la llegibilitat del joc sense tocar la seva logica base;
- fer que els objectes del mapa siguin recognoscibles d'un cop d'ull;
- millorar la presentacio de cartes, stats i botiga perquè el vertical slice es percebi menys com un prototip intern.

Canvis destacats d'aquesta fase:

- creacio de `ProceduralGoalRenderer` per convertir el goal en una porta procedural;
- redisseny de `ProceduralChestRenderer` per distingir clarament cofre tancat i obert;
- nova passada a `ProceduralPlayerRenderer` per donar mes detall i colors al personatge;
- nova passada a `ProceduralEnemyRenderer` per fer mes diferents entre si els enemics;
- creacio de `RunUiTheme` per donar coherencia visual a la UI IMGUI;
- ampliacio dels panells HUD, cartes, botiga i resum final de run.

### Fitxers que convé considerar afegits o especialment rellevants en l'estat actual

Aquest bloc complementa la llista principal del document i destaca fitxers que aporten valor directe a la versio jugable actual.

- `Assets/Scripts/Chest.cs`
  - defineix l'entitat runtime del cofre al mapa.

- `Assets/Scripts/ProceduralChestRenderer.cs`
  - genera la representacio visual del cofre en estat tancat i obert.

- `Assets/Scripts/ProceduralGoalRenderer.cs`
  - genera la representacio visual del goal com a porta procedural.

- `Assets/Scripts/UI/RunUiTheme.cs`
  - centralitza colors, estils i components visuals de la UI IMGUI.

### Aclariments addicionals sobre Unity

Per a l'estat actual del vertical slice, convé tenir presents aquestes regles practiques:

- no cal posar cofres manualment a `MainScene`, perquè es generen proceduralment en runtime;
- no cal crear un prefab separat per a cada enemic, perquè la variacio visual ve donada pel renderer procedural i les dades del seed;
- el `Goal` pot mantenir-se com a objecte simple amb `GoalTile`, perquè el renderer procedural es pot afegir automaticament;
- la UI actual no necessita `Canvas`, perquè es dibuixa amb IMGUI;
- si mes endavant es vol una UI mes de produccio, el cami recomanat es migrar el contingut de `RunManager` cap a un `Canvas` amb panells separats per HUD, cartes, botiga i resum.

### Proposta de migracio futura a Canvas

Aquesta part no es obligatoria per al MVP actual, pero es important deixar-la documentada.

Estructura recomanada si es fa el pas a `Canvas`:

- `Canvas/RunHudRoot/HeroPanel`;
- `Canvas/RunHudRoot/CardChoicePanel`;
- `Canvas/RunHudRoot/ShopPanel`;
- `Canvas/RunHudRoot/RunSummaryPanel`.

Dades que aquests panells haurien de llegir des del runtime:

- `RunManager.CurrentCardChoices`;
- `RunManager.CurrentShopOffers`;
- `RunManager.CurrentGold` i `RunManager.CurrentEmeralds`;
- `RunManager.CurrentSegment`;
- `RunManager.Player`.

Això permetria substituir la presentacio sense tocar la logica de run, que es precisament un dels objectius de l'arquitectura actual.

### Valor documental d'aquesta revisio

Aquesta ampliacio serveix per deixar clar que el projecte ja no esta nomes en una fase funcional, sino en una fase de refinament del vertical slice. La logica base, la font de veritat de dades i la capa visual ja treballen conjuntament, i el `MakingOf` ha de reflectir aquest salt de qualitat.

## Actualitzacio posterior. Consolidacio del seed final i sincronitzacio SQL-Unity

En una iteracio posterior s'ha fet una passada de consolidacio sobre el contingut mestre del projecte. Aquesta fase no s'ha centrat tant en afegir noves mecaniques, sino en garantir que la font de veritat SQL i el seed local carregat per Unity quedin perfectament alineats.

### Problema detectat

El fitxer `Docs/BddFinal.sql` contenia, al final, un bloc d'expansio antic que no respectava l'esquema real de la base de dades. Aquest bloc utilitzava noms de columnes que no existien a les taules definitives i fins i tot feia referencia a estructures que no formen part del model actual.

Aixo generava tres riscos:

- tenir una BDD aparentment completa pero no executable de punta a punta;
- mantenir ids antics o inconsistents respecte al runtime actual;
- desalinear el SQL amb els seeds JSON que Unity fa servir de veritat en mode local.

### Objectiu d'aquesta iteracio

- deixar `BddFinal.sql` coherent i usable com a seed principal del projecte;
- sincronitzar els ids de relíquies, consumibles, poders divins i recompenses amb el joc actual;
- fer que el seed SQL i els fitxers JSON locals descriguin el mateix contingut;
- preparar una base de dades i un cataleg de contingut mes propers a una versio final del vertical slice.

### Canvis aplicats a `Docs/BddFinal.sql`

El fitxer SQL s'ha revisat i ampliat perquè reflecteixi una seed completa coherent amb el joc actual.

Canvis principals:

- s'ha eliminat el bloc antic incompatible del final del fitxer;
- s'han consolidat els poders divins `speed_blessing`, `strength_blessing`, `shield_blessing`, `command_attack`, `command_escape` i `summon_companion`;
- s'han consolidat les relíquies `relic_iron_heart`, `relic_warrior_spirit`, `relic_swift_boots`, `relic_golden_touch` i `relic_shadow_instinct`;
- s'han consolidat els consumibles `healing_potion`, `speed_dose`, `smoke_bomb` i `guard_token`;
- s'han revisat les recompenses de cartes per donar coherencia al progrés de desbloqueig i a les recompenses del vertical slice;
- s'ha mantingut `local-player` amb `speed_blessing` com a unic poder inicial desbloquejat, tal com marca el disseny actual.

### Canvis aplicats als seeds locals de Unity

Com que el joc no consumeix directament l'SQL en runtime, no n'hi havia prou amb arreglar la base de dades. Tambe s'han actualitzat els fitxers locals que l'arquitectura actual carrega des de `Resources`.

Fitxers sincronitzats:

- `Assets/Resources/Seeds/vertical_slice_content.json`
  - ara reflecteix el mateix cataleg principal de biomes, enemics, modificadors, relíquies, consumibles, poders i recompenses que el SQL;
  - s'han netejat ids antics que ja no encaixaven amb el model actual.

- `Assets/Resources/Seeds/local_player_seed.json`
  - s'han actualitzat els consumibles inicials del `local-player` per utilitzar els ids nous i coherents amb el cataleg actual.

### Impacte arquitectonic

Aquesta iteracio reforca una idea important del projecte: la BDD i els seeds locals no son peces independents, sino dues representacions del mateix model de contingut.

La relacio correcta queda aixi:

- `Docs/BddFinal.sql`
  - defineix l'esquema i la seed mestra de referencia;
- `Assets/Resources/Seeds/vertical_slice_content.json`
  - tradueix el contingut necessari a una forma carregable per Unity en local;
- `Assets/Resources/Seeds/local_player_seed.json`
  - defineix l'estat inicial del jugador local per al vertical slice;
- repositoris locals i DTOs
  - consumeixen aquests fitxers sense dependre d'un backend viu.

### Estat funcional despres d'aquesta consolidacio

Despres d'aquesta passada, el projecte queda millor preparat com a vertical slice complet:

- el cataleg de contingut te identitats mes consistents;
- el SQL i els JSON locals deixen de competir entre ells;
- el joc te una seed local mes neta i mes coherent amb la documentacio;
- les recompenses, poders i peces de progrés tenen una base mes clara per continuar ampliant contingut.

### Limitacions conegudes

Tot i que el seed ja inclou relíquies com `relic_golden_touch` o `relic_shadow_instinct`, no tots els seus efectes especials estan encara connectats al runtime actual.

A nivell de documentacio, cal deixar clar que hi ha dos estats diferents:

- contingut definit i seedat correctament;
- contingut totalment aplicat a la logica del joc.

Ara mateix, alguns efectes especials ja existeixen com a dades, pero encara requereixen integracio a codi per impactar de manera real en el comportament de cofres o de la IA.

### Valor afegit d'aquesta iteracio

Aquesta fase te molt valor documental i tecnic encara que no sigui una millora visual directa:

- evita incoherencies futures entre base de dades i Unity;
- deixa el projecte mes preparat per un backend real;
- facilita proves, manteniment i defensa tecnica del projecte;
- ajuda a considerar `BddFinal.sql` com una base seriosa de contingut i no nomes com un esborrany de model.

## Actualitzacio posterior. Integracio de l artwork de cartes amb `art_key`

En una iteracio posterior s'ha refet de manera completa el fitxer `Docs/BddFinal.sql` per deixar-lo preparat per reinicialitzar la base de dades des de zero i per resoldre d'una manera clara la gestio visual de les cartes.

La decisio aplicada ha estat unica i consistent: la base de dades no guarda ni binaris ni URLs externes de les imatges de carta. En lloc d'aixo, cada carta guarda una clau interna de recurs anomenada `art_key`, pensada perque Unity carregui un `Sprite` local des de `Resources`.

### Canvi estructural aplicat a la BDD

La taula on viu aquesta dada es:

- `cards`

El camp introduit i consolidat es:

- `art_key text not null default ''`

Aquesta clau representa el nom intern del recurs visual de la carta. Alguns exemples reals del projecte son:

- `card_ruins_abandoned`
- `card_ruins_unstable`
- `card_forest_dense`
- `card_swamp_deep`
- `card_crypt_profaned`

Aquesta decisio millora tres coses al mateix temps:

- manté la BDD lleugera i centrada en dades de gameplay;
- evita dependencias externes per carregar art;
- permet que Unity resolgui l'art localment amb una ruta estable i molt simple.

### Impacte sobre l esquema de contingut

Aquesta revisio del SQL no s'ha limitat a afegir `art_key`. Tambe ha deixat el model general millor tancat per a la versio actual del joc.

Entre les taules principals que han quedat consolidades hi ha:

- `biomes`
- `enemy_archetypes`
- `world_modifier_definitions`
- `relic_definitions`
- `consumable_definitions`
- `divine_power_definitions`
- `shop_offer_definitions`
- `cards`
- `card_enemy_pool`
- `card_modifier_pool`
- `card_reward_pool`
- `chest_reward_pool`
- `players`
- `player_progress`
- `player_card_unlocks`
- `player_divine_power_unlocks`
- `player_relics`
- `player_consumables`
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

A nivell documental, aquesta passada es important perquè transforma `BddFinal.sql` en una base de dades molt mes executable, mes defensable i mes alineada amb el vertical slice real.

### Canvis aplicats a Unity per consumir `art_key`

Perque aquesta decisio de BDD tingui efecte real al joc, tambe s'han fet canvis a la capa local de Unity.

Fitxers afectats:

- `Assets/Scripts/Core/Models/ContentModels.cs`
  - s'ha afegit el camp `artKey` a `CardSeedData`.

- `Assets/Resources/Seeds/vertical_slice_content.json`
  - totes les cartes inclouen ara el seu `artKey`.

- `Assets/Scripts/UI/CardChoiceOverlayPanel.cs`
  - el panell de cartes carrega l'art des de `Resources/CardArt/{artKey}`;
  - si no troba el `Sprite`, mostra un fallback visual amb el nom de la clau.

- `Assets/Resources/CardArt/`
  - s'ha creat la carpeta recomanada per guardar els sprites de les cartes.

### Flux de dades que queda establert

El flux correcte, tal com queda documentat i implementat, es aquest:

- BDD SQL
  - la carta guarda `art_key` a la taula `cards`.
- seed local JSON
  - el contingut exportat o replicat per Unity manté aquest valor com `artKey`.
- model C#
  - `CardSeedData` exposa `artKey`.
- UI runtime
  - la vista de carta fa `Resources.Load<Sprite>($"CardArt/{artKey}")`.
- presentacio final
  - el `Sprite` es mostra dins la targeta visual de seleccio.

Aquesta cadena es molt important perquè deixa una ruta neta i sense ambiguitats entre dades i presentacio.

### Que s ha de fer manualment a Unity

Perque el sistema funcioni del tot, encara hi ha una part manual molt concreta que ha de fer l'equip a Unity:

- col.locar cada imatge de carta dins `Assets/Resources/CardArt/`;
- assegurar que cada fitxer estigui importat com a `Sprite (2D and UI)`;
- fer que el nom del fitxer coincideixi exactament amb el valor de `artKey`.

Exemples:

- `Assets/Resources/CardArt/card_ruins_abandoned.png`
- `Assets/Resources/CardArt/card_forest_dense.png`
- `Assets/Resources/CardArt/card_crypt_profaned.png`

Si el nom no coincideix exactament, la carta no podra carregar l'art i es veura el fallback textual.

### Valor afegit d aquesta integracio

Aquesta fase aporta molt mes del que sembla a primera vista:

- fa que la BDD sigui mes propera a una base de dades de produccio orientada a contingut;
- evita acoblar la UI a noms hardcoded dispersos;
- deixa preparada una futura API sense haver de redissenyar la gestio d'imatges;
- permet continuar treballant en local amb el mateix model conceptual que tindra el backend real.

Documentalment, es un pas important perquè tanca la connexio entre contingut de dades, recursos visuals i presentacio jugable.

### Llista final de cartes i `artKey` per preparar els sprites

Aquesta llista serveix com a check-list directa per exportar o nomenar correctament les imatges locals de les cartes.

- `abandoned_path` -> `card_ruins_abandoned`
- `unstable_ruins` -> `card_ruins_unstable`
- `fallen_fortress` -> `card_ruins_fallen_fortress`
- `dark_clearing` -> `card_forest_dark_clearing`
- `dense_forest` -> `card_forest_dense`
- `cursed_forest` -> `card_forest_cursed`
- `calm_marsh` -> `card_swamp_calm`
- `stagnant_waters` -> `card_swamp_stagnant`
- `deep_swamp` -> `card_swamp_deep`
- `silent_crypt` -> `card_crypt_silent`
- `ancient_corridors` -> `card_crypt_ancient_corridors`
- `profaned_crypt` -> `card_crypt_profaned`

La correspondencia practica dins Unity ha de quedar aixi:

- `Assets/Resources/CardArt/card_ruins_abandoned.png`
- `Assets/Resources/CardArt/card_ruins_unstable.png`
- `Assets/Resources/CardArt/card_ruins_fallen_fortress.png`
- `Assets/Resources/CardArt/card_forest_dark_clearing.png`
- `Assets/Resources/CardArt/card_forest_dense.png`
- `Assets/Resources/CardArt/card_forest_cursed.png`
- `Assets/Resources/CardArt/card_swamp_calm.png`
- `Assets/Resources/CardArt/card_swamp_stagnant.png`
- `Assets/Resources/CardArt/card_swamp_deep.png`
- `Assets/Resources/CardArt/card_crypt_silent.png`
- `Assets/Resources/CardArt/card_crypt_ancient_corridors.png`
- `Assets/Resources/CardArt/card_crypt_profaned.png`

## Actualitzacio posterior. Preparacio del HUD amb Canvas dins Unity

En una iteracio posterior s'ha fet una passada orientada a migrar progressivament la UI del vertical slice des del sistema actual basat en IMGUI cap a una estructura de `Canvas` propia de Unity.

L'objectiu d'aquesta fase no ha estat redissenyar la logica del joc, sino desacoblar la presentacio del runtime i deixar preparada una base clara per construir una UI mes neta, escalable i visualment mes cuidada.

### Problema detectat

Amb la UI dibuixada des de `OnGUI`, hi havia diversos limits practics:

- panells massa compactes per a la quantitat d'informacio actual;
- textos que es podien tallar o quedar menys clars segons resolucio;
- dificultat per controlar visualment ancoratges, marges i jerarquia de lectura;
- mes dificultat per fer una presentacio semblant a una interfície de joc acabada.

A mes, a mesura que s'han afegit poders divins, cartes amb artwork, botiga, resum de run i feedback de combat, la capa IMGUI ha deixat de ser el millor format per continuar refinant la UX.

### Objectiu d'aquesta iteracio

- preparar una capa HUD basada en `Canvas` sense tocar la logica central de `RunManager`;
- mantenir el runtime actual com a font de veritat;
- permetre apagar la UI IMGUI antiga sense perdre funcionalitat;
- definir una jerarquia clara d'objectes de Unity per al HUD, cartes, botiga i resum;
- mantenir tambe la barra de vida i feedback de combat del player en format compatible amb `Canvas`.

### Canvis aplicats al codi

S'han creat nous scripts de presentacio dins de `Assets/Scripts/UI/Canvas`.

Scripts principals afegits:

- `Assets/Scripts/UI/Canvas/RunCanvasHudController.cs`
  - actua com a coordinador general del HUD de `Canvas`;
  - llegeix l'estat de `RunManager` i refresca els diferents panells;
  - pot desactivar automaticament el HUD IMGUI antic a traves de `RunHudController.SetShowUi(false)`.

- `Assets/Scripts/UI/Canvas/HeroHudCanvasPanel.cs`
  - mostra l'estat de la run, economia, mode de l'heroi, vida, stats, segment i feedback.

- `Assets/Scripts/UI/Canvas/DivinePowersCanvasPanel.cs`
  - mostra el panell de poders divins com a bloc de `Canvas`.

- `Assets/Scripts/UI/Canvas/DivinePowerCanvasSlot.cs`
  - representa cada slot de poder divi amb titol, descripcio, carregues, cooldown i boto.

- `Assets/Scripts/UI/Canvas/CardChoiceCanvasPanel.cs`
  - controla la visibilitat del panell de seleccio de cartes.

- `Assets/Scripts/UI/Canvas/CardChoiceCanvasSlot.cs`
  - representa cada carta seleccionable en UI i carrega el `Sprite` a partir de `artKey`.

- `Assets/Scripts/UI/Canvas/ShopCanvasPanel.cs`
  - controla la presentacio de la botiga entre segments.

- `Assets/Scripts/UI/Canvas/ShopOfferCanvasSlot.cs`
  - representa cada oferta de botiga en format visual de `Canvas`.

- `Assets/Scripts/UI/Canvas/RunSummaryCanvasPanel.cs`
  - mostra el resum de run completada o fallida.

- `Assets/Scripts/UI/Canvas/PlayerWorldCanvasPanel.cs`
  - mostra la barra de vida del player i el feedback de combat en overlay de `Canvas`, posicionat a partir del `WorldToScreenPoint`.

- `Assets/Scripts/UI/Canvas/CardArtSpriteCache.cs`
  - encapsula la carrega i cache dels sprites de cartes des de `Resources/CardArt`.

### Ajustos de compatibilitat

Per fer possible una transicio neta entre el sistema antic i el nou, tambe s'ha retocat:

- `Assets/Scripts/UI/RunHudController.cs`
  - s'hi ha afegit un control explicit de visibilitat amb `SetShowUi(bool)`;
  - aixo permet mantenir l'IMGUI disponible mentre es munta el `Canvas`, i apagar-lo quan el HUD nou ja estigui connectat.

Aquesta decisio te valor tecnic i de produccio, perquè evita una migracio destructiva i permet provar el HUD nou de manera incremental.

### Relacio amb la feina previa de HUD i UX

Aquesta fase no substitueix la passada anterior de millora d'UX, sino que la continua.

Abans d'aquesta migracio, ja s'havien introduit:

- panells laterals mes grans i mes clars per heroi i poders divins;
- centrat automatic del mapa;
- barra de vida del player sobre el cap;
- registre del darrer combat per mostrar dany infligit i rebut;
- millora d'estils per evitar canvis de color no desitjats al passar el punter.

La preparacio del `Canvas` aprofita tota aquesta logica i la trasllada a un model visual mes robust.

### Objectes de Unity que ara formen part de la documentacio recomanada

A nivell d'escena, la documentacio del projecte ha passat a recomanar una estructura semblant a aquesta:

- `Canvas_HUD`
- `RunCanvasRoot`
- `HeroHudPanel`
- `DivinePowersPanel`
- `CardChoicePanel`
- `ShopPanel`
- `RunSummaryPanel`
- `PlayerWorldPanel`
- `EventSystem`

Aquesta jerarquia no altera les peces de gameplay (`GameManager`, `World`, `Player`, `Goal`), sino que afegeix una capa de presentacio separada i controlada.

### Valor documental d'aquesta fase

Aquesta iteracio es important dins del `MakingOf` perquè marca el moment en que el projecte deixa de dependre exclusivament d'una UI provisional i comenca a preparar-se per una interfície real de vertical slice.

Aporta tres beneficis molt clars:

- deixa mes neta la separacio entre dades, logica i vista;
- facilita una presentacio mes professional del joc a Unity;
- permet evolucionar el look de la UI sense reescriure la logica de la run.

En resum, aquesta fase consolida el pas des d'un HUD intern funcional cap a una arquitectura de presentacio mes propera a produccio.

## Actualitzacio posterior. Generalitzacio del sistema `art_key` per a HUD, botiga i meta-progres

En una passada posterior s'ha ampliat el model de dades perquè el sistema de referencies visuals no quedi limitat a les cartes de bioma. La necessitat detectada era clara: si el joc ha de mostrar poders divins, buffs temporals, ofertes de botiga, consumibles, relíquies i pantalles de final de run amb una identitat visual coherent, la BDD havia de poder referenciar aquestes imatges amb el mateix patró que ja s'havia aplicat a les cartes.

### Problema detectat

Fins aquest moment, el projecte tenia el concepte de `art_key` ben resolt per a `cards`, pero la resta d'elements HUD encara depenien de textos, presentacio local o futures decisions obertes.

Aixo generava un problema de coherencia de model:

- les cartes ja estaven preparades per carregar artwork local des de la BDD;
- els poders divins i la botiga no tenien encara una referencia equivalent dins l'esquema;
- el futur arbre de tecnologies del menu principal necessitara reutilitzar icones o il.lustracions dels mateixos poders i millores;
- la pantalla final de run no tenia una entitat clara on referenciar una imatge de resum.

### Decisio aplicada a la BDD

S'ha estandarditzat el mateix concepte de `art_key` per a totes les peces de contingut que han d'alimentar HUD, seleccio o meta-progres.

El principi aplicat es el mateix en tots els casos:

- la BDD no guarda binaris;
- la BDD no guarda URLs externes;
- la BDD guarda una clau interna `art_key`;
- Unity resol el `Sprite` local a partir d'aquesta clau.

### Taules ampliades a `Docs/BddFinal.sql`

S'han afegit camps `art_key` a les definicions que tenen valor visual directe:

- `cards`
- `world_modifier_definitions`
- `relic_definitions`
- `consumable_definitions`
- `divine_power_definitions`
- `shop_offer_definitions`

A mes, s'ha afegit una nova taula:

- `run_result_definitions`

Aquesta taula permet definir les imatges de resum per a:

- run completada;
- run fallida;
- run abandonada o variants futures.

### Valor de disseny d'aquesta ampliacio

Aquesta decisio te molt valor perquè resol tres fronts al mateix temps:

- HUD in-game: poders divins, ofertes, buffs, recompenses i resum final poden mostrar artwork propi;
- menu principal i meta-progres: el mateix `art_key` dels poders o millores es pot reutilitzar a l'arbre de tecnologies;
- manteniment de contingut: es centralitza la referencia visual a la mateixa capa de dades on ja viuen nom, descripcio i efecte.

### Exemples de claus visuals que ara queden documentades

La documentacio SQL ja contempla exemples de claus per a diferents families de contingut:

- poders divins: `divine_speed_blessing`, `divine_command_attack`, `divine_summon_companion`
- ofertes de botiga: `shop_heal_small`, `shop_buff_strength`, `shop_temporary_equipment`
- consumibles: `consumable_healing_potion`, `consumable_smoke_bomb`
- relíquies: `relic_iron_heart`, `relic_shadow_instinct`
- modificadors: `mod_safe_paths`, `mod_blood_mist`
- final de run: `run_result_victory`, `run_result_failure`, `run_result_abandoned`

### Que queda pendent a nivell de runtime

Aquesta fase deixa el model i la documentacio preparats, pero cal deixar clar que no tota la cadena de runtime s'ha connectat encara per a aquests nous `art_key`.

Ara mateix, el projecte ja consumeix artwork de cartes. El seguent pas logic sera fer el mateix per:

- panell de poders divins en `Canvas`;
- ofertes de botiga en `Canvas`;
- resum final de run;
- futur arbre de tecnologies del menu principal.

### Següent pas recomanat a Unity i al codi

Perque aquesta ampliacio acabi impactant la build jugable, els passos recomanats son:

- ampliar els DTOs i seeds locals perquè exposin `artKey` tambe per poders divins, consumibles, relíquies, modificadors i ofertes de botiga;
- deixar de tenir la botiga definida nomes com a cataleg hardcoded si es vol que vingui completament del model de dades;
- afegir `Image` a les targetes i slots del `Canvas` per a poders divins, botiga i resum de run;
- guardar els sprites corresponents dins `Assets/Resources` seguint una nomenclatura estable;
- reutilitzar exactament els mateixos `artKey` quan es construeixi l'arbre de tecnologies del menu principal.

### Valor documental d'aquesta fase

Aquesta actualitzacio es important dins del `MakingOf` perquè amplia el sistema d'artwork des d'un cas puntual de cartes cap a una politica general de contingut visual del projecte.

Això fa que `BddFinal.sql` deixi de ser nomes una BDD de gameplay i passi a ser tambe una font de veritat per a la identitat visual de la UI i del meta-joc.

## Actualitzacio posterior. Preparacio final del sistema d'art del HUD per configurar-lo a Unity

En aquesta passada s'ha acabat de tancar el pont entre la BDD, els seeds locals i la UI de `Canvas` perque l'equip pugui muntar el HUD visual directament dins Unity sense haver de modificar la logica de joc.

### Quina part del model s'ha consolidat

La politica de `art_key` ja no es limita a `cards`. Ara el model i els seeds locals contemplen tambe artwork per a:

- `divine_power_definitions`
- `shop_offer_definitions`
- `run_result_definitions`
- `world_modifier_definitions`
- `relic_definitions`
- `consumable_definitions`

Aixo vol dir que una mateixa clau visual pot alimentar:

- el HUD in-game;
- la botiga;
- el resum final de run;
- i, mes endavant, l'arbre de tecnologies o meta-progres del menu principal.

### Canvis aplicats al runtime de Unity

S'han completat aquestes peces perque el projecte quedi realment preparat per a la configuracio dins Unity:

- `Assets/Scripts/Core/Models/ContentModels.cs`
  - ara defineix `artKey` per a modificadors, relíquies, consumibles i poders divins;
  - incorpora tambe `ShopOfferSeedData` i `RunResultSeedData`.

- `Assets/Scripts/Core/Models/RuntimeContentModels.cs`
  - amplia el model runtime d'oferta de botiga amb `artKey`, `rewardType`, `durationSegments` i `effectConfigJson`.

- `Assets/Scripts/Data/RepositoryInterfaces.cs`
  - el repositori de contingut ja exposa `GetShopOffers()` i `GetRunResults()`.

- `Assets/Scripts/Data/LocalRepositories.cs`
  - el seed local carrega ara ofertes de botiga i resultats de run des de JSON.

- `Assets/Scripts/Systems/ShopSystem.cs`
  - la botiga pot construir el seu cataleg des del seed local coherent amb la BDD.

- `Assets/Scripts/RunManager.cs`
  - ja resol el `artKey` del resultat actual de run per al panell de resum.

- `Assets/Scripts/UI/Canvas/CardArtSpriteCache.cs`
  - deixa de ser nomes una cache de cartes;
  - ara busca sprites a diverses carpetes de `Resources` segons la familia de contingut.

- `Assets/Scripts/UI/Canvas/DivinePowerCanvasSlot.cs`
  - cada slot de poder divi ja pot tenir `Image` propi per artwork.

- `Assets/Scripts/UI/Canvas/ShopOfferCanvasSlot.cs`
  - cada oferta de botiga ja pot tenir `Image` propi per artwork.

- `Assets/Scripts/UI/Canvas/RunSummaryCanvasPanel.cs`
  - el resum final ja pot mostrar la imatge definida a `run_result_definitions`.

- `Assets/Resources/Seeds/vertical_slice_content.json`
  - s'ha sincronitzat amb la BDD i ara inclou `artKey` per poders, relíquies, consumibles i modificadors;
  - s'hi han afegit tambe `shopOffers` i `runResults`.

### Carpetes de recursos que s'han deixat preparades

Per facilitar el muntatge dins Unity, s'han deixat creades aquestes carpetes sota `Assets/Resources`:

- `Assets/Resources/CardArt`
- `Assets/Resources/DivinePowerArt`
- `Assets/Resources/ShopArt`
- `Assets/Resources/RelicArt`
- `Assets/Resources/ConsumableArt`
- `Assets/Resources/ModifierArt`
- `Assets/Resources/RunResultArt`
- `Assets/Resources/ContentArt`

La carpeta `ContentArt` queda com a fallback per si en algun moment es vol compartir una mateixa il.lustracio entre diferents sistemes sense duplicar-la.

### Que s'ha de fer ara a Unity per muntar-ho

A nivell de `Canvas`, el que falta ja es purament de configuracio visual a escena.

Per a `DivinePowersPanel`:

- dins cada `PowerSlot`, crea un fill `ArtworkImage` amb component `Image`;
- assigna aquest `Image` al camp `artworkImage` del component `DivinePowerCanvasSlot`.

Per a `ShopPanel`:

- dins cada `ShopSlot`, crea un fill `ArtworkImage` amb component `Image`;
- assigna aquest `Image` al camp `artworkImage` del component `ShopOfferCanvasSlot`.

Per a `RunSummaryPanel`:

- crea un `Image` gran a la capcalera o al costat esquerre del resum;
- assigna'l al camp `artworkImage` de `RunSummaryCanvasPanel`.

Despres, importa els sprites com a `Sprite (2D and UI)` i posa'ls a la carpeta que toqui fent que el nom del fitxer coincideixi exactament amb el `artKey`.

### Families de claus visuals que ara pots preparar

Exemples de claus que ja estan alineades entre BDD i seed local:

- poders divins:
  - `divine_speed_blessing`
  - `divine_strength_blessing`
  - `divine_shield_blessing`
  - `divine_command_attack`
  - `divine_command_escape`
  - `divine_summon_companion`

- botiga i buffs temporals:
  - `shop_heal_small`
  - `shop_heal_large`
  - `shop_buff_strength`
  - `shop_buff_speed`
  - `shop_buff_shield`
  - `shop_reroll_cards`
  - `shop_summon_companion`
  - `shop_temporary_equipment`

- final de run:
  - `run_result_victory`
  - `run_result_failure`
  - `run_result_abandoned`

- relíquies:
  - `relic_iron_heart`
  - `relic_warrior_spirit`
  - `relic_swift_boots`
  - `relic_golden_touch`
  - `relic_shadow_instinct`

- consumibles:
  - `consumable_healing_potion`
  - `consumable_speed_dose`
  - `consumable_smoke_bomb`
  - `consumable_guard_token`

- modificadors:
  - `mod_safe_paths`
  - `mod_dense_growth`
  - `mod_stagnant_mud`
  - `mod_healing_shrine`
  - `mod_blood_mist`
  - `mod_elite_presence`

### Impacte sobre el futur arbre de tecnologies

Aquesta preparacio es important perque evita haver de duplicar el cataleg visual quan es construeixi el menu principal.

La norma recomanada queda aixi:

- si el node del tech tree representa un poder divi, reutilitza el mateix `artKey` de `divine_power_definitions`;
- si representa una relíquia o una millora tipus item, reutilitza el mateix `artKey` de la seva definicio de contingut;
- la UI del menu principal hauria de carregar l'art exactament amb el mateix criteri que el HUD in-game.

### Valor documental d'aquesta fase

Aquesta fase es rellevant dins del `MakingOf` perque marca el moment en que el projecte deixa preparada una cadena completa de contingut visual:

- la BDD defineix la clau visual;
- el seed local replica la mateixa clau;
- el model C# l'exposa com `artKey`;
- el `Canvas` pot pintar-la des de `Resources`;
- Unity nomes necessita que l'equip importi i assigni els `Image` del layout.

En resum, el joc queda preparat per una configuracio visual mes neta dins Unity, mantenint la BDD com a font de veritat del contingut.

## Actualitzacio posterior. Congelacio del temps dels poders divins en pantalles de decisio

En una passada posterior s'ha ajustat el comportament temporal dels poders divins perque el temps intern de gameplay no continui avancant mentre el jugador esta prenent decisions fora de l'exploracio activa del segment.

### Problema detectat

Fins aquest moment, el sistema de poders divins continuava actualitzant:

- el cooldown de recuperacio de carregues;
- i la durada activa dels buffs temporals;

encara que el joc estigues en un estat de pausa de decisio, com ara:

- seleccio de cartes;
- botiga entre segments;
- resum de run completada o fallida.

Aixo generava una incoherencia de disseny, perque el jugador podia perdre segons d'efecte o de recarrega mentre no hi havia exploracio real ni control de joc sobre el mapa.

### Decisio aplicada

S'ha establert que el temps dels poders divins nomes avanci durant l'estat:

- `ExploringSegment`

Per tant, quan la run entra en qualsevol estat de decisio o de transicio no interactiva per al mapa, el sistema queda congelat.

### Canvi aplicat al codi

S'ha retocat:

- `Assets/Scripts/RunManager.cs`

Concretament, la crida a `divinePowerSystem.Tick(Time.deltaTime, player)` ara nomes s'executa mentre `currentState == RunState.ExploringSegment`.

### Impacte funcional

Despres d'aquest canvi:

- el cooldown dels poders no baixa mentre s'escullen cartes;
- la durada activa d'un buff no es consumeix mentre el jugador esta a la botiga;
- el resum final de run no continua gastant temps intern dels poders;
- la lectura visual del `cooldownFill` passa a ser mes coherent amb el temps real de joc.

### Valor d'aquesta iteracio

Aquesta correccio es petita a nivell de codi, pero important a nivell d'experiencia:

- fa que el temps dels poders respongui al temps jugable real;
- evita perdre valor dels buffs durant pantalles de decisio;
- i deixa mes clara la separacio entre temps de combat/exploracio i temps d'interficie.

## Actualitzacio posterior. Refinament del HUD de canvas i alineacio automatica del fons del mon

En una iteracio posterior s'ha fet una passada de qualitat sobre la capa visual de joc per resoldre dues friccions molt concretes del vertical slice:

- la llegibilitat del `HeroHud` en format `Canvas`;
- i la col.locacio del fons del mapa dins l'espai de mon.

### Problemes detectats

Per una banda, el `HeroHudCanvasPanel` mostrava diversos valors numerics sense etiqueta explicita.

A la practica, el jugador podia veure nombres com:

- `5`
- `1`
- `1.00`

sense saber immediatament si corresponien a atac, defensa o velocitat.

Per altra banda, el `WorldBackground` s'havia comencat a preparar com a objecte visual del mon, pero la seva posicio i mida podien quedar desalineades respecte del grid real del segment. Aixo passava especialment quan es configurava manualment des de l'Inspector amb coordenades o escales que no tenien relacio directa amb:

- `Width`
- `Height`
- `cellSize`

### Decisio aplicada

S'ha optat per consolidar dues regles de presentacio:

- els textos del `HeroHudCanvasPanel` han de mostrar abreviatures o context semantic directament dins el text;
- el fons del mon s'ha de calcular per codi a partir de la geometria real del segment, i no dependre de valors manuals persistits a escena.

### Canvis aplicats al HUD

S'ha retocat:

- `Assets/Scripts/UI/Canvas/HeroHudCanvasPanel.cs`

Els valors que abans sortien "nus" ara es mostren amb context:

- `Mode: ...`
- `HP current/max`
- `Atk. valor`
- `Def. valor`
- `Vel. valor`

Aquesta decisio no canvia el sistema de stats, pero si la seva lectura immediata en pantalla. Es un pas petit, pero important, perque el Canvas deixi de semblar una capa provisional i comenci a comportar-se com una UI de vertical slice real.

### Canvis aplicats al HUD del mon

Tambe ha quedat documentat i consolidat el paper de:

- `Assets/Scripts/UI/Canvas/PlayerWorldCanvasPanel.cs`
- `Assets/Scripts/UI/Canvas/RunCanvasHudController.cs`

El projecte ja te una base clara per al panell flotant que segueix l'heroi sobre el mapa:

- transforma la posicio del player a coordenades de pantalla;
- aplica un `screenOffset`;
- mostra vida i text de combat recent;
- i nomes es veu durant `ExploringSegment`.

Aquesta capa complementa el `HeroHud` principal i representa el pas de l'antic overlay immediat cap a una UI de Canvas mes estructurada.

### Canvis aplicats al fons del mon

S'ha retocat:

- `Assets/Scripts/WorldGrid.cs`

Amb aquest canvi, `WorldGrid` assumeix la responsabilitat de buscar i actualitzar `WorldBackground`.

La logica nova fa el seguent:

- localitza el `SpriteRenderer` del fons, si existeix;
- el centra amb la mateixa formula que la camera i el segment;
- forca `localScale = 1`;
- activa `SpriteDrawMode.Tiled`;
- calcula `size` a partir de `Width * cellSize` i `Height * cellSize`;
- i fixa el `sortingOrder` a un valor de darrere del mapa.

En termes practics, el fons deixa de dependre d'una posicio accidental tipus "pantalla" i passa a estar ancorat a la mida real del segment generat.

### Impacte funcional i visual

Despres d'aquesta passada:

- el jugador llegeix millor les estadistiques sense haver d'interpretar numeros descontextualitzats;
- el HUD de canvas queda una mica mes a prop d'un format final i no d'una prova temporal;
- el fons del mon acompanya la camera i el segment d'una manera coherent;
- i la configuracio manual dins Unity es redueix a assignar l'objecte i el sprite correctes, no a reajustar la seva geometria cada vegada.

### Valor d'aquesta iteracio

Aquesta iteracio es important dins del `MakingOf` perque no introdueix una gran mecanica nova, pero si reforca una idea clau del projecte:

- la logica de joc ha d'alimentar directament la presentacio visual;
- la UI ha de parlar amb llenguatge clar per al jugador;
- i els elements visuals del mon han de dependre del sistema procedural, no de valors arbitraris d'escena.

Es, en definitiva, una fase de poliment estructural: menys espectacular que afegir una mecanica nova, pero molt rellevant per fer que el vertical slice es percebi com una experiencia mes coherent i mes professional.

## Actualitzacio posterior. Refinament del pixel art procedural de personatges, moviment i entorn

En una iteracio posterior s'ha fet una passada de qualitat centrada en la lectura visual del joc moment a moment. La necessitat era clara: el loop ja funcionava, pero el conjunt encara podia guanyar molta mes presencia si l'heroi, els enemics i el mapa es movien i es representaven amb mes intencio.

### Objectiu d'aquesta iteracio

Els objectius principals d'aquesta fase han estat:

- fer que l'heroi tingui una silueta mes reconeixible i mes heroica;
- donar als enemics una identitat pixel-art mes marcada;
- suavitzar el moviment perque no sembli una translacio rigida entre caselles;
- reforcar el modelat de l'entorn perque el segment tingui relleu, atmosfera i personalitat per bioma.

### Millores aplicades al player

S'ha retocat:

- `Assets/Scripts/ProceduralPlayerRenderer.cs`

La figura procedural del player s'ha enriquit en diversos fronts:

- s'ha redefinit la silueta amb mes detall;
- s'hi ha afegit contorn i ombra;
- la lectura visual de l'arma queda molt mes clara i l'heroi ja sembla portar una espasa;
- el renderer ara pot invertir-se segons la direccio horitzontal del moviment;
- s'hi ha afegit una respiracio idle, balanceig i una petita inclinacio en desplacament.

El resultat es que el personatge principal deixa de semblar nomes una icona funcional i comenca a transmetre presencia, direccio i intencio.

### Millores aplicades als enemics

S'ha retocat:

- `Assets/Scripts/ProceduralEnemyRenderer.cs`

Els enemics procedurals s'han refinat per donar-los millor lectura en pantalla:

- s'han redibuixat les formes de `skeleton`, `bat`, `zombie` i `ghost_elite`;
- s'han afegit mes capes de color, contrast, ombra i accents;
- els enemics ara es poden orientar segons el sentit del moviment;
- les animacions visuals de pas, flotacio o aleteig s'han fet mes expressives.

Aquesta passada reforca la diferenciacio entre tipus d'enemic i fa mes clara la lectura del camp de joc.

### Millores aplicades al moviment

S'han retocat:

- `Assets/Scripts/PlayerGridMovement.cs`
- `Assets/Scripts/EnemyGridMovement.cs`

El moviment ja no es limita a un `MoveTowards` lineal pur. Ara s'hi ha introduit:

- una interpolacio mes suau d'inici i final de passa;
- un arc de moviment que fa que el desplacament tingui pes visual;
- una durada minima configurable per evitar sensacions brusques;
- una arribada mes neta a la casella objectiu.

En termes de sensacio de joc, aquesta decisio fa que tant l'heroi com els enemics semblin actors dins del mon i no peces que simplement canvien de coordenada.

### Millores aplicades a l'entorn procedural

S'han retocat:

- `Assets/Scripts/ProceduralEnvironmentFactory.cs`
- `Assets/Scripts/WorldGrid.cs`

La capa visual de l'entorn ha rebut una ampliacio important:

- cada casella del terra te ara una base mes rica i mes modulada;
- s'han afegit ombres de vora i relleu segons les parets adjacents;
- el terra pot mostrar esquerdes, pedres, vegetacio, taques o tolls segons el bioma;
- els arbres i les ruines tenen una silueta mes treballada;
- els murs es refresquen junt amb les caselles veines per mantenir la coherencia visual;
- el `WorldBackground` es tenyeix segons la paleta del segment per reforcar l'atmosfera general.

Amb aixo, l'entorn deixa de ser nomes un suport funcional per al gameplay i passa a participar activament en la direccio artistica del vertical slice.

### Valor d'aquesta fase dins del projecte

Aquesta iteracio es rellevant dins del `MakingOf` perque consolida un pas important: el projecte no nomes funciona, sino que comenca a presentar-se amb una capa visual coherent entre moviment, personatges i biomes.

Els beneficis principals d'aquesta passada son:

- mes llegibilitat visual en exploracio i combat;
- millor identitat del player i dels enemics;
- una sensacio de moviment mes polida;
- i un entorn mes creible dins del llenguatge procedural del projecte.

En resum, aquesta fase no afegeix una mecanica nova, pero si eleva molt la percepcio de qualitat del vertical slice i deixa la base preparada per futures capes de poliment artistic.

## Actualitzacio posterior. Implementacio del menu principal i de l'arbre de millores

En una fase posterior s'ha abordat una necessitat que ja apareixia de manera recurrent a la documentacio del projecte: el vertical slice necessitava deixar de començar directament dins la run i passar a tenir una capa d'entrada de meta-joc.

La decisio presa ha estat implementar una primera versio funcional del:

- menu principal;
- hub de seleccio de preferencies de partida;
- i pantalla d'arbre de millores.

La part visual del layout continua pensada per muntar-se manualment des de Unity, pero el flux, les dades i els scripts ja queden preparats.

### Relacio directa amb la BDD i el model de progres

Aquesta implementacio s'ha alineat explicitament amb la BDD i amb el model local-first del projecte. Els scripts del menu es recolzen en:

- `players`
- `player_progress`
- `player_card_unlocks`
- `player_divine_power_unlocks`
- `biomes`
- `cards`
- `divine_power_definitions`

Aixo es important perquè evita que el menu sigui una UI separada del joc real. El hub principal llegeix el mateix progres persistent que fa servir la run.

### Scripts nous i scripts ampliats

S'han afegit:

- `Assets/Scripts/UI/Canvas/MainMenuCanvasController.cs`
- `Assets/Scripts/UI/Canvas/MainMenuHomeCanvasPanel.cs`
- `Assets/Scripts/UI/Canvas/TechTreeCanvasPanel.cs`
- `Assets/Scripts/UI/Canvas/TechTreeNodeCanvasSlot.cs`

I s'han ampliat:

- `Assets/Scripts/RunManager.cs`
- `Assets/Scripts/Systems/EconomySystem.cs`

### Paper de cada peça

#### `MainMenuCanvasController`

Actua com a coordinador del menu:

- obre el hub en arrencar;
- bloqueja l'`autoStart` del `RunManager` mentre el menu esta visible;
- permet navegar entre pantalla principal i arbre de millores;
- i llança la run quan el jugador prem `Play`.

#### `MainMenuHomeCanvasPanel`

Mostra el resum del perfil actiu:

- nom del jugador;
- esmeraldes;
- runs completades i fallides;
- segment maxim assolit;
- resum de cartes, poders i biomes desbloquejats;
- longitud de run seleccionada;
- i mode inicial de l'heroi.

També deixa preparada la seleccio de:

- run curta o llarga;
- `prudent`
- `aggressive`
- `escape`

#### `TechTreeCanvasPanel`

Construeix una primera versio funcional de l'arbre de millores a partir de dades reals del projecte. Actualment contempla nodes per a:

- run llarga;
- biomes;
- poders divins;
- cartes.

#### `TechTreeNodeCanvasSlot`

Representa cada node individual i separa clarament:

- branca o categoria;
- titol i descripcio;
- cost;
- estat;
- artwork;
- i boto d'accio.

### Decisio important sobre costos i desbloquejos

No totes les peces del model actual tenen un cost de desbloqueig definit a SQL.

Per aquest motiu s'ha pres una decisio de transicio:

- els poders divins fan servir el `unlockCost` real del seed;
- la run llarga es tracta com una millora meta fixa;
- els biomes fan servir un cost simple per categoria;
- les cartes fan servir un cost derivat de `baseDifficulty` i `rewardTier`.

Aquesta decisio no pretén tancar definitivament el balanc del meta-joc, sino permetre que la pantalla de millores funcioni ja amb dades consistents mentre el model continua evolucionant.

### Impacte sobre l'arquitectura del projecte

L'entrada al joc deixa de dependre exclusivament de l'escena de gameplay.

Ara el projecte queda preparat per un flux mes propi d'un joc amb meta-progres:

1. obrir joc;
2. entrar al menu principal;
3. revisar progres i preferencies;
4. desbloquejar millores;
5. llançar la run.

En paral.lel, el `RunManager` continua sent l'orquestrador de la run, pero ja no ha d'imposar que la partida comenci immediatament.

### Documentacio complementaria

Per facilitar el muntatge manual a Unity, aquesta fase queda reforcada amb instruccions concretes a:

- `Docs/MAIN_MENU_PROFILE_AND_PROGRESS_PLAN.md`

Alli s'hi descriu:

- quins GameObjects cal crear al `Canvas`;
- quins components s'han d'afegir;
- quins camps s'han d'assignar a Inspector;
- i quina jerarquia es recomana per al prefab dels nodes de l'arbre.

### Ajustos posteriors durant el muntatge real a Unity

Una vegada traslladada aquesta primera versio funcional a l'editor de Unity, s'han fet dos ajustos importants que formen part del resultat final i que convé deixar documentats.

El primer ha estat de tipus tecnic: durant la connexio real del `Canvas_MainMenu` s'ha detectat un error de compilacio a `MainMenuCanvasController`, provocat per una referencia a `FirstOrDefault` sense l'espai de noms necessari. La incidència s'ha corregit afegint `System.Linq`, de manera que el component ja es pot assignar correctament des de l'Inspector.

El segon ajust ha estat de tipus d'usabilitat visual dins de l'arbre de millores. En la primera versio, quan un node no es podia comprar, el cost podia arribar a mostrar-se com `--`. Despres de provar-ho en context real, s'ha decidit que el cost s'ha de veure sempre, i que la diferència entre una millora disponible o no disponible s'ha d'expressar nomes a traves de l'estat del boto i del node.

Aixo dona un resultat molt mes clar:

- el jugador veu sempre quantes esmeraldes costa cada millora;
- pot distingir millor entre "no la tinc desbloquejada" i "no em arriba encara el pressupost";
- i l'arbre de tecnologies transmet millor la progressio economica del meta-joc.

### Valor d'aquesta fase dins del vertical slice

Aquesta iteracio es molt rellevant perquè introdueix una capa de producte que el projecte ja necessitava:

- dona context al progres persistent;
- visibilitza les esmeraldes i els desbloquejos;
- converteix el meta-joc en una peça jugable i no nomes documental;
- i deixa preparada la base per futures pantalles de perfil, poders, opcions o seleccio de perfils.

En resum, aquesta fase marca el pas des d'un vertical slice centrat exclusivament en el loop de run cap a una estructura mes completa, on el joc ja te entrada, hub i progressio entre partides.
