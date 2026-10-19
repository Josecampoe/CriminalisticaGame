---
name: evidencias-decisiones
description: Sistemas de evidencias, análisis, tablero de hipótesis, decisiones, flags de historia, finales y guardado de El Caso 404. Usar al crear o cambiar evidencias, condiciones para avanzar de escena, decisiones, la evaluación de finales o el GameState.
---

# Evidencias, decisiones y finales

## Flujo por escena
Explorar → recoger evidencias → analizarlas → hablar con personajes y Alex → completar la hipótesis de la escena → decisión → siguiente escena.
Las decisiones importantes ocurren al cerrar la escena, no después de cada objeto.

## `EvidenceData`
- `id`, `displayName`, icono, descripción breve
- `analysisResult`: texto que aparece al analizar (ej. "La huella pertenece a Laura")
- `revealsFactIds`: hechos que el jugador conoce tras analizarla
- `contradicts`: lista de `lieId` o `factId` que refuta
- `sceneId` donde aparece, y si es obligatoria para avanzar

Estados en el `GameState`: no encontrada → encontrada → analizada.

## Hipótesis de escena (`SceneHypothesis`)
Preguntas tipo ¿Quién estuvo? ¿Cómo entró? ¿Qué lo prueba? Cada opción tiene `requiredEvidenceIds`: si el jugador no las ha analizado, la opción aparece bloqueada con el motivo. Así no se adivina: se deduce.

## Decisiones (`DecisionData`)
- `id`, texto, opciones
- Cada opción: `setsFlags`, `alexTrustDelta`, `nextSceneId` (permite ramificar), `requiredEvidenceIds`
- Marca como irreversibles las que afectan el final y pide confirmación

## `GameState` (serializable a JSON)
```csharp
[Serializable]
public class GameState
{
    public string currentSceneId;
    public List<string> foundEvidenceIds = new();
    public List<string> analyzedEvidenceIds = new();
    public List<string> flags = new();            // ej. "confronted_alex_camera"
    public List<string> decisionsTaken = new();   // "decisionId:optionId"
    public List<CaseContradictions> alexContradictions = new(); // por caso: {caseId, lieIds}
    public int alexTrust = 60;                    // 0–100, empieza en 60 (GUION §1)
    public string accusedSuspectId;
}
```

## Finales (`EndingData`)
Cada final tiene condiciones y prioridad; el `EndingEvaluator` recorre los finales de mayor a menor prioridad y devuelve el primero que se cumple. Lógica pura, sin MonoBehaviour, con pruebas EditMode.

| Prioridad | Final | Condición orientativa |
|---|---|---|
| 1 | Secreto (Alex involucrado) | acusa al culpable real + flag `alex_involvement_proven` |
| 2 | Verdadero | acusa al culpable real + ≥ N contradicciones de Alex detectadas |
| 3 | Incompleto | acusa al culpable real sin pruebas suficientes, o no acusa |
| 4 | Engaño | acusa al sospechoso que Alex promovía |

Ajusta los umbrales en los datos, no en código.

## Reglas de diseño justo
- Cada final debe ser alcanzable: escribe una prueba que simule el camino mínimo hacia cada uno.
- Toda mentira debe tener al menos una evidencia que la refute, y esa evidencia debe estar en una escena alcanzable antes de la acusación final.
- Nada esencial depende de la respuesta de la IA: si la IA falla, el jugador igual puede llegar a todos los finales.

## Guardado
`SaveService` serializa el `GameState` con `JsonUtility` (o Newtonsoft si está instalado) en `Application.persistentDataPath/save.json`. Guarda al cambiar de escena y al tomar una decisión. Incluye `saveVersion` para migraciones.
