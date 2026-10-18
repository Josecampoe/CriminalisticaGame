# Decisiones técnicas — El Caso 404

Registro de decisiones tomadas durante el desarrollo, con su motivo. Cuando el guion no dice algo, aquí queda qué se eligió y por qué.

## Fase A — Preparación

| # | Decisión | Motivo |
|---|---|---|
| A1 | Se quitaron `com.unity.ai.inference` y `com.unity.pipeline` del manifest; se mantuvo `com.unity.ai.assistant`. | Son experimentales y no los usa el juego. Alargan la compilación. El asistente se deja por si lo usas en el editor. |
| A2 | `com.unity.test-framework` 1.8.0 se agregó como dependencia directa. | Las pruebas EditMode lo necesitan. Es la versión que Unity 6.6 ya traía incluida. |
| A3 | **No se instaló ProBuilder**; el greybox se arma con primitivas de Unity (cubos y cilindros). | No hay forma de verificar sin conexión qué versión de ProBuilder es compatible con Unity 6.6, y un número de versión equivocado rompe la resolución de paquetes. Las primitivas cumplen la misma función de greybox y se reemplazan igual por modelos. |
| A4 | Tampoco se instaló Cinemachine. | El acercamiento al rostro del NPC se hace interpolando la cámara del jugador. Es menos código que configurar Cinemachine. |
| A5 | Graphics → Default Render Pipeline = `PC_RPAsset` (lo asigna `ProjectSetup`). | Evita que algún nivel de calidad o herramienta vuelva en silencio a Built-in. |
| A6 | Se crearon las capas `Interactable` (8) e `Inspection` (9). | El raycast de interacción solo mira la capa `Interactable`. `Inspection` queda para el objeto que se gira en el modo inspección. |
| A7 | Los TMP Essential Resources se extrajeron del `.unitypackage` del paquete ugui conservando sus GUID. | `AssetDatabase.ImportPackage` es asíncrono y no termina en modo batch con `-quit`. El resultado es idéntico a importarlos desde el menú. |
| A8 | Tres asmdefs: `Caso404.Runtime` (juego), `Caso404.Editor` (herramientas) y `Caso404.Tests.EditMode` (pruebas). | Compilación más rápida, el código de editor no entra en la build y las pruebas quedan aisladas. |
| A9 | Fuente: LiberationSans (la que trae TMP) en lugar de Special Elite, Inter o IBM Plex. | Sin conexión no se pueden descargar las fuentes de Google Fonts. El `UITheme` tiene campos para cambiarlas después. |

## Fase B — Datos

