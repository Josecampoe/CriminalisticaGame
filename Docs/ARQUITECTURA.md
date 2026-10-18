# Arquitectura — El Caso 404

Guía para entender y sustentar cómo está construido el juego. Para el detalle de cada decisión, consulta `Docs/DECISIONES.md`.

## 1. La regla de oro

> **La verdad del caso vive en datos, no en la IA.**

Culpables, evidencias, mentiras, qué refuta cada mentira, condiciones de las hipótesis y de los finales: todo está en **ScriptableObjects** generados desde `Docs/GUION.md`. La IA (o las frases de respaldo sin conexión) solo **pone la voz** de los personajes. Que una mentira quede descubierta, que un secreto se revele o qué final te toca lo deciden **funciones deterministas** con pruebas automáticas.

## 2. Capas

```
┌─────────────────────────────────────────────────────────────────────┐
│ ESCENA (Unity)                                                      │
│  Player (FirstPersonController, PlayerInteractor, InspectionController)
│  Objetos: EvidencePickup, CharacterNpc, SceneCloseTrigger, AccusationTrigger
│  UI: HUDView, JournalView, DialogueView, AlexPanelView, HypothesisView,
│      DecisionView, AccusationView, EndingView, MessageView, MainMenuView
└───────────────▲───────────────────────────────┬─────────────────────┘
                │ eventos (C# event / canales SO) │ intenciones (llamadas a servicios)
┌───────────────┴───────────────────────────────▼─────────────────────┐
│ SERVICIOS (clases C# puras, se crean en GameContext)                │
│  GameStateService  EvidenceService  AlexService  DialogueService    │
│  DecisionService   HypothesisService  AccusationService             │
│  SceneFlowService  SaveService      EndingEvaluator (estático)      │
└───────────────▲───────────────────────────────┬─────────────────────┘
                │ lee                             │ modifica (solo GameStateService)
┌───────────────┴─────────────┐   ┌──────────────▼──────────────────────┐
│ DATOS (ScriptableObjects)   │   │ ESTADO (GameState, serializable)    │
│ CaseData, SceneData,        │   │ IDs de evidencias encontradas y      │
│ EvidenceData, LieData,      │   │ analizadas, hechos, mentiras         │
│ CharacterProfile, AlexProfile│  │ expuestas, flags, alexTrust,         │
│ HypothesisData, DecisionData,│  │ contradicciones por caso, decisiones,│
│ EndingData, FlagData         │  │ acusaciones → save.json              │
└─────────────────────────────┘   └─────────────────────────────────────┘
```

- **Datos** (`Scripts/Core/Data/`): son de solo lectura en el juego. Sus campos tienen `internal set`, así que solo los asignan el generador de Editor y las pruebas.
- **Estado** (`Scripts/Core/GameState.cs`): guarda solo IDs y listas, para poder serializarlo con `JsonUtility`. **Solo `GameStateService` lo modifica**, y avisa de cada cambio con eventos.
- **Servicios**: contienen las reglas. No son MonoBehaviour, así que se prueban en EditMode sin escenas.
- **Escena**: los objetos emiten intenciones a través de `PlayerContext` (inspeccionar, hablar, acusar). La UI solo muestra el estado y llama a los servicios. No usamos `FindObjectOfType`: cada referencia se asigna en el Inspector (lo hace el generador de escenas).

## 3. Flujo de una investigación

1. **Explorar**: `PlayerInteractor` lanza un rayo de 2.5 m. Al pulsar E sobre un `EvidencePickup`, se emite `PlayerContext.RequestInspect`.
2. **Inspeccionar**: `InspectionController` copia el objeto a un "estudio" lejos de la escena y lo muestra en una RenderTexture. Girarlo puede revelar un detalle oculto (el vidrio en el overol).
3. **Guardar**: `EvidenceService.TryCollect` comprueba que la evidencia exista en este momento de la historia (`SpawnCondition`; por ejemplo, el overol no aparece si Carlos fue detenido).
4. **Analizar**: `EvidenceService.Analyze` aplica hechos y flags, y desbloquea evidencias que dependen de esta (el DVR desbloquea la grabación original).
5. **ALEX**: `AlexService` escucha esos eventos y lanza su afirmación falsa cuando se encuentra o analiza la evidencia que la dispara. Con **Cuestionar** + una evidencia que la refuta se registra una contradicción y la confianza baja 10.
6. **Hablar**: `DialogueService` pide la voz a `IAiClient` (sin conexión: las frases del guion; con conexión: el proxy). **Presentar** una evidencia o un testimonio que refuta una mentira la descubre con reglas fijas.
7. **Hipótesis**: las opciones se desbloquean con evidencias analizadas (`Condition`). Acertar activa flags (`flag_c1_h_llave`).
8. **Decisión**: `DecisionService` aplica flags, la confianza en ALEX y la escena siguiente. La partida se guarda.
9. **Acusación**: `AccusationService` valida las 3 pruebas y llama a `EndingEvaluator`.

