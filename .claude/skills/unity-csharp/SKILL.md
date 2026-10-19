---
name: unity-csharp
description: Convenciones y patrones de Unity/C# para El Caso 404. Usar al crear o modificar cualquier script, ScriptableObject, prefab o sistema del juego, o al depurar errores de compilación o de ejecución en Unity.
---

# Unity + C# para El Caso 404

## Antes de escribir código
1. Lee `ProjectSettings/ProjectVersion.txt` y `Packages/manifest.json` para saber la versión de Unity y qué paquetes hay (Input System, URP, TextMeshPro, Cinemachine, ProBuilder). No uses APIs de paquetes que no estén instalados; si falta uno, dímelo y pregunta antes de agregarlo.
2. Busca si ya existe un sistema parecido en `Assets/_Project/Scripts/` y extiéndelo en lugar de duplicarlo.

## Patrones que usamos
- **ScriptableObjects como datos**: `CaseData`, `CharacterProfile`, `EvidenceData`, `SceneData`, `EndingData`. Llevan `[CreateAssetMenu(menuName = "Caso404/...")]`. Son de solo lectura en tiempo de juego: el estado cambiante va en `GameState`.
- **IDs estables**: cada dato tiene un `string id` (ej. `ev_c1_huella`, siempre los IDs de `Docs/GUION.md`). El guardado y los flags usan IDs, nunca referencias a objetos.
- **Eventos**: un sistema avisa con `public event Action<EvidenceData> OnEvidenceCollected;` y los demás se suscriben. Para eventos globales, usa canales `GameEventChannel` (ScriptableObject con `Raise()` y listeners).
- **Servicios**: `GameStateService`, `EvidenceService`, `DialogueService`, `DecisionService`, `SaveService`. Viven en una escena `Bootstrap` persistente (`DontDestroyOnLoad`) y se inyectan por `[SerializeField]` o un localizador simple. Evita singletons estáticos salvo para el localizador.
- **Asíncrono**: `async Task`/`Awaitable` para la red; nunca bloquees el hilo principal. Cancela con `CancellationToken` cuando el objeto se destruye.
- **Jugador en primera persona**: `PlayerController` con `CharacterController`, mirar con mouse o mando (Input System), caminar sin correr ni saltar (es un juego de investigación). La cámara va en un hijo del jugador o en una cámara virtual de Cinemachine.
- **Interacción 3D**: los objetos investigables implementan `IInteractable` (`string Prompt`, `void Interact(PlayerContext ctx)`) y están en la capa `Interactable`. `PlayerInteractor` lanza un `Physics.Raycast` desde el centro de la cámara (alcance 2.5 m, solo esa capa). Cuando apunta a uno, lo resalta y muestra el prompt en el HUD.
- **Modo inspección**: al recoger una evidencia, el objeto se muestra frente a la cámara en una capa propia (o con una segunda cámara de inspección). El jugador lo rota con el mouse y luego pulsa "Guardar en expediente". Mientras dura, se bloquean el movimiento y el cursor del jugador.
- **Personajes 3D**: cada NPC tiene un `Collider`, implementa `IInteractable` y gira la cabeza o el cuerpo hacia el jugador al hablar. La conversación abre la UI de diálogo y bloquea el movimiento.
- **Escenas**: carga con `SceneManager.LoadSceneAsync` desde un `SceneLoader` con pantalla de transición.

## Rendimiento y escenas 3D
- Iluminación horneada (*Baked*) para las escenas, con pocas luces en tiempo real; *Light Probes* para los personajes.
- Objetos estáticos marcados como *Static*. Materiales compartidos en lugar de una copia por objeto.
- Modelos low-poly; primero greybox con ProBuilder y luego se reemplazan los cubos por los modelos finales, sin cambiar los scripts.
- Meta: 60 FPS en un portátil de gama media.

## Plantilla de MonoBehaviour
```csharp
using UnityEngine;

namespace Caso404.Evidence
{
    /// <summary>Objeto de la escena que entrega una evidencia al inspeccionarlo.</summary>
    public class EvidencePickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private EvidenceData _evidence;
        [SerializeField] private EvidenceService _evidenceService;

        public string Prompt => $"Inspeccionar {_evidence.DisplayName}";

        public void Interact(PlayerContext ctx)
        {
            if (_evidenceService.TryCollect(_evidence)) gameObject.SetActive(false);
        }
    }
}
```

## Verificación
- Después de crear o cambiar scripts, revisa la consola de Unity (con MCP for Unity: lee los logs y recompila). Corrige errores antes de seguir.
- Para lógica pura (evaluación de finales, validación de respuestas de IA, guardado) escribe pruebas en Unity Test Framework (EditMode) en `Assets/_Project/Tests/`.
- Al terminar, entrégame una lista corta: archivos creados, qué configurar en el Inspector y cómo probarlo en Play Mode.

## Errores comunes a evitar
- Referencias nulas por campos sin asignar: valida en `OnValidate` o `Awake` y loguea un error claro con el nombre del GameObject.
- Lógica en el constructor de un MonoBehaviour (usa `Awake`).
- Modificar un ScriptableObject en Play Mode creyendo que se reinicia (persiste en el Editor).
- `async void` fuera de manejadores de eventos de UI.
