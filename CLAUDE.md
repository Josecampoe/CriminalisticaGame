# El Caso 404 — Instrucciones del proyecto

Videojuego **3D** de investigación criminal en **primera persona**, hecho en Unity (C#). Proyecto universitario de 5.º semestre de Ingeniería de Software. El jugador es un investigador que explora escenas, recolecta y analiza evidencias, interroga personajes controlados por IA y toma decisiones que llevan a uno de varios finales. Su asistente de IA, **ALEX**, parece ayudar pero manipula la investigación.

**La historia completa está en `Docs/GUION.md`. Es la fuente de verdad**: personajes, evidencias, mentiras, decisiones y finales, con sus IDs. No inventes hechos ni cambies la historia sin preguntarme.

Responde siempre en español. Los nombres de código (clases, métodos, variables) van en inglés; los textos que ve el jugador van en español.

## Alcance (no ampliarlo sin que yo lo pida)
- 2 casos: "Apartamento 404" y "Protocolo 404"
- 3 escenas 3D por caso + 1 Comisaría compartida (7 escenas en total)
- 8 personajes + ALEX
- 7 finales: 3 en el caso 1 y 4 en el caso 2, incluido el secreto con 2 variantes
- Sistemas: exploración en primera persona, inspección de objetos, expediente de evidencias, análisis, diálogos con IA, panel de ALEX con Aceptar/Cuestionar, tablero de hipótesis, decisiones, acusación, finales y guardado

## Stack
- Unity 3D con URP (plantilla Universal 3D), C#, Input System nuevo
- Controlador en primera persona con `CharacterController`; Cinemachine si está instalado
- Escenas armadas primero con ProBuilder (greybox) y luego con assets low-poly
- Interfaz con uGUI + TextMeshPro
- Datos del juego en ScriptableObjects; guardado en JSON (`Application.persistentDataPath`)
- IA conversacional vía HTTP (`UnityWebRequest` + async/await). Ver skill `dialogo-ia`
- Antes de asumir versiones o paquetes, revisa `Packages/manifest.json` y `ProjectSettings/ProjectVersion.txt`

## Estructura de carpetas
```
Assets/_Project/
  Scripts/{Core,Player,Interaction,Evidence,Dialogue,AI,Decisions,UI,Save}/
  Data/{Cases,Characters,Evidence,Scenes,Decisions,Endings}/   ← ScriptableObjects
  Scenes/  Prefabs/  Art/{Models,Materials,Textures}/  UI/  Audio/
Docs/GUION.md
```
Todo el código propio vive bajo `Assets/_Project/`. No toques `Library/`, `Temp/`, `Logs/` ni paquetes de terceros.

## Regla de oro del diseño
**La verdad del caso vive en datos, no en la IA.** Culpables, líneas de tiempo, evidencias y condiciones de los finales están en ScriptableObjects, generados a partir de `Docs/GUION.md`. La IA solo da voz a los personajes y a ALEX dentro de lo que cada uno sabe. Los finales se calculan con lógica determinista a partir del `GameState`, nunca con una respuesta del modelo.

## Arquitectura
- Sistemas desacoplados que se comunican con eventos (C# `event`/`Action` o canales de eventos en ScriptableObject), no con referencias cruzadas
- Un `GameState` único y serializable con: caso actual, evidencias encontradas y analizadas, flags, contradicciones de ALEX por caso, `alexTrust`, decisiones tomadas y escena actual
- Nada de `FindObjectOfType`/`GameObject.Find` en tiempo de juego; referencias por `[SerializeField]`
- La UI solo lee estado y emite intenciones; no contiene lógica de juego

## Cómo trabajamos
- Antes de un cambio grande: explora, propón un plan corto y espera mi visto bueno
- Cambios pequeños y verificables; uno o dos sistemas por vez. Primero el caso 1 completo, después el caso 2
- Después de escribir scripts: revisa que compile (consola de Unity vía MCP for Unity si está conectado) y dime qué debo configurar en el Inspector
- Si un script necesita objetos en escena, dime exactamente qué GameObject crear y qué arrastrar a cada campo, o créalo con MCP for Unity
- Explica brevemente el porqué de las decisiones: estoy aprendiendo y lo voy a sustentar ante el profesor
- Nunca pongas claves de API en el código, en ScriptableObjects ni en archivos que se suban a git

## Skills del proyecto
- `unity-csharp` — convenciones de código y patrones de Unity 3D
- `diseno-ui` — identidad visual, HUD en primera persona e interfaces
- `dialogo-ia` — personajes con IA, ALEX y la conexión al modelo
- `evidencias-decisiones` — evidencias, hipótesis, flags y finales
- `/nueva-escena` — checklist para crear una escena jugable
- `/revisar` — revisión antes de commit o entrega