| # | Decisión | Motivo |
|---|---|---|
| B1 | Las mentiras son assets propios (`LieData`), no entradas dentro del personaje. Esto cambia lo que decía el plan. | Así se pedía en la fase B. Además, una sola fuente define qué refuta cada mentira: la evidencia no repite su columna "Refuta". El panel de ALEX, los diálogos y el validador leen lo mismo. |
| B2 | Las mentiras de ALEX son `LieData` con `IsAlexLie = true` y un disparador (al encontrar o al analizar una evidencia). | Las "afirmaciones" de ALEX funcionan igual que las mentiras de los personajes: se refutan presentando una evidencia. |
| B3 | Los hechos (`f_`) son entradas `{id, texto}` dentro de cada `CharacterProfile`. | Son muchos y solo los usa su dueño. Los IDs se comprueban en las pruebas. |
| B4 | Los campos de los SO son privados con propiedades públicas de solo lectura y un `internal set`. Solo el generador de Editor y las pruebas pueden asignarlos (`InternalsVisibleTo`). | Cumple la regla "los SO son de solo lectura en juego" sin perder un generador reproducible. |
| B5 | IDs de personajes: `carlos`, `laura`, `hernan`, `andres`, `alex`. | El guion no les da ID. Son cortos y estables. |
| B6 | La Comisaría del caso 1 tiene el id `c1_s4` y la escena de Unity `Comisaria`. | El guion la llama `c1_s4`. La escena de Unity se comparte con el caso 2. |
| B7 | `ev_c1_camara_original` es de tipo `Unlock`: aparece sola al analizar `ev_c1_dvr`. | El guion dice que el DVR "desbloquea" la grabación original. |
| B8 | El overol tiene un detalle oculto (vidrio en la manga) que se descubre al girarlo, y no se puede analizar sin haberlo visto. | GUION §2: algunas evidencias solo revelan su detalle clave al girarlas. |
| B9 | `dec_c1_s1` A: el jugador elige después entre las escenas pendientes (`LetPlayerChooseNextScene`). | El guion no dice adónde lleva A, pero exige completar s2 y s3 antes de la Comisaría. |
| B10 | Reacción de Laura al refutarle `lie_c1_laura_notoque`: *"Yo... solo quería ayudarla. No me pregunte eso todavía."* Reacción de Andrés al refutarle `lie_c1_andres_casa`: entrega el celular *"para que vea"*. | El guion no trae esas frases. Se escribieron sin agregar hechos nuevos: Laura sigue ocultando el cuaderno (su secreto) y Andrés hace lo que dice el guion. |
| B11 | Las frases "Presionar" del guion son la reacción del personaje cuando se le refuta su mentira principal. | Esas frases son confesiones. Si se dijeran sin pruebas, el jugador no tendría que deducir. |
| B12 | El celular de Andrés solo se entrega si su mentira `lie_c1_andres_casa` se refuta con una evidencia (llavero, overol o grabación original), no con el testimonio de Hernán. | GUION: lo entrega si se le presenta "al menos una prueba sólida". Las tres evidencias que refutan esa mentira son pruebas sólidas. |
| B13 | Las frases de respaldo de ALEX para el diálogo libre se guardan en `CaseData.AlexLines`, tomadas de los ejemplos del GUION §3.5. | Dependen del caso, y ALEX se comparte entre los dos casos. |

## Fase C — Núcleo

| # | Decisión | Motivo |
|---|---|---|
| C1 | Los servicios (`GameStateService`, `EvidenceService`, `AlexService`, `DecisionService`, `HypothesisService`, `AccusationService`, `SceneFlowService`, `SaveService`) son clases C# normales, no MonoBehaviour. Un `GameContext` en la escena los crea y los reparte. | Se pueden probar en EditMode sin escenas. Es la base para que las pruebas recorran los finales. |
| C2 | Solo `GameStateService` modifica el `GameState`, y avisa con eventos C# (`Changed`, `TrustChanged`, `FlagAdded`...). Para avisos globales entre escenas también hay canales en ScriptableObject (`GameEventChannel`, `StringEventChannel`). | Sistemas desacoplados, como pide CLAUDE.md. |
| C3 | Aceptar una afirmación de ALEX suma +5 a `alexTrust` una sola vez. Seguir su recomendación en una decisión suma lo que diga la opción (+10 en `dec_c1_s1` A). | El guion dice "+5 a +10". +5 para lo pequeño y +10 para la decisión grande. |
| C4 | Una afirmación de ALEX se puede cuestionar aunque antes se haya aceptado. Cuestionar con una evidencia que no la refuta no cambia nada: ALEX responde *"Esa evidencia no contradice mi análisis, detective."* | A veces la prueba aparece más tarde (la grabación original en s3 refuta el ERROR 404 de s1). Castigar los intentos fallidos haría que el jugador adivine en vez de deducir. |
| C5 | Responder mal una hipótesis está permitido y la respuesta se puede cambiar. La decisión de cierre se habilita cuando todas las preguntas tienen respuesta. Acertar activa los flags de la pregunta. | El guion solo dice qué pasa al acertar. Así no se bloquea el avance. |
| C6 | Al acusar, el final se guarda también como flag (`end_c1_verdadero`, etc.). | GUION §2: los `end_c1_*` pasan al caso 2. Así lo leen las mismas condiciones que el resto de flags. |
| C7 | Una prueba sólida solo cuenta si está analizada, y en la acusación no se puede repetir una prueba. | Evita acusaciones con evidencias que el jugador no entendió. |
| C8 | `SaveService` escribe primero un archivo `.tmp` y luego lo renombra. Rechaza partidas de una versión más nueva que la del juego. | Si el juego se cierra a mitad de un guardado, la partida no queda corrupta. `saveVersion` deja lugar para migraciones. |

