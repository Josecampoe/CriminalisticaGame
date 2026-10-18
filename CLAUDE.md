# El Caso 404 — Instrucciones del proyecto

Videojuego 2D de investigación criminal hecho en Unity (C#). Proyecto universitario de 5.º semestre de Ingeniería de Software. El jugador es un investigador que explora escenas, recolecta y analiza evidencias, interroga personajes controlados por IA y toma decisiones que llevan a uno de varios finales. Su asistente de IA, **Alex**, parece ayudar pero puede manipular la investigación.

Responde siempre en español. Los nombres de código (clases, métodos, variables) van en inglés; los textos que ve el jugador van en español.

## Alcance del prototipo (no ampliarlo sin que yo lo pida)
- 1 caso, 4–5 escenas 2D, 4 personajes + Alex
- Sistemas: exploración/interacción, inventario de evidencias, análisis, diálogos con IA, tablero de hipótesis, decisiones, 3–4 finales, guardado
- Finales: Verdadero, Engaño (acusa al equivocado por confiar en Alex), Incompleto, Secreto (Alex estaba involucrado)

## Stack
- Unity 2D (URP), C#, Input System nuevo, uGUI + TextMeshPro para la interfaz
- Datos del juego en ScriptableObjects; guardado en JSON (`Application.persistentDataPath`)
- IA conversacional vía HTTP (`UnityWebRequest` + async/await). Ver skill `dialogo-ia`
- Si la versión de Unity o algún paquete difiere de esto, revisa `Packages/manifest.json` y `ProjectSettings/ProjectVersion.txt` antes de asumir

## Estructura de carpetas
```
Assets/_Project/
  Scripts/{Core,Player,Interaction,Evidence,Dialogue,AI,Decisions,UI,Save}/
  Data/{Case,Characters,Evidence,Scenes,Endings}/   ← ScriptableObjects
  Scenes/  Prefabs/  Art/  UI/  Audio/
```
Todo el código propio vive bajo `Assets/_Project/`. No toques `Library/`, `Temp/`, `Logs/` ni paquetes de terceros.

## Regla de oro del diseño
**La verdad del caso vive en datos, no en la IA.** El culpable, la línea de tiempo, las evidencias y las condiciones de cada final están en ScriptableObjects. La IA solo da voz a los personajes y a Alex dentro de lo que cada uno sabe. Los finales se calculan con lógica determinista a partir del `GameState`, nunca con una respuesta del modelo.

## Arquitectura
- Sistemas desacoplados que se comunican con eventos (C# `event`/`Action` o canales de eventos en ScriptableObject), no con referencias cruzadas
- Un `GameState` único y serializable: evidencias encontradas/analizadas, flags de historia, confianza en Alex, decisiones tomadas, escena actual
- Nada de `FindObjectOfType`/`GameObject.Find` en tiempo de juego; referencias por `[SerializeField]`
- La UI solo lee estado y emite intenciones; no contiene lógica de juego

## Cómo trabajamos
- Antes de un cambio grande: explora, propón un plan corto y espera mi visto bueno
- Cambios pequeños y verificables; uno o dos sistemas por vez
- Después de escribir scripts: revisa que compile (consola de Unity vía MCP for Unity si está conectado) y dime qué debo configurar en el Inspector
- Si un script necesita objetos en escena, dime exactamente qué GameObject crear y qué arrastrar a cada campo, o créalo con las herramientas de MCP for Unity
- Explica brevemente el porqué de las decisiones: estoy aprendiendo y lo voy a sustentar ante el profesor
- Nunca pongas claves de API en el código, en ScriptableObjects ni en archivos que se suban a git

## Skills del proyecto
- `unity-csharp` — convenciones de código y patrones de Unity
- `diseno-ui` — identidad visual y construcción de interfaces
- `dialogo-ia` — personajes con IA, Alex y la conexión al modelo
- `evidencias-decisiones` — evidencias, hipótesis, flags y finales
- `/nueva-escena` — checklist para crear una escena jugable
- `/revisar` — revisión antes de commit o entrega
