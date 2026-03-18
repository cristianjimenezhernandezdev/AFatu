# Pla Del Menu Principal, Perfils I Progres Persistent

## Objectiu

Aquest document defineix el pla de disseny per incorporar una pantalla principal al projecte, amb acces a joc, millores, poders i perfil de jugador, mantenint coherencia amb l'arquitectura actual de Unity, la API backend i la base de dades PostgreSQL.

Aquest document no implica canvis de codi. El seu objectiu es deixar clar:

- quines pantalles cal construir;
- quin flux ha de seguir el jugador;
- quines dades s'han de carregar abans de jugar;
- com ha d'encaixar el sistema classic de guardar i carregar;
- i com s'hauria de connectar tot plegat amb la BDD sense trencar el model actual.

## Punt De Partida Del Projecte

Segons l'estat actual del projecte, ja existeixen aquestes bases:

- un model local de `PlayerProfileData`;
- un model local de `PlayerProgressData`;
- repositoris locals per carregar i desar perfil i progres;
- una estructura backend amb API per progres remot;
- una BDD amb taules per `players`, `player_progress`, desbloquejos i inventari;
- i un `RunManager` que ja arrenca carregant perfil i progres.

Per tant, el menu principal no s'ha de plantejar com una capa decorativa, sino com la porta d'entrada al meta-joc persistent.

## Objectiu De Producte

El joc ha de tenir una entrada clara i professional:

1. El jugador obre el joc.
2. Ha de seleccionar o crear un perfil.
3. El joc carrega el progres associat a aquell perfil.
4. El jugador entra al menu principal.
5. Des del menu pot:
   - jugar;
   - veure o comprar millores;
   - revisar poders i desbloquejos;
   - consultar el perfil;
   - i, mes endavant, reprendre una run si existeix.

La idea central es aquesta:

- no es pot iniciar una run sense tenir un perfil actiu;
- el perfil actiu es la font de veritat del progres persistent carregat a la sessio.

## Pantalles Recomanades

## 1. Pantalla De Boot I Seleccio De Perfil

Ha de ser la primera pantalla obligatoria del joc.

Cap run ni cap menu principal s'hauria d'obrir fins que no hi hagi un perfil actiu seleccionat.

### Funcions principals

- mostrar perfils existents;
- crear un perfil nou;
- seleccionar perfil;
- esborrar perfil;
- i entrar al joc nomes quan hi hagi un perfil carregat.

### Informacio que ha de mostrar cada perfil

Com a minim:

- nom visible del perfil;
- identificador intern;
- esmeraldes actuals;
- runs completades;
- segment maxim assolit;
- longitud de run preferida;
- mode preferit de l'heroi.

### Valor de disseny

Aquesta pantalla resol un requisit clau del projecte:

- el progres ja no queda associat a un unic `local-player` rigid;
- passa a estar associat a un perfil triat explicitament abans de jugar.

## 2. Menu Principal

Aquesta pantalla es el hub central del joc.

### Opcions recomanades

- `Jugar`
- `Millores`
- `Poders`
- `Perfil`
- `Opcions`
- `Sortir`

Opcional mes endavant:

- `Continuar run`

### Contingut visual recomanat

El menu principal hauria de mostrar ja dades del perfil actiu:

- nom del perfil;
- esmeraldes;
- nombre de runs completades;
- segment maxim assolit;
- alguns desbloquejos destacats;
- i artwork reutilitzat des dels `artKey` ja previstos a BDD i seeds.

### Paper dins del projecte

Aquest menu no nomes ha de navegar entre pantalles. Ha de fer visible que el joc te una capa de meta-progres persistent.

## 3. Pantalla De Millores I Meta-Progres

Ha de ser la pantalla on el jugador gasta esmeraldes i desbloqueja contingut permanent.

Segons el balanc i la documentacio existent, aqui hi han d'entrar:

- desbloqueig de biomes;
- desbloqueig de cartes;
- desbloqueig de poders divins;
- millores petites permanents;
- i, si interessa, algunes millores tipus tech tree.

### Principis de disseny

- les millores han de ser petites i acumulatives;
- no han de trencar la dificultat base;
- han de reforcar la sensacio de progres entre runs.

### Relacio amb la BDD

