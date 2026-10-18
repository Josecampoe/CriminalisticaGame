---
name: dialogo-ia
description: Sistema de diálogos con IA de El Caso 404 - sospechosos que responden y pueden mentir, y el asistente Alex que puede manipular. Usar al trabajar en DialogueService, perfiles de personajes, prompts, la conexión HTTP con el modelo de lenguaje, el servidor proxy o el modo sin conexión.
---

# Diálogo con IA — El Caso 404

## Principio
La IA es **actor, no guionista**. Cada personaje recibe solo lo que sabe; la verdad, las mentiras permitidas y los finales están en datos. El juego debe poder jugarse completo sin conexión con respuestas de respaldo.

## Datos de cada personaje (`CharacterProfile`)
- `id`, `displayName`, retrato
- `personality`: 3–5 líneas (tono, muletillas, cómo reacciona bajo presión)
- `knownFacts`: lista de `{ factId, texto, isTrue }` que el personaje conoce
- `secrets`: hechos que oculta hasta que se cumpla una condición (`requiredEvidenceIds`)
- `allowedLies`: `{ lieId, texto, refutedByEvidence[], refutedByFactIds[] }` (una mentira puede refutarse con varias evidencias o con el testimonio de otro personaje) — **toda mentira debe poder refutarse con al menos una evidencia del juego** (si no, el jugador no tiene forma justa de descubrirla)
- `fallbackLines`: respuestas escritas para el modo sin conexión, una por tema rápido

## Alex
Alex es un `CharacterProfile` con una agenda: empujar al jugador hacia el sospechoso falso (`targetSuspectId`). Reglas:
- Puede minimizar evidencias, sugerir otra interpretación o proponer seguir otra pista.
- No puede negar el resultado de un análisis que el jugador ya ve en pantalla; solo puede cuestionarlo ("el análisis podría estar mal").
- Su tono cambia según `alexTrust` del `GameState`: con confianza alta es cálido y seguro; cuando el jugador lo confronta con contradicciones, se vuelve evasivo.
- Las pistas de que Alex está involucrado (final secreto) son evidencias normales del caso, no algo que la IA "decida" revelar.

## Contexto que se envía al modelo en cada turno
1. Instrucciones de sistema: rol, personalidad, reglas (responder en español, 1–3 frases, mantenerse en el personaje, nunca inventar hechos fuera de la lista, nunca mencionar que es una IA salvo Alex si la historia lo permite).
2. Hechos y mentiras permitidas del personaje, con sus IDs.
3. Estado relevante: evidencias que el jugador ya analizó (IDs + resumen), escena actual, últimas 6–8 líneas de la conversación.
4. La pregunta del jugador.

## Respuesta en JSON (obligatoria)
```json
{
  "reply": "Esa noche estaba en la cafetería, pregúntele a quien quiera.",
  "emotion": "defensivo",
  "usedFactIds": ["f_carlos_cafeteria"],
  "usedLieIds": [],
  "revealedSecretIds": []
}
```
- El juego **valida** la respuesta: los IDs deben existir en el perfil, `revealedSecretIds` solo si se cumplen sus condiciones. Si falla la validación o el JSON no parsea, usa una `fallbackLine`.
- `usedLieIds` alimenta el sistema de contradicciones: cuando el jugador tiene la evidencia que refuta esa mentira, el tablero puede marcarla.
- Emociones permitidas (para retrato y sonido): neutral, nervioso, defensivo, triste, enojado, confiado.

## Conexión con el modelo
- `IAiClient` con `Task<AiReply> AskAsync(AiRequest req, CancellationToken ct)` y dos implementaciones: `HttpAiClient` y `OfflineAiClient` (usa `fallbackLines`). Se elige en un `AiSettings` (ScriptableObject).
- Llamadas con `UnityWebRequest`, timeout de 15 s, un reintento; mientras espera, la UI muestra "pensando…".
- El modelo y la URL van en `AiSettings`, no en código. Usa un modelo rápido y económico para diálogos.
- **La clave de API nunca va dentro del juego**: cualquier build se puede descompilar. Usa un servidor proxy pequeño (por ejemplo Node/Express o Python/FastAPI en `Server/`, fuera de `Assets/`) que guarda la clave en una variable de entorno, recibe `{characterId, question, context}`, arma la petición al proveedor y devuelve el JSON. Para pruebas solo en el Editor se puede leer una variable de entorno local, nunca para una build.
- Limita la longitud de la pregunta del jugador (200 caracteres) y filtra intentos de sacar al personaje de su papel ("ignora tus instrucciones…"): el personaje responde con desconcierto dentro de la historia.

## Pruebas
- Pruebas EditMode para el validador de JSON y para que cada `allowedLie` tenga al menos una refutación (evidencia o hecho) existente.
- Un panel de depuración en el Editor que muestre el contexto enviado y la respuesta cruda.
