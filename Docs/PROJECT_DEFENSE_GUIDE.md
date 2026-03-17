# ArchitectusFati - Guia De Defensa Del Projecte

## 1. Objectiu Del Projecte

`ArchitectusFati` es un videojoc 2D fet amb Unity on l'heroi avanca per segments generats a partir de cartes de mapa. Cada segment te unes propietats concretes:

- mida del mapa
- bioma visual
- probabilitat d'obstacles
- probabilitat d'enemics
- recompenses i progressio

La idea principal del projecte es separar clarament:

- la logica del joc dins de Unity
- la persistencia de dades en una base de dades PostgreSQL a Neon
- una API backend que fa d'intermediaria segura entre Unity i Neon

Aquest disseny evita exposar la `connection string` de la base de dades dins del client de joc i fa possible que el joc escali, guardi progressos, centralitzi contingut i pugui evolucionar a un producte mes complet.

## 2. Arquitectura General

L'arquitectura es divideix en tres capes:

### Capa 1. Client de joc

Implementada a Unity, dins de `Assets/Scripts`.

Responsabilitats:

- generar i mostrar segments
- moure el jugador i els enemics
- gestionar combat i final de run
- consumir l'API remota quan esta activa
- mantenir un fallback local si el backend no esta disponible

### Capa 2. API backend

Implementada amb ASP.NET Core, dins de `Backend/ArchitectusFati.Api`.

Responsabilitats:

- rebre peticions HTTP de Unity
- validar i transformar dades
- parlar amb PostgreSQL/Neon
- servir contingut del joc
- guardar i recuperar progres
- iniciar, actualitzar i tancar runs

### Capa 3. Base de dades

Implementada a Neon Serverless Postgres.

Responsabilitats:

- emmagatzemar contingut mestre del joc
- guardar progres persistent
- registrar sessions de run
- mantenir historial de segments, recompenses i events

## 3. Flux De Dades

Flux funcional:

1. Unity arrenca.
2. `RunManager` decideix si treballa en mode local o remot.
3. Si el mode remot esta activat, Unity crida l'API.
4. L'API consulta Neon.
5. Neon retorna cartes, progressio o estat de run.
6. L'API retorna JSON a Unity.
7. Unity genera el segment i continua la partida.

Aquest flux es important per defensar el projecte, perque demostra:

- separacio de responsabilitats
- seguretat
- capacitat d'escalar
- persistencia real de dades

## 4. Estructura Del Projecte

### 4.1 Carpeta Unity

Fitxers propis del joc:

- [Enemy.cs](/e:/Dev/AFati/ArchitectusFati/Assets/Scripts/Enemy.cs)
- [EnemyGridMovement.cs](/e:/Dev/AFati/ArchitectusFati/Assets/Scripts/EnemyGridMovement.cs)
- [GoalTile.cs](/e:/Dev/AFati/ArchitectusFati/Assets/Scripts/GoalTile.cs)
- [MapCardData.cs](/e:/Dev/AFati/ArchitectusFati/Assets/Scripts/MapCardData.cs)
- [PlayerGridMovement.cs](/e:/Dev/AFati/ArchitectusFati/Assets/Scripts/PlayerGridMovement.cs)
- [RemoteGameApiClient.cs](/e:/Dev/AFati/ArchitectusFati/Assets/Scripts/RemoteGameApiClient.cs)
- [RunManager.cs](/e:/Dev/AFati/ArchitectusFati/Assets/Scripts/RunManager.cs)
- [WorldGrid.cs](/e:/Dev/AFati/ArchitectusFati/Assets/Scripts/WorldGrid.cs)
- [WorldModifier.cs](/e:/Dev/AFati/ArchitectusFati/Assets/Scripts/WorldModifier.cs)

Carpetes de Unity no explicades fitxer a fitxer:

- `Library`
- `Temp`
- `Logs`
- `UserSettings`

Aquestes carpetes son generades per Unity o per l'entorn i no formen part de la logica funcional del treball.

### 4.2 Carpeta Backend

Fitxers principals del backend:

- [Program.cs](/e:/Dev/AFati/ArchitectusFati/Backend/ArchitectusFati.Api/Program.cs)
- [README.md](/e:/Dev/AFati/ArchitectusFati/Backend/ArchitectusFati.Api/README.md)
- [appsettings.json](/e:/Dev/AFati/ArchitectusFati/Backend/ArchitectusFati.Api/appsettings.json)
- [DatabaseOptions.cs](/e:/Dev/AFati/ArchitectusFati/Backend/ArchitectusFati.Api/Configuration/DatabaseOptions.cs)
- [GameContracts.cs](/e:/Dev/AFati/ArchitectusFati/Backend/ArchitectusFati.Api/Contracts/GameContracts.cs)
- [GameRepository.cs](/e:/Dev/AFati/ArchitectusFati/Backend/ArchitectusFati.Api/Data/GameRepository.cs)
- [ContentRepository.cs](/e:/Dev/AFati/ArchitectusFati/Backend/ArchitectusFati.Api/Data/ContentRepository.cs)
- [RunRepository.cs](/e:/Dev/AFati/ArchitectusFati/Backend/ArchitectusFati.Api/Data/RunRepository.cs)
- [DatabaseInitializerHostedService.cs](/e:/Dev/AFati/ArchitectusFati/Backend/ArchitectusFati.Api/Hosting/DatabaseInitializerHostedService.cs)
- [001_init.sql](/e:/Dev/AFati/ArchitectusFati/Backend/ArchitectusFati.Api/Database/001_init.sql)
- [002_full_game_schema.sql](/e:/Dev/AFati/ArchitectusFati/Backend/ArchitectusFati.Api/Database/002_full_game_schema.sql)

Carpetes no funcionals per a la defensa:

- `bin`
- `obj`

Aquestes son generades per la compilacio de .NET.

## 5. Explicacio De Cada Arxiu Del Joc

### [MapCardData.cs](/e:/Dev/AFati/ArchitectusFati/Assets/Scripts/MapCardData.cs)

Es l'arxiu que defineix la dada principal del joc: la carta de mapa.

Contingut:

- identificador de carta
- nom i descripcio
- si comenca desbloquejada
- bioma
- colors visuals
- amplada i alçada del segment
- punts d'entrada i sortida
- probabilitat d'obstacles
- probabilitat d'enemics

També incorpora una `MapCardRuntimeFactory` que:

- genera cartes de fallback en temps d'execucio
- sanititza ids
- construeix cartes a partir de dades remotes de l'API

Paper dins del projecte:

- connecta la capa de contingut amb la capa de generacio del mapa

### [RunManager.cs](/e:/Dev/AFati/ArchitectusFati/Assets/Scripts/RunManager.cs)

Es el cervell de la partida.

Responsabilitats:

- arrencar la run
- carregar cartes
- gestionar el progres local o remot
- controlar els estats de la run
- oferir eleccions de cartes
- desbloquejar recompenses
- sincronitzar el progres amb el backend

Estats principals:

- `Bootstrapping`
- `Transitioning`
- `ExploringSegment`
- `AwaitingCardChoice`
- `Completed`
- `Failed`

Aquest arxiu es clau en la defensa del projecte, perque demostra:

- orquestracio del gameplay
- integracio Unity + API
- tolerancia a fallades amb fallback local

### [RemoteGameApiClient.cs](/e:/Dev/AFati/ArchitectusFati/Assets/Scripts/RemoteGameApiClient.cs)

Es el client HTTP de Unity.

Responsabilitats:

- fer `GET /api/cards`
- carregar el progres del jugador
- enviar el progres actualitzat al backend

Aquest arxiu es important per justificar que Unity no parla directament amb Neon.

### [WorldGrid.cs](/e:/Dev/AFati/ArchitectusFati/Assets/Scripts/WorldGrid.cs)

Genera el mapa del segment en forma de graella.

Responsabilitats:

- crear cel·les visuals
- col·locar murs
- instanciar enemics
- posar la meta
- gestionar pathfinding

Inclou eines auxiliars:

- `GridDirectionUtility`
- `GridPathfinding`

Aquest arxiu mostra la part algoritmica del projecte.

### [PlayerGridMovement.cs](/e:/Dev/AFati/ArchitectusFati/Assets/Scripts/PlayerGridMovement.cs)

Gestiona el moviment del jugador.

Responsabilitats:

- decidir el proper pas
- evitar enemics perillosos
- moure's fins a la meta
- iniciar combat
- perdre vida i fallar la run

Aquest arxiu aporta la logica autonoma de comportament del personatge.

### [Enemy.cs](/e:/Dev/AFati/ArchitectusFati/Assets/Scripts/Enemy.cs)

Model de l'enemic.

Responsabilitats:

- guardar vida i atac
- rebre dany
- morir
- registrar-se o eliminar-se del `WorldGrid`

### [EnemyGridMovement.cs](/e:/Dev/AFati/ArchitectusFati/Assets/Scripts/EnemyGridMovement.cs)