Aquesta pantalla hauria de reutilitzar:

- ids persistents de contingut;
- desbloquejos guardats a `player_progress`;
- relacions de `player_card_unlocks` i `player_divine_power_unlocks`;
- i `artKey` de contingut per mostrar icones o artwork.

## 4. Pantalla De Poders I Loadout

Abans de jugar, el jugador hauria de poder revisar i preparar el seu equipament de run.

Com a minim:

- veure quins poders divins te desbloquejats;
- equipar els poders disponibles;
- escollir longitud de run;
- i escollir mode de comportament inicial de l'heroi.

### Dades ja alineades amb aquest objectiu

El projecte ja contempla:

- `selectedHeroMode`;
- `preferredRunLength`;
- slots maxims de poders divins;
- i desbloquejos persistents de poders.

Per tant, aquesta pantalla encaixa molt be amb el model actual.

## 5. Pantalla De Perfil

Ha de servir per consultar l'estat persistent del jugador.

### Informacio recomanada

- nom del perfil;
- idioma preferit;
- esmeraldes;
- estadistiques agregades;
- cartes desbloquejades;
- poders desbloquejats;
- reliquies persistents;
- consumibles persistents;
- i preferencies de joc.

### Objectiu

Fer visible que el perfil no es nomes un nom, sino una entitat persistent amb historial i progres.

## 6. Flux De Resultat I Retorn Al Menu

Quan una run acaba:

- es desa el progres actualitzat;
- es reflecteixen recompenses i desbloquejos;
- es mostra la pantalla de resum;
- i despres es torna al menu principal amb les dades ja refrescades.

Aquesta part es clau per donar sensacio de cicle complet:

- jugar;
- guanyar o perdre;
- rebre recursos;
- tornar al meta-joc;
- invertir el progres;
- tornar a jugar.

## Flux Recomanat Del Jugador

El flux recomanat del producte queda aixi:

1. `Boot`
2. `Select Profile`
3. `Main Menu`
4. `Loadout / Pre-Run Setup`
5. `Gameplay`
6. `Run Summary`
7. `Main Menu`

Flux alternatiu:

1. `Boot`
2. `Select Profile`
3. `Main Menu`
4. `Millores`
5. `Main Menu`
6. `Jugar`

## Sistema De Perfil Actiu

La decisio arquitectonica mes important es introduir el concepte de `perfil actiu`.

### Regla principal

Tot el joc ha de funcionar sobre un `activePlayerId`.

No s'hauria d'assumir que sempre existeix un unic jugador fix.

### Consequencies de disseny

- `RunManager` no hauria de decidir per si sol quin perfil carrega;
- una capa anterior de boot o menu hauria de seleccionar el perfil actiu;
- llavors s'inicialitza la resta del joc amb aquell context.

### Beneficis

- suport real per a multiples perfils;
- base neta per a guardar i carregar classic;
- coherencia amb el model `players` de la BDD;
- i futura extensio a autenticacio o perfils remots.

## Sistema Classic De Guardar I Carregar

La recomanacio es mantenir un model `local-first`.

## Principi general

Primer:

- existeix una copia local del perfil i del progres.

Despres:

- si el backend esta disponible, es sincronitza.

Aixo encaixa amb l'arquitectura actual del projecte, que ja defensa:

- fallback local;
- i sincronitzacio remota sense dependre completament del servidor.

## Quines dades s'han de desar

Com a minim, el sistema classic ha de guardar:

- perfil;
- progres persistent;
- desbloquejos;
- poders;
- consumibles;
- reliquies;
- moneda persistent;
- preferencies;
- i opcionalment estat d'una run activa.

## Separacio recomanada de dades

### Bloc 1. Identitat de perfil

- `playerId`
- `displayName`
- idioma
- preferencies de presentacio o joc

### Bloc 2. Progres persistent

- esmeraldes;
- runs completades;
- runs fallides;
- segment maxim;
- cartes desbloquejades;
- poders desbloquejats;
- biomes desbloquejats;
- flags de progres.

### Bloc 3. Inventari persistent

- reliquies;
- consumibles;
- altres millores permanents.

### Bloc 4. Estat temporal opcional

- run activa;
- deck actual;
- progressio interna de la run.

