# Implementacio del selector de perfil i del sistema de guardat multi-perfil

Aquest document explica com queda implementat el selector de perfil a `Architectus Fati` i quins passos has de seguir a Unity per connectar la UI manual que ja tens muntada.

## Objectiu funcional

La pantalla principal ha de permetre:

- veure la llista de perfils disponibles;
- seleccionar quin perfil queda actiu;
- crear un perfil nou;
- i carregar de manera consistent el seu progres persistent:
  - esmeraldes;
  - cartes desbloquejades;
  - poders divins;
  - biomes;
  - preferencies de run;
  - i consumibles.

## Relacio amb la BDD

Per a aquesta funcionalitat no ha calgut afegir taules noves a la BDD.

L'esquema actual ja suporta multi-perfil a traves de:

- `players`
- `player_progress`
- `player_card_unlocks`
- `player_divine_power_unlocks`
- `player_consumables`

La clau de la implementacio ha estat fer que el repositori local deixe de treballar com si nomes existira `local-player` i passe a guardar una col.leccio de perfils, mantenint la correspondencia amb aquestes taules.

## Canvi arquitectonic principal

Abans:

- el projecte guardava un unic bloc `profile + progress + unlocks + consumables`.

Ara:

- el fitxer local de guardat funciona com una base de dades local simplificada;
- guarda multiples perfils;
- recorda quin perfil esta actiu;
- i substitueix nomes les dades del perfil seleccionat quan hi ha canvis.

Aquest enfocament permet:

- tindre selector de perfil real al menu;
- no perdre el progres quan es canvia de perfil;
- i mantindre el sistema preparat per a una futura sincronitzacio remota.

## Scripts nous o ampliats

### `Assets/Scripts/Core/Models/ProgressionModels.cs`

S'han afegit:

- `LocalProgressionDatabaseSeed`
- `PlayerProfileSummaryData`

També s'ha ampliat `PlayerProfileData` amb:

- `createdAtUtc`
- `lastSeenAtUtc`

### `Assets/Scripts/Data/RepositoryInterfaces.cs`

`IProgressionRepository` ara exposa operacions de perfil:

- obtindre perfil actiu;
- llistar perfils;
- seleccionar perfil;
- crear perfil nou.

### `Assets/Scripts/Data/LocalRepositories.cs`

`LocalFileProgressionRepository` ara:

- migra automaticament el guardat antic single-profile;
- carrega una base de dades local multi-perfil;
- crea perfils nous a partir del seed inicial;
- canvia el perfil actiu;
- i guarda progressio separada per `playerId`.

### `Assets/Scripts/RunManager.cs`

`RunManager` ara:

- treballa amb un `CurrentPlayerId` real;
- pot llistar perfils disponibles;
- pot carregar un perfil seleccionat;
- pot crear i activar un perfil nou;
- i torna a construir economia, desbloquejos i estat persistent en canviar de perfil.

### `Assets/Scripts/UI/Canvas/MainMenuCanvasController.cs`

S'han afegit ponts per:

- seleccionar perfil;
- crear perfil;
- refrescar els panells del menu quan canvia el perfil actiu.

### `Assets/Scripts/UI/Canvas/MainMenuHomeCanvasPanel.cs`

El panell principal ara admet camps opcionals per a:

- `TMP_Dropdown` de perfils;
- `TMP_InputField` per al nom del perfil nou;
- boto de crear perfil;
- text de feedback del selector.

## Elements de UI que has d'afegir a Unity

Com que la UI la continues muntant manualment, a `HomePanel` afegeix aquests elements:

- un `TMP_Dropdown` anomenat per exemple `ProfileDropdown`;
- un `TMP_InputField` per exemple `NewProfileNameInput`;
- un `Button` per exemple `CreateProfileButton`;
- un `TMP_Text` opcional per exemple `ProfileFeedbackText`.

## Assignacions a l'Inspector

Al component `MainMenuHomeCanvasPanel` de `HomePanel`, a mes dels camps que ja tens, assigna:

- `profileDropdown` -> el `TMP_Dropdown` del selector;
- `newProfileNameInput` -> l'input del nom;
- `createProfileButton` -> el boto de crear;
- `profileFeedbackText` -> el text on vols mostrar missatges de carrega o creacio.

No cal afegir `OnClick` manuals als botons ni al dropdown des de l'Inspector, perquè el mateix script registra els listeners en `Initialize`.

## Flux esperat en execucio

1. El joc arrenca i el `RunManager` obri el repositori de progressio.
2. El repositori resol quin perfil era l'ultim actiu.
3. El `HomePanel` ompli el dropdown amb tots els perfils guardats.
4. Si l'usuari canvia el dropdown:
   - es carrega el nou perfil;
   - s'actualitzen esmeraldes, desbloquejos i preferencies;
   - i l'arbre de millores es refresca amb aquestes dades.
5. Si l'usuari crea un perfil nou:
   - es genera un `playerId` nou;
   - es clonen les dades inicials del seed;
   - el perfil queda guardat;
   - i passa a ser el perfil actiu.

## Com es guarda la informacio

Cada vegada que el joc desa progressio:

- es desa el `players` equivalent del perfil actiu;
- es desa el seu `player_progress`;
- es reescriuen els desbloquejos i consumibles d'eixe `playerId`;
- i no es toquen les dades de la resta de perfils.

Aixo garanteix consistencia entre:

- pantalla principal;
- arbre de millores;
- i loop de run.

## Compatibilitat amb el guardat anterior

Si ja existeix un guardat antic amb un unic perfil:

- el repositori el migra automaticament al nou format multi-perfil;
- el conserva com a perfil existent;
- i el marca com a perfil actiu.

Per tant, no hauries de perdre el progres que ja tens.

## Recomanacio visual per al dropdown

El text del selector mostra actualment:

- nom del perfil;
- esmeraldes;
- i nombre de runs completades.

Si vols fer-lo mes llegible en el layout final, et recomane una capçalera amb:

- `Perfil`
- `Esmeraldes`
- `Runs`

o be una targeta lateral amb els detalls ampliats del perfil seleccionat.

## Validacio minima despres del muntatge

Quan connectes aquests elements a Unity, comprova:

1. que el dropdown mostra el perfil actual en arrancar;
2. que crear un perfil nou el selecciona automaticament;
3. que les esmeraldes i desbloquejos canvien realment en canviar de perfil;
4. que comprar una millora en un perfil no afecta els altres;
5. que tancar i reobrir el joc conserva l'ultim perfil actiu.
