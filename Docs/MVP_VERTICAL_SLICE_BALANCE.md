# ArchitectusFati - Document De Balanc MVP Vertical Slice

## 1. Objectiu Del Document

Aquest document defineix els valors inicials de balanc i la corba de dificultat per al vertical slice de `Architectus Fati`.

Serveix per:

- fixar una referencia comuna de disseny
- ajudar a implementar mecaniques amb criteri coherent
- deixar clar que es objectiu de balanc i que es estat actual del projecte
- facilitar futures iteracions de playtesting

Versio:

- `0.1`

Important:

- tots els valors d'aquest document son inicials
- s'han d'ajustar despres de proves reals
- l'objectiu del vertical slice no es tancar el balanc final, sino validar la direccio del joc

## 2. Pilar De Disseny Del Vertical Slice

`Architectus Fati` es un roguelite on el jugador no controla directament l'heroi. El jugador actua com a arquitecte del desti i modifica el recorregut a traves de:

- seleccio de cartes de bioma
- activacio de poders divins
- decisions estrategiques de risc, recompensa i recursos

El balanc del vertical slice ha de garantir:

- inici amable i llegible
- pressio real a partir del tercer segment
- valor tactico de les cartes de bioma
- utilitat de botiga i poders divins sense convertir-los en obligatoris
- consequencies clares en combats, economia i recursos

## 3. Objectius De Jugabilitat

Els objectius de sensacio de joc son aquests:

- el jugador ha d'entendre el sistema en els dos primers segments
- l'heroi no ha de morir per errors menors al principi
- el segment tres ha de marcar el punt d'inflexio
- la run curta de cinc segments ha de tenir un climax clar
- la run llarga de set segments ha de quedar preparada com a contingut avancat

## 4. Estat Base De L'Heroi

### 4.1 Estadistiques Base Objectiu

| Valor | Quantitat | Nota |
| --- | ---: | --- |
| Vida maxima | 30 | vida inicial i maxima al comencament de run |
| Atac base | 5 | valor de referencia per al combat base |
| Defensa base | 1 | redueix dany rebut en formula simple |
| Velocitat base | 1.0 | valor normalitzat de moviment |
| Radi d'avaluacio de perill | 6 caselles | area de lectura de risc |
| Distancia de prudencia | 3 caselles | distancia davant enemics perillosos o elites |

### 4.2 Criteri De Balanc

L'heroi ha de tenir prou resistencia per:

- aguantar combats basics al primer i segon segment
- castigar males decisions repetides
- obligar el jugador a valorar curacions i buffs a partir del tercer segment

## 5. Sistema De Combat

### 5.1 Formula Base

Formula de dany objectiu:

```text
dany = max(1, atac_atacant - defensa_defensor)
```

### 5.2 Intercanvi De Cops

Quan dos actors entren en combat cos a cos:

- s'intercanvien cops segons velocitat o interval d'atac
- el combat no s'ha de sentir instantani si es busca millor llegibilitat
- per al vertical slice es acceptable una resolucio simplificada si el resultat es coherent amb el balanc objectiu

### 5.3 Regles De Decisio De L'Heroi

L'heroi entra en combat si:

- la vida estimada despres del combat queda per sobre del `50%` de la vida actual
- l'enemic bloqueja completament el cami cap al `Goal`
- hi ha un poder divi actiu que afavoreix agressivitat

L'heroi evita o intenta fugir si:

- la vida estimada despres del combat pot caure per sota del `35%`
- hi ha multiples enemics propers
- hi ha un enemic elite sense suport favorable

## 6. Enemics Del MVP

El vertical slice objectiu inclou quatre tipus d'enemic.

### 6.1 Taula D'Estadistiques

| Enemic | Rol | Vida | Atac | Defensa | Velocitat | Recompensa |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Skeleton | basic | 10 | 4 | 0 | 1.0 | 2 or |
| Bat | rapid | 6 | 3 | 0 | 1.5 | 2 or |
| Zombie | lent i resistent | 18 | 5 | 1 | 0.65 | 4 or |
| Ghost | elite a distancia | 14 | 6 | 1 | 1.1 | 7 or |

### 6.2 Notes De Rol

`Skeleton`

- ha de ser l'enemic estandard
- ha d'apareixer aviat i sovint
- ajuda a calibrar la base del combat

`Bat`

- ha de pressionar per mobilitat
- ha de castigar trajectes insegurs
- ha de ser visualment rapid i lleuger

`Zombie`

- ha de forcar desviaments o decisions prudents
- ha de ser menys frequent que skeleton i bat
- es ideal per segments tres en endavant

`Ghost`

- es enemic elite
- ha de ser clarament mes amena�ador visualment
- ha d'apareixer a partir del segment tres

### 6.3 Estetica Recomanada

Tots els enemics nous han de respectar:

- moviment per punts
- construccio procedural o per mini-graella
- multiples colors per donar volum i lectura
- una estetica "point HD", no un simple pixel unic

Recomanacio visual:

- figures de `8x8`, `10x10` o `12x12`
- 3 a 5 colors per enemic
- animacio per canvi de forma o per subcapes

## 7. Economia De Run