Aquest quart bloc no es imprescindible per a la primera iteracio del menu principal, pero deixa preparada l'opcio `Continuar`.

## Connexio Amb La Base De Dades

La connexio amb la BDD no s'ha de fer directament des de Unity.

S'ha de mantenir el patro ja definit a la documentacio del projecte:

- Unity
- API ASP.NET Core
- Neon PostgreSQL

## Que ja encaixa amb la BDD actual

La BDD ja te una base molt valida per aquest sistema:

- `players`
- `player_progress`
- `player_card_unlocks`
- `player_divine_power_unlocks`
- `player_relics`
- `player_consumables`

Aixo vol dir que:

- la idea de perfil ja existeix a nivell de dades;
- la idea de progres persistent ja existeix;
- i la idea d'inventari persistent tambe hi es.

## Gap actual a tenir en compte

Tot i que la BDD i els models locals son amplis, el contracte remot actual es mes reduit.

Ara mateix, el client remot i els DTOs del backend cobreixen sobretot:

- cartes;
- i una part del progres del jugador.

Per tant, el pla correcte no es fer dependre tota la UX nova del backend des del primer dia.

La recomanacio es:

- primera iteracio local-first;
- segona iteracio amb sincronitzacio remota ampliada;
- tercera iteracio amb perfil remot complet i, si conve, continuacio de run.

## Fases Recomanades D'Implementacio

## Fase 1. Definicio Del Flux I Wireframe

Abans de muntar res a Unity:

- definir el flux complet de pantalles;
- decidir quina informacio veu el jugador a cada una;
- i establir quina pantalla bloqueja l'acces a la run si no hi ha perfil seleccionat.

Sortida esperada:

- wireframe simple de `Profile Select`, `Main Menu`, `Millores`, `Loadout` i `Perfil`.

## Fase 2. Perfil Actiu I Navegacio Base

Objectiu:

- que el joc no arrenqui directament la run;
- que primer demani un perfil;
- i que el menu principal rebi aquest context.

Sortida esperada:

- es pot triar perfil;
- el joc mostra dades del perfil actiu;
- `Play` queda lligat a aquest perfil.

## Fase 3. Menu Principal Functional

Objectiu:

- tenir operatius els botons principals;
- encara que algunes pantalles internes comencin com a placeholders visuals.

Minim viable:

- `Play`
- `Millores`
- `Poders`
- `Perfil`

## Fase 4. Meta-Progres I Compra De Millores

Objectiu:

- connectar la pantalla de millores amb el progres persistent;
- fer visibles esmeraldes, desbloquejos i costos;
- i preparar la logica de compra o desbloqueig.

Encara que el codi no es toqui en aquesta fase de disseny, la pantalla s'ha de plantejar ja com una vista de dades reals.

## Fase 5. Sincronitzacio Remota Del Perfil

Objectiu:

- ampliar el backend i el client remot quan arribi el moment;
- portar no nomes el progres basic, sino perfil i estat meta complet.

Aquesta fase no s'ha de posar al davant de la UX local. S'ha de fer quan la capa de menu i perfil ja estigui validada.

## Fase 6. Continuar Run

Objectiu opcional:

- permetre reprendre una run activa.

No ho recomanaria com a requisit inicial del menu principal. Te sentit com a extensio futura.

## Quines Parts Son Prioritat Real

Si s'ha de prioritzar, l'ordre recomanat es aquest:

1. Seleccio de perfil
2. Perfil actiu
3. Menu principal
4. Pantalla de millores
5. Pantalla de loadout
6. Sincronitzacio remota ampliada
7. Continuar run

La prioritat mes alta no es el look del menu, sino el mecanisme que decideix quin progres estas carregant.

## Recomanacio De Muntatge A Unity

Com que el disseny visual el faras des de Unity, la recomanacio es separar clarament:

- escena o capa de menu;
- escena o capa de gameplay;
- i, si conve, una capa comuna de gestio de sessio o perfil actiu.

### Jerarquia recomanada de pantalles

- `Canvas_MainMenu`
- `BootPanel`
- `ProfileSelectPanel`
- `MainMenuPanel`
- `MetaProgressPanel`
- `LoadoutPanel`
- `ProfilePanel`
- `OptionsPanel`

