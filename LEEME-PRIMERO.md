# Kit de Claude para El Caso 404

Este kit le da a Claude el contexto de tu juego y "skills" (instrucciones especializadas) de programación en Unity, diseño de interfaz, IA conversacional y sistema de evidencias/finales.

## Qué hay dentro
```
CLAUDE.md                  ← contexto del proyecto (Claude lo lee en cada sesión)
.gitignore                 ← para Unity + protege tus claves
.claude/
  rules/csharp.md          ← reglas que se activan al tocar scripts .cs
  rules/ui.md              ← reglas que se activan al tocar la UI
  skills/
    unity-csharp/          ← patrones y convenciones de Unity
    diseno-ui/             ← paleta, fuentes, pantallas, estilo noir
    dialogo-ia/            ← sospechosos con IA, Alex, conexión al modelo
    evidencias-decisiones/ ← evidencias, hipótesis, GameState, finales
    nueva-escena/          ← comando /nueva-escena
    revisar/               ← comando /revisar
```

## Paso 1 — Copiar el kit
Copia **todo** el contenido (incluida la carpeta oculta `.claude`) a la raíz de tu proyecto de Unity, al mismo nivel que `Assets/`, `Packages/` y `ProjectSettings/`. Unity ignora lo que esté fuera de `Assets/`, así que no interfiere.

> En Windows, si no ves `.claude`, activa "Elementos ocultos" en la pestaña Vista del Explorador.

## Paso 2 — Git
En la carpeta del proyecto:
```
git init
git add .
git commit -m "Proyecto base + kit de Claude"
```
Sube el repo a GitHub. Así Claude puede deshacer cambios con seguridad y tu equipo comparte las mismas skills.

## Paso 3 — Instalar Claude Code
Claude Code es la versión de Claude que trabaja directamente en las carpetas de tu proyecto. Puedes usarlo desde la app de escritorio de Claude (pestaña Code), desde la terminal o desde VS Code. Guía oficial: https://code.claude.com/docs/en/quickstart

## Paso 4 — Conectar Claude con el Editor de Unity (MCP for Unity)
Esto permite que Claude cree GameObjects, escenas, lea la consola y recompile, no solo que escriba scripts.
1. Instala Python 3.10+ y `uv` (https://docs.astral.sh/uv/).
2. En Unity: **Window → Package Manager → + → Add package from git URL** y pega:
   `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main`
3. En Unity: **Window → MCP for Unity → Configure All Detected Clients**.
4. Abre Claude Code en la carpeta del proyecto y escribe `/mcp` para confirmar que aparece conectado.

Es un proyecto comunitario gratuito (no oficial de Unity ni de Anthropic): https://github.com/CoplayDev/unity-mcp

## Paso 5 — Comprobar que Claude cargó todo
Dentro de Claude Code, en la carpeta del proyecto:
- `/context` → debe aparecer `CLAUDE.md` en *Memory files*
- `/skills` → deben aparecer las 6 skills

## Paso 6 — Primer mensaje sugerido
> Lee CLAUDE.md y revisa el proyecto. Antes de programar, entrevístame para definir el caso: víctima, culpable real, sospechoso al que Alex empuja, los 4 personajes, la línea de tiempo y las evidencias de cada escena. Luego propón la arquitectura de carpetas y scripts y espera mi aprobación.

## Orden de trabajo recomendado
| Etapa | Qué pedirle a Claude |
|---|---|
| 1. Guion del caso | "Escribe el documento del caso con hechos, mentiras y qué evidencia refuta cada una" |
| 2. Base | "Crea la escena Bootstrap, GameState, SaveService y SceneLoader" |
| 3. Exploración | "Jugador en primera persona, interacción con raycast desde la cámara, resaltado de objetos y modo inspección" |
| 4. Evidencias | "EvidenceData, EvidenceService, expediente y pantalla de análisis" |
| 5. Diálogo sin IA | "DialogueService con OfflineAiClient y la UI de diálogo" (el juego ya se puede jugar) |
| 6. IA real | "Servidor proxy + HttpAiClient siguiendo la skill dialogo-ia" |
| 7. Alex | "Panel de Alex, alexTrust y sugerencias Aceptar/Cuestionar" |
| 8. Hipótesis y decisiones | "Tablero de hipótesis y DecisionData con ramificación" |
| 9. Finales | "EndingEvaluator con pruebas que recorran cada final" |
| 10. Pulido | "/revisar" y ajustes de UI, sonido y transiciones |
| Cada escena | `/nueva-escena Cafeteria` |

## Consejos para trabajar con Claude
- **Pide un plan antes de cambios grandes.** Usa el modo plan (Shift+Tab en la terminal) para que proponga antes de tocar archivos.
- **Una cosa a la vez.** "Haz el expediente" funciona mejor que "haz todo el juego".
- **Prueba en Unity después de cada paso** y pégale a Claude el error exacto de la consola si algo falla.
- **Cuando corrija algo dos veces**, pídele: "agrega esta regla a CLAUDE.md".
- **Commit después de cada etapa que funcione.** Si algo se rompe, vuelves atrás.
- **Las claves de API nunca van en Unity.** La skill `dialogo-ia` explica cómo usar un servidor proxy.
- Las skills son tuyas: edítalas cuando el proyecto cambie (por ejemplo, si decides usar UI Toolkit en lugar de uGUI, cámbialo en `CLAUDE.md` y en `diseno-ui`).