### 7.1 Monedes

Hi ha dues monedes objectiu:

`Or`

- moneda de run
- s'obte derrotant enemics i obrint cofres
- es gasta a botiga o en efectes temporals

`Esmeraldes`

- moneda de metaprogres
- s'obte al final de la run
- serveix per desbloquejos permanents

### 7.2 Recompenses D'Or

#### Enemics

| Enemic | Or |
| --- | ---: |
| Skeleton | 2 |
| Bat | 2 |
| Zombie | 4 |
| Ghost | 7 |

#### Cofres

| Tipus De Cofre | Or |
| --- | ---: |
| Petit | 4 |
| Mitja | 6 |
| Rar | 9 |

#### Or Esperat Per Segment

| Segment | Or Esperat |
| --- | --- |
| 1 | 4 a 8 |
| 2 | 6 a 10 |
| 3 | 8 a 13 |
| 4 | 10 a 15 |
| 5 | 12 a 18 |

Objectiu de run curta eficient:

- `40 a 55` d'or total en 5 segments

## 8. Botiga

### 8.1 Aparicio

Per a la run curta:

- la botiga apareix a partir del segment `3`

Per a la run llarga:

- la primera botiga pot apareixer despres del segment `5`

### 8.2 Regla Base

Cada botiga ofereix exactament:

- `3 opcions aleatories`

### 8.3 Costos Inicials

| Opcio | Efecte | Cost |
| --- | --- | ---: |
| Curacio petita | +8 vida | 6 or |
| Curacio gran | +15 vida | 10 or |
| Buff de for�a | +2 atac durant 1 segment | 7 or |
| Buff de velocitat | +25% velocitat durant 1 segment | 6 or |
| Escut temporal | +2 defensa durant 1 segment | 7 or |
| Reroll de cartes | regenerar opcions | 5 or |
| Company temporal | invocacio temporal | 12 or |
| Equipament temporal | bonus situacional | 9 or aprox |

### 8.4 Principi De Balanc

La botiga ha de ser:

- util
- temptadora
- no obligatoria

## 9. Poders Divins

### 9.1 Regla Base

Abans d'iniciar la run el jugador pot equipar:

- `2 poders divins`

Al comencament del joc hi ha disponible:

- `1 poder`

### 9.2 Poder Inicial

`Benediccio De Velocitat`

| Valor | Quantitat |
| --- | ---: |
| Bonus | +35% velocitat |
| Durada | 8 segons |
| Cooldown | 30 segons |

### 9.3 Categories Futures

`Buffs`

- velocitat
- for�a
- defensa

`Control De Comportament`

- agressivitat
- prudencia

`Intervencio Directa`

- company temporal
- escut
- distraccio o control enemic

## 10. Corba De Dificultat

### 10.1 Run Curta De 5 Segments

| Segment | Enemics | Elites | Multiplicador | Vida Esperada Al Final |
| --- | --- | --- | ---: | --- |
| 1 | 2 a 3 | 0 | 1.00 | 70% a 100% |
| 2 | 3 a 4 | 0 | 1.10 | 55% a 85% |
| 3 | 4 a 5 | 0 a 1 | 1.25 | 40% a 70% |
| 4 | 5 a 6 | 1 | 1.40 | 25% a 60% |
| 5 | 6 a 7 | 1 a 2 | 1.60 | climax de run |

### 10.2 Run Llarga De 7 Segments

| Segment | Elites | Multiplicador | Nota |
| --- | --- | ---: | --- |
| 6 | 1 a 2 | 1.80 | contingut avancat |
| 7 | fins a 2 | 2.00 | punt mes dificil |

## 11. Metaprogres

Les esmeraldes s'utilitzen per:

- desbloquejar biomes
- desbloquejar cartes
- desbloquejar nous poders divins
- aplicar millores permanents petites

### 11.1 Exemples De Millora Permanent

- `+3` vida inicial
- `+1` or inicial
- petita pujada de probabilitat de cofres
- reduccio del cost del primer reroll
- petita millora de recompenses d'or

Principi de balanc:

- millores petites
- acumulatives
- no han de trencar la dificultat base

## 12. Cartes De Bioma

Les cartes defineixen el seguent segment.

### 12.1 Categories

`Segures`

- menys enemics
- menys recompensa
- camins mes nets

`Equilibrades`

- risc i recompensa estandard

`Arriscades`

- mes enemics
- mes probabilitat d'elites
- mes cofres i or

### 12.2 Funcio Estrategica

Aquest sistema ha de permetre:

- decidir quin risc assumir
- modular economia i pressio de run
- donar identitat al rol d'arquitecte del desti

## 13. Estat Actual Del Projecte Vs Objectiu De Balanc

Aquest apartat aterra el document al projecte Unity actual.