Gestiona el comportament de persecucio dels enemics.

Responsabilitats:

- buscar cami fins al jugador
- moure's per la graella
- col·lisionar amb l'heroi
- forçar combats

### [GoalTile.cs](/e:/Dev/AFati/ArchitectusFati/Assets/Scripts/GoalTile.cs)

Representa la casella objectiu del segment.

Responsabilitat:

- marcar la sortida del segment

### [WorldModifier.cs](/e:/Dev/AFati/ArchitectusFati/Assets/Scripts/WorldModifier.cs)

En l'estat actual es una eina de prova o debug.

Responsabilitats:

- detectar clicks
- afegir o treure murs del mapa

Aquest fitxer pot evolucionar a un sistema real de modificadors ambientals.

## 6. Explicacio Del Backend

### [Program.cs](/e:/Dev/AFati/ArchitectusFati/Backend/ArchitectusFati.Api/Program.cs)

Es el punt d'entrada del backend.

Configura:

- lectura de la connection string de Neon
- `NpgsqlDataSource`
- CORS per permetre comunicacio amb Unity
- injeccio de dependencies
- endpoints HTTP

Endpoints actuals:

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

### [GameContracts.cs](/e:/Dev/AFati/ArchitectusFati/Backend/ArchitectusFati.Api/Contracts/GameContracts.cs)

Defineix tots els DTOs.

Serveix per:

- donar forma als JSON d'entrada i sortida
- unificar el contracte entre backend i client

### [GameRepository.cs](/e:/Dev/AFati/ArchitectusFati/Backend/ArchitectusFati.Api/Data/GameRepository.cs)

Gestiona:

- comprovacio de connexio
- lectura de progres
- actualitzacio de progres

Es la capa de persistencia basica del jugador.

### [ContentRepository.cs](/e:/Dev/AFati/ArchitectusFati/Backend/ArchitectusFati.Api/Data/ContentRepository.cs)

Gestiona el contingut mestre del joc.

Serveix per llegir:

- biomes
- enemics
- modificadors
- reliquies
- consumibles
- cartes

### [RunRepository.cs](/e:/Dev/AFati/ArchitectusFati/Backend/ArchitectusFati.Api/Data/RunRepository.cs)

Gestiona el cicle de vida d'una run.

Responsabilitats:

- iniciar run
- recuperar run activa
- guardar segments
- tancar run
- actualitzar impacte sobre el progres

### [DatabaseInitializerHostedService.cs](/e:/Dev/AFati/ArchitectusFati/Backend/ArchitectusFati.Api/Hosting/DatabaseInitializerHostedService.cs)

Quan el backend arrenca, aquest servei aplica l'script SQL configurat.

Aixo facilita:

- bootstrap automatic
- desplegaments repetibles
- preparacio automatica de l'entorn

### [appsettings.json](/e:/Dev/AFati/ArchitectusFati/Backend/ArchitectusFati.Api/appsettings.json)

Conté configuracio del backend:

- connection strings
- ruta de l'script SQL
- logging

Ara mateix esta configurat per aplicar:

- `Database/002_full_game_schema.sql`

## 7. Base De Dades I Model De Dades

### Script inicial

[001_init.sql](/e:/Dev/AFati/ArchitectusFati/Backend/ArchitectusFati.Api/Database/001_init.sql)

Servia per a una versio simple amb:

- `cards`
- `player_progress`

### Script complet

[002_full_game_schema.sql](/e:/Dev/AFati/ArchitectusFati/Backend/ArchitectusFati.Api/Database/002_full_game_schema.sql)

Es la versio orientada al joc complet.

Taules principals:

- `biomes`
- `enemy_archetypes`
- `world_modifier_definitions`
- `relic_definitions`
- `consumable_definitions`
- `cards`
- `card_enemy_pool`
- `card_modifier_pool`
- `card_reward_pool`
- `players`
- `player_progress`
- `player_card_unlocks`
- `player_relics`
- `player_consumables`
- `run_sessions`
- `run_deck_state`
- `run_segments`
- `run_segment_choices`
- `run_segment_modifiers`
- `run_segment_enemies`
- `run_rewards`
- `run_events`

Model conceptual:

- el contingut mestre esta separat del progres
- el progres persistent esta separat de l'estat temporal d'una run
- una run te segments
- un segment te enemics, eleccions, modificadors i recompenses

## 8. Canvis Que S'Han FET En El Projecte

Els canvis principals que s'han introduit son aquests:

### Canvis de backend

S'ha creat un projecte nou:

- [ArchitectusFati.Api.csproj](/e:/Dev/AFati/ArchitectusFati/Backend/ArchitectusFati.Api/ArchitectusFati.Api.csproj)

S'ha afegit:

- connexio segura amb PostgreSQL via `Npgsql`
- injeccio de `NpgsqlDataSource`
- inicialitzacio automatica de base de dades
- endpoints REST per progres, contingut i runs

### Canvis de base de dades

S'han creat dos scripts:

- [001_init.sql](/e:/Dev/AFati/ArchitectusFati/Backend/ArchitectusFati.Api/Database/001_init.sql)
- [002_full_game_schema.sql](/e:/Dev/AFati/ArchitectusFati/Backend/ArchitectusFati.Api/Database/002_full_game_schema.sql)

El segon amplia la base de dades per donar suport al joc complet.

### Canvis a Unity

S'ha afegit:

- [RemoteGameApiClient.cs](/e:/Dev/AFati/ArchitectusFati/Assets/Scripts/RemoteGameApiClient.cs)

S'ha ampliat:

- [RunManager.cs](/e:/Dev/AFati/ArchitectusFati/Assets/Scripts/RunManager.cs)
- [MapCardData.cs](/e:/Dev/AFati/ArchitectusFati/Assets/Scripts/MapCardData.cs)

Nous comportaments:

- mode remot opcional
- carregat de cartes des de l'API
- sincronitzacio de progres
- fallback local si la API falla

### Canvis d'organitzacio

S'ha afegit el backend a la solucio:

- [ArchitectusFati.slnx](/e:/Dev/AFati/ArchitectusFati/ArchitectusFati.slnx)

## 9. Decisions Tecniques Que Es Poden Defensar Davant Del Tribunal

### Separar client i backend

Justificacio:

- mes seguretat
- millor mantenibilitat
- mes facil escalar
- permet persistencia real multiusuari

### Fer servir Neon Postgres

Justificacio:

- base de dades real en el núvol
- PostgreSQL estandard
- bona opcio per projectes moderns
- facilita entorns reals de treball

### Fer servir una API REST

Justificacio:

- desacobla el joc de la base de dades
- permet evolucionar el client sense tocar l'accés SQL
- facilita proves i integracions futures

### Mantenir fallback local

Justificacio:

- el joc continua funcionant encara que l'API no estigui disponible
- permet desenvolupar i provar sense dependre sempre del servidor

### Modelar la run com una entitat persistent

Justificacio:

- permet reprendre o analitzar sessions
- facilita analytics
- ajuda a construir futurament classificacions o balancing

## 10. Com Explicar Oralment L'Arquitectura

Una explicacio oral simple podria ser:

> El joc està fet en Unity i la lògica principal de gameplay viu dins del client. Tot i això, les dades persistents no es guarden directament des de Unity a la base de dades, perquè això seria insegur. Per això he creat una API en ASP.NET Core que actua com a capa intermèdia. Aquesta API parla amb una base de dades PostgreSQL allotjada a Neon. D'aquesta manera, Unity només consumeix JSON via HTTP, mentre que el backend centralitza el contingut, el progrés del jugador i l'estat de les runs.

## 11. Punts Forts Del Projecte

- separacio clara entre gameplay, API i base de dades
- arquitectura professional per a un treball final de curs
- base de dades relacional real
- persistencia de progres
- preparat per ampliar-se a mes contingut
- model de dades coherent amb un roguelike de cartes i segments
- backend compilat i funcional

## 12. Limitacions Actuals

Per ser honest davant del tribunal, tambe es bo explicar limits actuals:

- Unity encara no consumeix tots els endpoints nous de runs i bootstrap
- no hi ha sistema visual complet d'inventari, relíquies o consumibles dins del client
- falta autenticacio real d'usuaris
- el sistema de modificadors encara esta mes preparat a base de dades que no pas integrat al gameplay

Aquestes limitacions no resten valor. Al contrari, mostren una visio realista d'un projecte en evolucio.

## 13. Línies Futures

Possibles ampliacions:

- login d'usuaris
- guardar i reprendre runs actives
- panell d'administracio per editar contingut
- analytics de partides
- balancing automàtic segons estadistiques de runs
- sincronitzacio de relíquies, consumibles i recompenses amb Unity

## 14. Frase Final De Defensa

Una frase bona per tancar la defensa pot ser:

> Aquest projecte no només resol la part jugable, sinó que també planteja una arquitectura escalable i segura, similar a la que faria servir un producte real, separant client, API i base de dades per garantir mantenibilitat, persistència i evolució futura.