## Fase D — Jugador en primera persona

| # | Decisión | Motivo |
|---|---|---|
| D1 | Los controles se definen en código dentro de `InputReader` (Input System), no en el asset `InputSystem_Actions` de la plantilla. | Hay un solo lugar donde ver y cambiar todos los controles, y no hace falta enlazar referencias en el Inspector. Teclas: WASD/flechas para moverse, mouse para mirar, E interactuar, Tab expediente, Q ALEX, H hipótesis, Esc cerrar. También funciona con mando. |
| D2 | Velocidad al caminar: 2.2 m/s. No se puede correr ni saltar. | La skill `unity-csharp` lo pide para un juego de investigación. |
| D3 | El resaltado tiñe el color del objeto con `MaterialPropertyBlock` (ámbar si es nuevo, blanco si ya se revisó), sin shader de contorno. | Funciona con cualquier material URP Lit y no duplica materiales. Más adelante se puede cambiar por un contorno sin tocar el resto. |
| D4 | El modo inspección copia el objeto a un "estudio" lejos de la escena (y = −500) y lo filma con una cámara propia en una `RenderTexture` que la UI muestra. | El objeto nunca atraviesa paredes y el fondo de la escena queda oscurecido detrás de la UI. Si la evidencia tiene `InspectPrefab`, se usa ese modelo en vez de la copia. |
| D5 | El detalle oculto se revela al girar el objeto 150° acumulados. | Obliga a girarlo de verdad (GUION §2) sin exigir una orientación exacta. |
| D6 | Los objetos interactivos solo emiten intenciones a través de `PlayerContext` (`InspectRequested`, `TalkRequested`, `SceneCloseRequested`). | Los objetos no conocen la UI ni los controladores, como pide la arquitectura. |
| D7 | El jugador se bloquea escuchando un canal `BoolEventChannel` (`evt_ui_modal`) que emite el `UIManager`. | Ni el jugador conoce la UI ni la UI conoce al jugador. |
| D8 | `GameContext` (con `[DefaultExecutionOrder(-100)]`) crea los servicios en cada escena. Entre escenas, el estado pasa por el archivo de guardado. `Start New Game` está activado en c1_s1. | Es lo más simple mientras solo existe una escena. La escena `Bootstrap` persistente queda pendiente para cuando haya varias. |
| D9 | En esta fase también entraron `DialogueService`, la capa de IA (`IAiClient`, `OfflineAiClient`, `HttpAiClient`, `AiReplyValidator`, `AiSettings`) y sus pruebas. | `GameContext` y el NPC los necesitan para compilar. |
| D10 | Descubrir mentiras, revelar secretos y entregar evidencias en el diálogo lo deciden reglas fijas con los datos. La IA solo pone la voz, y su respuesta se valida antes de usarla. | Regla de oro de CLAUDE.md. |

## Fase E — UI básica