| Sistema | Estat Actual | Objectiu Del Document | Gap Principal |
| --- | --- | --- | --- |
| Heroi | 10 vida, 3 atac, sense defensa exposada | 30 vida, 5 atac, 1 defensa | cal refactor d'estadistiques |
| Combat | intercanvi simple d'un cop cadascu | formula atac-defensa + heuristica millor | falta defensa i avaluacio completa |
| Enemics | component generic + visuals Skeleton/Bat | 4 tipus: Skeleton, Bat, Zombie, Ghost | falten Zombie i Ghost reals |
| Economia | no hi ha or persistent de run | sistema d'or i esmeraldes | falta manager economic |
| Cofres | no implementats | petit, mitja, rar | falten prefabs + logica |
| Botiga | no implementada | 3 opcions a partir del segment 3 | falta sistema complet |
| Poders divins | no implementats | 2 slots, 1 inicial disponible | falta sistema complet |
| Dificultat | segments actuals procedurals basics | corba 1.0 a 1.6 o 2.0 | falta escalat per segment |
| Cartes de risc | biomes visuals i probabilitats simples | segures, equilibrades, arriscades | cal enriquir definicio de cartes |

## 14. Elements Nous Que Cal Afegir A Unity

Si vols que el vertical slice reflecteixi de veritat aquest document, si, caldra afegir nous elements.

### 14.1 Nous Enemics

Cal afegir:

- `Zombie`
- `Ghost`

Recomanacio:

- no calen sprites externs
- millor fer-los amb renderer procedural de punts, igual que `Player`, `Skeleton` i `Bat`

### 14.2 Nous Elements D'Entorn I Interaccio

Cal afegir:

- `ChestSmall`
- `ChestMedium`
- `ChestRare`
- `ShopNode` o `ShopTrigger`

Opcional pero recomanat:

- `DivinePowerManager`
- `EconomyManager`
- `SegmentDifficultyController`

### 14.3 Dades O Configuracions

Cal afegir o ampliar:

- dades d'arquetips d'enemic
- recompenses d'or
- multiplicadors per segment
- configuracio de botiga
- configuracio de poders divins

## 15. Com Afegir Aquests Elements A Unity

### 15.1 Enemics Nous

Per `Zombie` i `Ghost`, el flux recomanat es:

1. Crear un renderer procedural nou o ampliar `ProceduralEnemyRenderer`.
2. Donar-los una forma de `10x10` o `12x12`.
3. Assignar 3 a 5 colors per donar volum.
4. Crear variants d'animacio per canvi de forma, no nomes un bloc unic.
5. Afegir-los al pool de `RunManager` i a la generacio de cartes.

### 15.2 Cofres

Flux recomanat:

1. Crear un script `Chest.cs`.
2. Crear tres variants visuals procedurals: petit, mitja, rar.
3. Afegir un camp de recompensa en or.
4. Instanciar-los per segment segons la carta i el risc.

### 15.3 Botiga

Flux recomanat:

1. Crear un `ShopManager`.
2. Crear una UI simple amb 3 ofertes.
3. Connectar l'or de la run amb les compres.
4. Activar botiga a partir del segment definit.

### 15.4 Poders Divins

Flux recomanat:

1. Crear un `DivinePowerManager`.
2. Crear almenys `BenediccioDeVelocitat`.
3. Afegir cooldown, durada i UI minima.
4. Connectar el poder a les decisions o estadistiques de l'heroi.

## 16. Requisits Visuals Recomanats Per Als Nous Elements

Per mantenir coherencia amb l'estil actual i pujar-lo de nivell:

- moviment i representacio per punts
- multiple color, no silueta plana
- lectura clara de cap, cos, extremitats o massa principal
- subgraella interna visible
- contrast net amb el terra
- detall "HD pixel" a traves de mes punts i millor paleta, no a traves de filtres borrosos

### 16.1 Guia Visual Rapida

`Skeleton`

- os blanc, ombra grisa, ulls blau fred

`Bat`

- ales porpra fosc, cos negre, accents magenta

`Zombie`

- pell verd apagada, roba marron o gris, to pesat

`Ghost`

- blanc turquesa, nucli brillant, ombra translucida

`Chest`

- fusta, metall i brillantor d'or segons raresa

`Shop`

- simbol clar, paleta calida i lectura inmediata

## 17. Prioritat Recomanada D'Implementacio

Ordre recomanat:

1. Refactor d'estadistiques de l'heroi i combat base.
2. Afegir `Zombie` i `Ghost`.
3. Implementar or de run i recompenses d'enemics.
4. Afegir cofres.
5. Implementar botiga.
6. Implementar el primer poder divi.
7. Ajustar corba de dificultat per segment.

## 18. KPI De Playtesting Recomanats

Quan comencis a provar el balanc, revisa com a minim:

- vida mitjana restant al final de cada segment
- nombre de combats evitats vs acceptats
- or mitja acumulat per segment
- percentatge de compres a botiga
- percentatge de runs completades
- enemics que causen mes baixes

## 19. Resum Executiu

El vertical slice objectiu necessita:

- heroi mes robust i amb sistema de defensa
- quatre enemics diferenciats
- economia de run amb or
- botiga funcional
- almenys un poder divi
- corba de dificultat clara per segment

El projecte actual ja te una bona base per:

- moviment automatic
- combat simple
- generacio procedural de segments
- seleccio de cartes
- estil visual procedural per punts

El seguent pas natural es passar de prototype jugable a vertical slice balancejat i mes ric en sistemes.
