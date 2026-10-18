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
