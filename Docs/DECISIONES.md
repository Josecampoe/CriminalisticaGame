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