| # | Decisión | Motivo |
|---|---|---|
| E1 | Cada pantalla se arma por código en su `Build()` con `UIFactory` y los tokens del `UITheme`. No hay prefabs de UI. | Se puede generar y enlazar todo desde un script de Editor en modo batch, sin trabajo manual en el Inspector. Cambiar el tema cambia todas las pantallas. Más adelante cada vista se puede pasar a prefab sin tocar su lógica. |
| E2 | La skill `diseno-ui` pide describir la jerarquía de cada pantalla y esperar tu visto bueno. Como esta sesión es autónoma, la jerarquía queda escrita abajo (E10) para que la revises. | Pediste no hacer preguntas. |
| E3 | Fuente única LiberationSans SDF en los tres roles (títulos, cuerpo, mono). | Ver A9. El `UITheme` ya tiene los tres campos para Special Elite, Inter y IBM Plex Mono. |
| E4 | En el expediente, el texto cian de ALEX sobre papel se oscurece un 55 %. | El cian #4FD1C5 sobre papel #E9E2D0 no llega al contraste mínimo de 4.5:1 que pide la skill. |
| E5 | El tablero no dice si una respuesta es correcta: solo "Respuesta registrada". | Si dijera "correcto/incorrecto", el jugador probaría opciones hasta acertar en vez de deducir. |
| E6 | La decisión que dispara un secreto (el cuaderno de Laura → `dec_c1_cuaderno`) se abre al cerrar la conversación, no en medio de ella. | Así el jugador alcanza a leer la frase de Laura antes de que la pantalla de decisión la tape. |
| E7 | Los atajos (Tab, Q, H) solo funcionan si no hay otro panel abierto, o si el panel abierto es el mismo (entonces lo cierran). | Evita que escribir una pregunta con Q o Tab abra otro panel. |
| E8 | Panel de ALEX: tres preguntas rápidas con las frases de ejemplo del GUION §3.5. El tono se muestra en el estado ("modo formal", "respuesta limitada"). Con confianza menor que 30, el panel hace glitch cada 3–6 s. | GUION §1: con confianza baja ALEX se vuelve evasivo y su panel muestra glitches. No se muestra el número de confianza porque el guion no lo hace visible al jugador. |
| E9 | Si la siguiente escena no está en Build Settings, aparece "Fin de la demo" con la partida guardada y botones para reiniciar o salir. | c1_s2 y c1_s3 todavía no existen. |

### E10 — Jerarquía de cada pantalla (qué ve primero el jugador y cuál es la acción principal)
- **HUD**: retícula al centro (ámbar si apunta a algo nuevo) → prompt "E — ..." debajo → nombre de la escena arriba (se desvanece) → contador de evidencias sin analizar arriba a la derecha → atajos abajo a la izquierda → avisos abajo al centro.
- **Inspección**: objeto 3D grande al centro → nombre arriba → descripción y detalle oculto (ámbar) abajo → **Guardar en expediente** (principal, ámbar) y Soltar.
- **Expediente**: fichas a la izquierda (nombre + estado: NUEVA, ANALIZADA o "Sin valor como prueba") → ficha ampliada a la derecha → **Analizar** (principal, ámbar) → informe con título rojo. Si lo entrega ALEX, el texto va en cian oscuro.
- **Diálogo**: panel inferior → nombre (ámbar) y rol → texto con efecto de escritura → botones rápidos (Sobre el crimen, Sobre otra persona, **Presentar evidencia**, Presionar, Despedirse y, cuando aplica, Hablarle con calma) → campo para escribir la pregunta.
- **Panel de ALEX**: lateral derecho, borde cian → última frase de ALEX → preguntas rápidas → tarjetas de observaciones con **Cuestionar** (rojo) y Aceptar (cian). Las refutadas aparecen tachadas.
- **Tablero de hipótesis**: preguntas sobre papel → opciones (bloqueadas en gris con lo que falta analizar) → Responder → **Tomar la decisión** (rojo, abajo a la derecha).
- **Decisión**: pregunta grande → recomendación de ALEX en cian → opciones grandes → confirmación si es irreversible.

## Fase F — Escena c1_s1 "Apartamento 404"