No cal que tot sigui en escenes separades. Pot ser una sola escena de menu amb panells activables, si et resulta mes rapid per al vertical slice.

## Decisions De Disseny Que Conve Mantenir

Per no desalinear-te amb el projecte actual, conve mantenir aquestes regles:

- Unity no accedeix directament a la BDD;
- el perfil actiu es decideix abans d'arrencar la run;
- el menu principal llegeix dades del perfil real, no placeholders arbitraris;
- les millores usen esmeraldes com a moneda de meta-progres;
- els `artKey` existents s'han de reutilitzar per al menu i l'arbre de millores;
- el mode local ha de continuar funcionant encara que el backend no estigui disponible.

## Riscos Si No Es Fa Aixi

Si es construeix el menu principal sense aquest plantejament, hi ha risc de:

- acabar amb una UI visualment correcta pero desconnectada del progres real;
- duplicar logica de perfil a llocs diferents;
- acoblar massa aviat el menu al backend parcial actual;
- o haver de refer el sistema quan arribi la seleccio real de perfils.

## Conclusio

El menu principal que necessita ara `Architectus Fati` no es nomes una portada amb un boto de `Play`.

Es una capa de meta-joc que ha de fer de pont entre:

- perfil de jugador;
- progres persistent;
- desbloquejos;
- economia d'esmeraldes;
- preparacio de run;
- i, mes endavant, sincronitzacio remota completa.

La millor estrategia, en base al que ja existeix al projecte, es aquesta:

- primer definir perfil actiu i seleccio de perfil;
- despres construir el menu principal com a hub;
- tot seguit connectar-hi millores i loadout;
- i finalment ampliar la sincronitzacio remota perque la BDD reflecteixi el sistema complet.

Aquest ordre respecta l'arquitectura actual, aprofita el model de dades existent i evita construir una UI bonica pero desvinculada del cor del projecte.

## Implementacio actual de la primera versio a Unity

Despres d'aquesta planificacio, el projecte ja te una primera implementacio funcional del hub principal i de la pantalla d'arbre de millores. La idea no ha estat muntar un layout final des de codi, sino deixar preparats els scripts i el flux de dades perque el disseny visual es pugui construir manualment dins Unity.

### Scripts afegits per a aquesta capa

- `Assets/Scripts/UI/Canvas/MainMenuCanvasController.cs`
  - controlador principal del menu;
  - obre el hub en arrencar;
  - impedeix que `RunManager` iniciï automaticament una run mentre el menu esta visible;
  - navega entre `Home` i `TechTree`;
  - llança `StartRun()` quan es prem `Play`.

- `Assets/Scripts/UI/Canvas/MainMenuHomeCanvasPanel.cs`
  - refresca les dades del perfil actiu;
  - mostra esmeraldes, runs, segment maxim i resum de desbloquejos;
  - permet canviar longitud de run i mode inicial de l'heroi;
  - dona acces al joc i a l'arbre de millores.

- `Assets/Scripts/UI/Canvas/TechTreeCanvasPanel.cs`
  - construeix la pantalla de meta-progres a partir del contingut real;
  - llegeix poders divins, biomes, cartes i la millora de run llarga;
  - calcula estat del node, cost i disponibilitat;
  - envia les compres al `RunManager`.

- `Assets/Scripts/UI/Canvas/TechTreeNodeCanvasSlot.cs`
  - representa un node individual de l'arbre;
  - mostra text, cost, estat, art i boto d'accio;
  - es pot usar com a prefab replicable dins un `ScrollRect`.

### Dades del projecte que ja alimenten el menu

La implementacio actual reutilitza directament el model persistent existent:

- `players`
- `player_progress`
- `player_card_unlocks`
- `player_divine_power_unlocks`
- `biomes`
- `cards`
- `divine_power_definitions`

A la practica, el menu llegeix:

- el nom i preferencies del perfil;
- les esmeraldes disponibles;
- les cartes desbloquejades;
- els poders divins desbloquejats;
- els biomes disponibles;
- i la disponibilitat de la run llarga.

### Peces de UI que has d'afegir manualment a Unity

Es recomana muntar-ho dins un `Canvas_MainMenu` separat del HUD de run.

Jerarquia minima recomanada:

- `Canvas_MainMenu`
- `MainMenuRoot`
- `HomePanel`
- `TechTreePanel`
- `TechTreeScrollView`
- `TechTreeContent`
- `TechTreeNodePrefab`

### Configuracio recomanada del controlador principal

Al GameObject `MainMenuRoot`:

- afegeix `MainMenuCanvasController`;
- assigna `runManager`;
- assigna `menuRoot` al mateix `MainMenuRoot`;
- assigna `homePanel` i `homePanelRoot`;
- assigna `techTreePanel` i `techTreePanelRoot`;
- a `gameplayRootsToHide`, afegeix el canvas HUD de run i qualsevol bloc visual que vulguis ocultar mentre el menu esta obert.

### Elements recomanats per a `HomePanel`

Al GameObject `HomePanel`:

- afegeix `MainMenuHomeCanvasPanel`;
- crea textos `TMP_Text` per a:
  - titol;
  - nom del perfil;
  - esmeraldes;
  - resum de progres;
  - resum de desbloquejos;
  - resum de longitud de run i mode;
  - missatge inferior o pista.

Crea i assigna botons per a:

- `Play`
- `Tech Tree`
- `Run curta`
- `Run llarga`
- `Mode prudent`
- `Mode aggressive`
- `Mode escape`

### Elements recomanats per a `TechTreePanel`

Al GameObject `TechTreePanel`:

- afegeix `TechTreeCanvasPanel`;
- crea textos `TMP_Text` per a:
  - titol;
  - resum superior;
  - feedback de compra o bloqueig.

Crea un boto `Back` i assigna'l.

Dins del `ScrollRect`:

- crea un contenidor `TechTreeContent`;
- crea un prefab o objecte model `TechTreeNodePrefab`;
- assigna aquest prefab al camp `nodePrefab`;
- assigna `TechTreeContent` al camp `nodeContainer`.

### Elements recomanats per a cada node de l'arbre

Al prefab `TechTreeNodePrefab`:

- afegeix `TechTreeNodeCanvasSlot`;
- crea textos `TMP_Text` per a:
  - branca o categoria;
  - titol;
  - descripcio;
  - cost;
  - estat;
  - text del boto.

Afegeix i assigna:

- una `Image` per artwork;
- una `Image` per al marc;
- una `Image` per a la barra d'accent;
- un `Button` d'accio.

Si no hi ha sprite per a algun node, el sistema simplement ocultara la `Image` de l'art.

### Criteris de desbloqueig que fa servir aquesta primera versio

Per mantenir coherencia amb la BDD actual:

- els poders divins usen el `unlockCost` real del seed;
- la run llarga es desbloqueja com a node meta independent;
- els biomes tenen un cost fix lleuger segons la seva importancia;
- les cartes fan servir un cost derivat de `baseDifficulty` i `rewardTier`, ja que la BDD actual no defineix encara un `unlock_cost` explicit per a cartes.

Aquesta ultima part es una decisio de transicio per poder tenir l'arbre funcional sense canviar ara mateix l'esquema SQL.

### Valor d'aquesta implementacio

Amb aquesta capa, el projecte ja te una entrada real de meta-joc:

- el joc no comenca directament a la run;
- el perfil actiu i les seves preferencies es fan visibles;
- es poden preparar opcions abans de jugar;
- i l'arbre de millores deixa de ser una idea de disseny i passa a ser una pantalla funcional.

## Actualitzacio posterior. Selector de perfil i guardat multi-perfil

Despres de tenir el menu principal i l'arbre de millores funcionant a Unity, la necessitat seguent ha estat donar suport real a multiples perfils.

La decisio presa ha estat no construir un sistema paral.lel, sino ampliar el repositori local perquè reflecteixi millor el model de la BDD:

- multiples files logiques de `players`;
- multiples entrades de `player_progress`;
- i desbloquejos separats per `playerId`.

Aquesta ampliacio permet que el menu principal mostri una llista de perfils, carregui el perfil seleccionat i cree perfils nous sense trencar el flux ja implementat de run i meta-progres.

Per al muntatge manual a Unity i el detall tecnic complet d'aquesta fase, consulta:

- `Docs/PROFILE_SELECTOR_SAVE_SYSTEM_IMPLEMENTATION.md`
