---
name: unity-csharp
description: Convenciones y patrones de Unity/C# para El Caso 404. Usar al crear o modificar cualquier script, ScriptableObject, prefab o sistema del juego, o al depurar errores de compilación o de ejecución en Unity.
---

# Unity + C# para El Caso 404

## Antes de escribir código
1. Lee `ProjectSettings/ProjectVersion.txt` y `Packages/manifest.json` para saber la versión de Unity y qué paquetes hay (Input System, URP 2D, TextMeshPro). No uses APIs de paquetes que no estén instalados; si falta uno, dímelo y pregunta antes de agregarlo.
2. Busca si ya existe un sistema parecido en `Assets/_Project/Scripts/` y extiéndelo en lugar de duplicarlo.

## Patrones que usamos
- **ScriptableObjects como datos**: `CaseData`, `CharacterProfile`, `EvidenceData`, `SceneData`, `EndingData`. Llevan `[CreateAssetMenu(menuName = "Caso404/...")]`. Son de solo lectura en tiempo de juego: el estado cambiante va en `GameState`.
- **IDs estables**: cada dato tiene un `string id` (ej. `ev_huella_ventana`). El guardado y los flags usan IDs, nunca referencias a objetos.
- **Eventos**: un sistema avisa con `public event Action<EvidenceData> OnEvidenceCollected;` y los demás se suscriben. Para eventos globales, usa canales `GameEventChannel` (ScriptableObject con `Raise()` y listeners).
- **Servicios**: `GameStateService`, `EvidenceService`, `DialogueService`, `DecisionService`, `SaveService`. Viven en una escena `Bootstrap` persistente (`DontDestroyOnLoad`) y se inyectan por `[SerializeField]` o un localizador simple. Evita singletons estáticos salvo para el localizador.
- **Asíncrono**: `async Task`/`Awaitable` para la red; nunca bloquees el hilo principal. Cancela con `CancellationToken` cuando el objeto se destruye.
- **Interacción 2D**: los objetos investigables implementan `IInteractable` (`string Prompt`, `void Interact(PlayerContext ctx)`), con `Collider2D` en modo trigger o raycast del puntero.
- **Escenas**: carga con `SceneManager.LoadSceneAsync` desde un `SceneLoader` con pantalla de transición.

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