| # | Decisión | Motivo |
|---|---|---|
| F1 | La escena la arma por completo `Apartment404SceneBuilder` (menú *Caso404/Construir escena c1_s1*). Al volver a ejecutarlo, la escena se reemplaza. | Es reproducible y no hubo que editar el `.unity` a mano. Ojo: si la modificas a mano en el Editor y vuelves a ejecutar el builder, se pierden tus cambios. |
| F2 | Distribución: sala de 8×6 m, balcón al norte (5×2 m, con la ventana rota y una puerta abierta al lado) y pasillo al sur (10×2 m) con la puerta del 403, la cámara y el ascensor. | Corresponde al ambiente del guion: sala desordenada, ventana del balcón rota, escritorio vacío y Laura en el pasillo. |
| F3 | Ubicación de las evidencias: ventana (pared norte), cerradura (puerta de entrada), huella y colilla (balcón), teléfono (delante del sofá), escritorio con marca de polvo, cargador y fotos con el cuaderno rojo (pared este), cámara (pasillo, sobre la puerta del 404). | Todas se alcanzan desde la altura de los ojos y quedan dentro del alcance del rayo (2.5 m). |
| F4 | El rayo de interacción también choca con la capa Default. | Así no se puede interactuar a través de paredes. |
| F5 | Para cerrar la escena se usa el ascensor del pasillo ("Repasar el caso y decidir") o la tecla H. | El jugador necesita un punto claro donde terminar. |
| F6 | Iluminación en tiempo real: luz direccional gris de mañana, lámpara cálida en la sala, fluorescente frío en el pasillo y niebla ligera. Sin iluminación horneada. | Hornear en modo batch es lento y el greybox va a cambiar. Hornearla queda pendiente para cuando llegue el arte. |
| F7 | `SampleScene` se quitó de Build Settings, pero no se borró. Apartamento404 es la escena 0. | La regla era no borrar archivos que no creé yo. |
| F8 | Se agregó una prueba PlayMode de humo (`Apartment404SmokeTest`) que recorre la escena hasta `dec_c1_s1`. Necesita gráficos: no corre con `-nographics`. | Es la única forma de comprobar la escena sin abrir el Editor. El modo inspección usa una RenderTexture, que no se puede crear sin GPU. |
| F9 | `ScenePreviewCapture` guarda capturas de la escena en `Logs/preview_*.png`. | Sirve para revisar el greybox sin abrir el Editor. |

---

# Sesión 2 (fases G a L)

## Infraestructura común

| # | Decisión | Motivo |
|---|---|---|
| S1 | El builder de c1_s1 se convirtió en un kit reutilizable, `SceneKit`: materiales, cajas, paredes con huecos para puertas y ventanas, evidencias, personajes, jugador, UI y luces. Cada escena tiene un builder corto que lo usa. El menú *Caso404 → Construir todo* regenera los datos y las 5 escenas. | No repetir unas 400 líneas por escena, y que todas las escenas tengan exactamente la misma UI y el mismo jugador. |
| S2 | El commit de G incluye todo el código nuevo de juego (acusación, final, fundido, menú y flujo de escenas), aunque varias piezas sean de las fases I y J. Los commits H, I y J agregan las escenas. | El `GameUIController` necesita esas vistas para compilar, y así cada commit compila. |
| S3 | Todas las escenas tienen `Start New Game = false`: el estado llega por el guardado. El menú (*Nueva partida*) crea un `GameState` limpio y lo guarda antes de cargar c1_s1. | Así el estado pasa de una escena a otra sin una escena Bootstrap persistente. Para probar una escena sola desde el Editor, sin pasar por el menú, se usa la última partida guardada. |
| S4 | Al pasar de escena, el `currentSceneId` se actualiza antes de guardar. | Así *Continuar* abre la escena a la que ibas, no la que ya terminaste. |
| S5 | Las escenas sin decisión de cierre (c1_s2 y c1_s3) se terminan desde el tablero con **"Terminar la escena"**. Luego el jugador elige entre las escenas pendientes, y la Comisaría solo aparece cuando s2 y s3 están completas. | GUION §3.4: hay que completar s2 y s3 antes de ir a la Comisaría. |

## Fase G — Café La Esquina

