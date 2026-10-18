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