## 4. Cómo se calcula el final

`EndingEvaluator.Evaluate` recorre los finales del caso por prioridad y devuelve el primero que cumple su `AccusationRequirement`:
- a quién acusas;
- cuántas **pruebas sólidas** presentas (solo cuentan si están analizadas, y algunas solo con un flag; por ejemplo, las placas tras el laboratorio externo);
- cuántas **contradicciones de ALEX** detectaste;
- una `Condition` extra (para el final secreto: `flag_c2_directiva` + la USB analizada + `flag_c1_pista_nx` o `flag_c1_cuaderno`).

Las variantes (las tres versiones del final incompleto) se eligen con la misma lógica. **Nunca interviene la IA.**

## 5. IA sin poner en riesgo el juego

- `IAiClient` tiene dos implementaciones: `OfflineAiClient` (frases del guion, determinista) y `HttpAiClient` (proxy).
- La clave de API **nunca** está en el juego: vive en una variable de entorno del servidor `Server/proxy.mjs`.
- El proxy pide JSON con un esquema fijo, y el juego lo **valida** (`AiReplyValidator`): los IDs tienen que existir en el perfil y los secretos tienen que estar desbloqueados. Si algo falla, se usa la frase de respaldo.
- Resultado: el juego se termina completo sin conexión, y la IA no puede cambiar la historia.

## 6. Escenas y guardado

- Cada escena tiene un `GameContext` (`[DefaultExecutionOrder(-100)]`) que crea los servicios y carga el estado desde `save.json`.
- El menú crea la partida (*Nueva partida*) o la continúa (*Continuar*). Al cambiar de escena se guarda primero, con la escena nueva como actual.
- La Comisaría es la misma escena en los dos casos: `GameContext` elige `c1_s4` o `c2_s4` según el caso en curso, y cada personaje solo aparece si el `SceneData` activo lo lista.

## 7. Cómo se genera y se prueba

| Herramienta | Qué hace |
|---|---|
| `Case1DataBuilder` / `Case2DataBuilder` | Crean los assets de cada caso desde el guion (IDs y textos). |
| `SceneKit` + un builder por escena | Arman las escenas en greybox y enlazan todas las referencias. |
| **Caso404 → Construir todo** | Regenera los datos y las 8 escenas. |
| Pruebas EditMode (40) | Finales de los dos casos, reglas de ALEX, diálogo, hipótesis, guardado, diseño justo (toda mentira es refutable) e integridad de las escenas. |
| Pruebas PlayMode (6) | Recorren el juego desde el menú hasta cada final, como lo haría el jugador. |
| `UiScreenshots`, `HttpProxyTests` (explícitas) | Capturas de cada pantalla y prueba de integración con el proxy. |

## 8. Patrones que puedes nombrar en la sustentación
- **Data-driven design / ScriptableObjects**: el contenido está separado del código.
- **Servicios + raíz de composición** (`GameContext`): inyección de dependencias manual, sin singletons.
- **Observer** (eventos C# y canales `GameEventChannel` / `StringEventChannel` / `BoolEventChannel`): sistemas desacoplados.
- **Strategy** (`IAiClient` con dos implementaciones, elegidas por `AiClientFactory`).
- **Estado serializable único** (`GameState`): guardar y cargar es trivial y fácil de probar.
- **Pruebas automatizadas** de la lógica pura y de recorridos completos (caminos mínimos del GUION §5).