| # | Decisión | Motivo |
|---|---|---|
| G1 | Café de noche (10×7 m): barra con cafetera y botellas, televisor con el "partido" en la pared este, 4 mesas y una vitrina a la calle. | Corresponde al ambiente del guion. |
| G2 | Ubicación de las evidencias: el recibo junto al datáfono, sobre la barra; el video del café en un monitor de seguridad al final de la barra; la servilleta en la mesa de Carlos. | Las tres se ven desde la entrada. |
| G3 | `CharacterNpc` revisa la condición de presencia de `SceneData` al iniciar. Carlos tiene la condición "no `flag_c1_carlos_detenido`" en el café, así que si fue detenido no aparece (sí aparece en la Comisaría). | GUION §3.4. |
| G4 | Con `flag_c1_h_llave`, Hernán y Andrés tienen el botón **"Preguntar por la llave maestra"**. Sus frases las escribí yo, porque el guion no las trae: Hernán indica que la llave está en el llavero de mantenimiento, con registro de quién la saca; Andrés le resta importancia. | GUION §3.4 pide la opción, pero no da texto. Las frases no agregan hechos nuevos: solo apuntan al registro del llavero (`ev_c1_llavero`). |

## Fase H — Portería y sótano

| # | Decisión | Motivo |
|---|---|---|
| H1 | Distribución: portería (con la caseta y la chaqueta de Hernán en su silla), pasillo de sótano con casilleros, administración (DVR en la estantería), mantenimiento (tablero de llaves con la hoja de control) y parqueadero subterráneo con carros y el contenedor (portátil encima). | Las 4 zonas del guion. |
| H2 | El overol y las cenizas están en el casillero abierto de Andrés y en una caneca al lado. Solo se activa uno, según `flag_c1_carlos_detenido`. | GUION §3.4: si Carlos fue detenido, Andrés quema el overol. |
| H3 | La grabación original no tiene objeto en la escena: aparece sola en el expediente al analizar el DVR, con aviso en el HUD. | Es la mecánica del ERROR 404: el original se consigue por otra vía. |

## Fase I — Comisaría

| # | Decisión | Motivo |
|---|---|---|
| I1 | Oficina con tablero de corcho (abre el tablero con **todas** las hipótesis del caso), pantalla de ALEX (abre su panel) y el "Expediente para la Fiscalía" (abre la acusación). La sala de interrogatorio tiene a los 4 personajes sentados alrededor de la mesa. | GUION §2: tablero, interrogatorio y acusación en la Comisaría. |
| I2 | El celular de Andrés solo se entrega en la Comisaría (`SceneId = c1_s4`). Si su mentira ya se descubrió en el sótano, basta con volver a presentarle una prueba sólida en la Comisaría. | GUION §3.4: "Aquí se obtiene `ev_c1_celular_andres`". |
| I3 | `dec_c1_cuaderno` funciona en cualquier escena donde esté Laura: en c1_s1 o en la Comisaría si no se resolvió antes. | El guion la ata a que Laura entregue el cuaderno, no a un lugar. |
| I4 | Acusación: se elige uno de los 4 sospechosos y exactamente 3 evidencias analizadas, con confirmación. El final lo calcula `AccusationService`. | GUION §3.6. |
| I5 | El final verdadero revela `f_c1_andres_pago` (`EndingData.RevealsFactIds`). | GUION: Andrés lo confiesa solo en el final verdadero. |
| I6 | La escena Comisaria apunta a `c1_s4`. Para el caso 2 habrá que elegir el `SceneData` según el caso actual. | Pendiente para el caso 2 (anotado en el reporte). |

## Fase J — Flujo completo

| # | Decisión | Motivo |
|---|---|---|
| J1 | Menú principal: título con efecto de máquina de escribir, **Nueva partida**, **Continuar** (solo si hay partida guardada) y **Salir**. Es la escena 0 de Build Settings. | Skill diseno-ui, pantalla 1. |
| J2 | Hay un fundido a negro de 0.6 s entre escenas (`ScreenFader`, en un Canvas aparte por encima de todo). | Transición simple que oculta la carga. |
| J3 | Pantalla de final: título y texto del guion (con la variante del final incompleto que corresponda) y un resumen: a quién acusaste, pruebas sólidas, contradicciones de ALEX (x de 5), evidencias encontradas y qué prueba sólida se te escapó. Después viene "Tres meses después..." con efecto de máquina de escribir, y un aviso de que el caso 2 aún no está disponible, con vuelta al menú. | Skill diseno-ui, pantalla 9: "lo que descubrió y lo que se le escapó". |
| J4 | Si se entra a la Comisaría con el caso ya cerrado (con *Continuar*), aparece "Caso cerrado" con el final obtenido y vuelta al menú. | Evita acusar dos veces. |

