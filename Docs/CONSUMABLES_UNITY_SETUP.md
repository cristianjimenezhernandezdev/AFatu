# Configuracio a Unity de la iteracio de consumibles

Aquest document explica exactament quins canvis has de fer dins Unity per connectar la nova iteracio de consumibles jugables.

## Resum de la iteracio

Ara el codi ja suporta:

- consumibles persistents per perfil;
- ús durant la run;
- efectes temporals;
- i un HUD de canvas per mostrar-los.

Els consumibles implementats son:

- `healing_potion`
- `speed_dose`
- `guard_token`
- `smoke_bomb`

Les tecles rapides per defecte son:

- `3`
- `4`
- `5`
- `6`

## Scripts nous que has de veure a Unity

Els scripts nous d'aquesta iteracio son:

- `Assets/Scripts/UI/Canvas/ConsumablesCanvasPanel.cs`
- `Assets/Scripts/UI/Canvas/ConsumableCanvasSlot.cs`

També s'ha ampliat:

- `Assets/Scripts/UI/Canvas/RunCanvasHudController.cs`

## Pas 1. Crear el panell de consumibles dins del HUD

Dins del teu `Canvas_HUD` o del canvas de run que ja tens:

- crea un nou GameObject, per exemple `ConsumablesPanel`;
- afegeix-li el component `ConsumablesCanvasPanel`.

Elements recomanats dins d'aquest panell:

- un `TMP_Text` per al titol;
- un `TMP_Text` per a la pista inferior;
- quatre fills, un per cada slot de consumible.

## Pas 2. Crear els quatre slots de consumible

Per a cada slot crea un GameObject, per exemple:

- `ConsumableSlot_1`
- `ConsumableSlot_2`
- `ConsumableSlot_3`
- `ConsumableSlot_4`

A cada slot afegeix el component:

- `ConsumableCanvasSlot`

## Pas 3. Elements UI que necessita cada slot

Dins de cada slot afegeix i assigna:

- un `TMP_Text` per a `hotkeyText`
- un `TMP_Text` per a `titleText`
- un `TMP_Text` per a `quantityText`
- un `TMP_Text` per a `stateText`
- un `TMP_Text` per a `descriptionText`
- una `Image` per a `artworkImage`
- un `Button` per a `useButton`

Opcionalment:

- pots usar el mateix GameObject arrel del slot com a `root`;
- o deixar `root` buit si vols que el mateix objecte actue com a contenidor visible.

## Pas 4. Assignar els slots al panell

Al component `ConsumablesCanvasPanel`:

- assigna el `TMP_Text` del titol a `headerText`;
- assigna el `TMP_Text` de pista a `hintText`;
- assigna els quatre `ConsumableCanvasSlot` al camp `slots`, en aquest ordre:
  - slot 1
  - slot 2
  - slot 3
  - slot 4

L'ordre es important perquè correspon a:

- tecla `3` -> primer slot
- tecla `4` -> segon slot
- tecla `5` -> tercer slot
- tecla `6` -> quart slot

## Pas 5. Connectar el panell al HUD principal

Selecciona l'objecte que te el component `RunCanvasHudController`.

Al camp nou:

- `consumablesPanel`

assigna-hi el teu `ConsumablesPanel`.

Sense aquesta assignacio, el HUD no refrescara els consumibles.

## Pas 6. No cal configurar events manuals

No cal afegir `OnClick` manuals als botons de cada slot.

El mateix script `ConsumableCanvasSlot` registra el comportament del boto i crida internament al `RunManager`.

## Com es veu en execucio

Quan la run esta en exploracio:

- el panell mostra els consumibles disponibles;
- cada slot mostra nom, quantitat i estat;
- si un consumible te art via `artKey`, es carrega automaticament;
- el boto d'ús queda actiu només si hi ha unitats disponibles.

## Significat dels estats

Els estats previstos al slot son:

- `Disponible`
- `Esgotat`
- `Actiu Xs`
- `Preparat`

`Preparat` s'usa especialment per a:

- `smoke_bomb`

quan ja esta armada per evitar el proxim combat.

## Comprovar que tot funciona

Fes aquesta comprovacio manual:

1. entra a una run amb algun consumible al perfil;
2. comprova que el panell mostra quantitat correcta;
3. prem `3`, `4`, `5` o `6`;
4. verifica que el consumible baixa de quantitat;
5. comprova el missatge de feedback al HUD;
6. comprova que `healing_potion` cura;
7. comprova que `speed_dose` i `guard_token` mostren estat temporal;
8. comprova que `smoke_bomb` evita el proxim combat.

## Nota important

Si el panell no apareix o no refresca:

- revisa que `RunCanvasHudController` tinga el camp `consumablesPanel` ben assignat;
- revisa que cada slot tinga el seu `Button` i `TMP_Text` assignats;
- i revisa que el perfil actiu tinga consumibles guardats.
