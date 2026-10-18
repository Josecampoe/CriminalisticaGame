---
name: nueva-escena
description: Crea una escena jugable nueva de El Caso 404 con su checklist completo.
disable-model-invocation: true
argument-hint: "[nombre de la escena, ej. Cafeteria]"
---

Crea la escena **$ARGUMENTS** siguiendo las skills `unity-csharp`, `diseno-ui` y `evidencias-decisiones`.

1. Pregúntame (si no lo sé ya): qué evidencias hay, qué personajes aparecen, qué hipótesis se responde al final y a qué escenas puede llevar la decisión.
2. Crea `Assets/_Project/Scenes/$ARGUMENTS.unity` (con MCP for Unity si está disponible) con:
   - greybox con ProBuilder (piso, paredes, puertas, muebles principales) y colisiones;
   - iluminación base acorde al ambiente del guion;
   - prefab del jugador en primera persona en el punto de aparición;
   - prefab del HUD.
3. Crea el `SceneData` en `Assets/_Project/Data/Scenes/` y los `EvidenceData` que falten en `Data/Evidence/`.
4. Coloca los objetos investigables (`EvidencePickup` o `IInteractable`, capa `Interactable`) y los personajes (`CharacterNpc` con su `CharacterProfile`). Usa placeholders 3D si todavía no hay modelos. Revisa que cada objeto sea alcanzable y visible desde la altura de los ojos del jugador.
5. Hornea la iluminación cuando el greybox esté listo.
6. Configura la `SceneHypothesis` y la `DecisionData` de cierre.
6. Agrega la escena a Build Settings.
7. Revisa la consola y verifica que no haya referencias vacías.
8. Entrégame: lista de lo creado, qué falta de arte (placeholders usados) y cómo probarla en Play Mode.