## Fase K — Pruebas del caso completo

| # | Decisión | Motivo |
|---|---|---|
| K1 | `Case1FullFlowTests` (PlayMode) tiene 4 recorridos desde el menú: **verdadero** (investigando todo, con celular y análisis profundo), **verdadero con Carlos detenido**, **incompleto** (sin cuestionar a ALEX) y **engaño** (siguiendo a ALEX). Reemplaza a la prueba de humo de c1_s1. | Pediste recorrer los 3 finales. Se agregó el caso de Carlos detenido porque es el camino mínimo del GUION §5. |
| K2 | Las pruebas usan las mismas entradas que el jugador: `PlayerContext` para inspeccionar, hablar y acusar; el modo inspección para guardar; los botones de las ventanas, por su texto, para elegir. Solo dos acciones van directo al servicio: girar el objeto, que marca el detalle oculto, y las respuestas del tablero. | El mouse no se puede simular en batch. |

## Fase L — Caso 2: datos

| # | Decisión | Motivo |
|---|---|---|
| L1 | `Case2DataBuilder` genera los assets del caso 2 a partir del GUION §4: 15 evidencias, 11 mentiras (5 de ALEX), 4 personajes (Elena como aliada), 3 hipótesis, 3 decisiones, 4 finales y 4 escenas. También agrega el caso 2 a la `GameDatabase`. Regenerar el caso 1 ya no borra el caso 2. | Mismo enfoque que el caso 1. |
| L2 | Hay refutaciones que exigen un flag, definidas por cada par mentira-evidencia (`EvidenceRequirement`). El portátil de Tomás solo refuta abierto con LUCERO (`flag_c2_directiva`). La cámara del parqueadero solo identifica el carro de Valeria tras el laboratorio externo. | GUION §4.4–4.5. |
| L3 | Agregué dos flags técnicos que el guion no nombra: `flag_c2_placas_legibles` (laboratorio externo) y `flag_c2_ines_testigo_debil` (opción B con Inés). | Hacen falta para que esas reglas sean deterministas. El guion habla de "versión débil", y esa versión no cuenta como prueba sólida. |
| L4 | `flag_c2_ines_testigo` cuenta como prueba sólida aunque no se elija entre las 3 pruebas (`CaseData.SolidProofFlags`). | GUION §4.6: "cuenta como prueba". |
| L5 | Al refutar la mentira de Inés se abre `dec_c2_ines` (`LieData.TriggersDecision`). La opción A entrega la copia espejo solo si no hay `flag_c2_tiempo_perdido`. | GUION §4.4. |
| L6 | El secreto de Sebastián (Kairos Labs) se revela al presentarle el chat analizado (`SecretEntry.RevealedByEvidence`) y le da al jugador la coartada del bar. | GUION §4.3: "Lo prueba `ev_c2_chat_kairos`". |
| L7 | Frases que escribí porque el guion no las trae, sin hechos nuevos: <ul><li>Sebastián al refutarle "no hablé con Tomás": *"Está bien, hablé con él. ¿Y qué? Eso no me convierte en asesino."*</li><li>Sebastián al refutarle "me fui a las 7": *"Me fui a las nueve y veinte, en la moto."*</li><li>Valeria reacciona con sus frases de respaldo.</li><li>La recomendación de ALEX en `dec_c2_s1` usa su propia mentira sobre la nota.</li><li>Elena tiene dos temas extra que repiten sus hechos del guion (el correo y el primer nombre de ALEX).</li></ul> | El guion no tiene esas líneas. |
| L8 | Las actitudes de Elena según el final del caso 1 quedan como datos (`AttitudeVariants`). Falta mostrarlas, el −5 de confianza por `flag_c1_cuaderno` y la regla de que solo coopera tras ver la USB si el caso 1 terminó en engaño. | Pendiente para la escena de la Comisaría del caso 2. |

## Fase L — Caso 2: c2_s1 y paso entre casos

| # | Decisión | Motivo |
|---|---|---|
| L9 | c2_s1 "Sala de servidores B": sala fría con tres filas de racks con luces azules, puerta de seguridad abierta (con lector: registro de la puerta vía ALEX), sensor de humo en el techo, panel rojo de gas, el cuerpo de Tomás cubierto (con la USB en la mano) y una taza con la nota en el escritorio técnico. Se cierra en el ascensor del piso 12. | GUION §4.4 y §7: sin personajes, solo comentarios de ALEX. |
| L10 | Después del final del caso 1 y de "Tres meses después...", aparece **"Comenzar «Protocolo 404»"**, que carga c2_s1 conservando flags, finales y `alexTrust`. | GUION §2: el caso 2 siempre empieza y `alexTrust` pasa al caso 2. |
| L11 | Si `alexTrust` es menor que 30 al empezar el caso 2, la intro usa la línea de ALEX *"Noto que has dejado de confiar en mí..."*. | GUION §4.5. |
| L12 | La Comisaría es una sola escena de Unity para los dos casos. `GameContext` elige `c1_s4` o `c2_s4` según el caso en curso (`_alternativeScenes`), y cada personaje solo aparece si el `SceneData` activo lo lista. El elenco del caso 2 (Sebastián, Valeria, Inés y Elena) ya está colocado. | GUION §2: espacio compartido. |
| L13 | En el final secreto, después del texto del interrogatorio a ALEX se abre `dec_c2_alex`. Su resultado ("Desconexión" o "Archivo encontrado") se muestra antes de los créditos "ERROR 404: CONFIANZA NO ENCONTRADA.". El flujo está listo, pero solo se podrá jugar cuando existan c2_s2 y c2_s3. | GUION §4.6. |
| L14 | La prueba del final verdadero del caso 1 sigue hasta el caso 2: comprueba que pasan los flags, el final y `alexTrust`, recorre c2_s1 (5 evidencias, 2 contradicciones de ALEX, hipótesis) y toma `dec_c2_s1`. | Así se comprueba la conexión entre casos. |

## Fase L (continuación) — c2_s2, c2_s3 y caso 2 jugable completo

| # | Decisión | Motivo |
|---|---|---|
| L15 | c2_s2 "Piso ejecutivo": tres oficinas con frente de vidrio (Tomás, Valeria y Sebastián), un ventanal con luces de la ciudad y el lector de accesos junto al ascensor (registro de la tarjeta). Valeria está de pie en su oficina; Sebastián, sentado en su escritorio (no aparece si hay `flag_c2_tiempo_perdido`). | GUION §4.4. |
| L16 | c2_s3 "Caseta y parqueadero": caseta con monitores, termo, el servidor de copias espejo y el casillero de Inés (con el sobre), y la cámara en el parqueadero. La copia espejo no es un objeto de la escena: la entrega Inés si la proteges y aún existe. | GUION §4.4: la consigue `dec_c2_ines` A. |
| L17 | Las actitudes de Elena salen como saludo al hablarle. Si guardaste el cuaderno fuera del sistema, su frase baja `alexTrust` en 5 una sola vez (se marca con un flag `att_elena_n`). Las actitudes que no son frases del guion van entre paréntesis, como indicación. | GUION §4.3. |
| L18 | Nuevas pruebas PlayMode del caso 2: **final secreto** (con `flag_c1_pista_nx`, hasta `dec_c2_alex`, el resultado y los créditos) y **engaño con tiempo perdido** (Sebastián no está en su oficina y la copia espejo ya no existe). Empiezan desde una partida guardada al terminar el caso 1, con *Continuar*. | Comprueba el caso 2 completo sin repetir el caso 1. |
| L19 | Pendiente: Elena "solo coopera si le muestras `ev_c2_usb_elena`" cuando el caso 1 terminó en engaño. Hoy solo cambia su saludo. | Hace falta una regla de diálogo nueva (bloquear temas hasta presentar una evidencia). |
