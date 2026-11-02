# Proxy de IA — El Caso 404

Servidor pequeño (Node 18 o superior) que conecta el juego con la IA de Anthropic usando el SDK oficial (`@anthropic-ai/sdk`). Existe porque **la clave de API nunca va dentro del juego**: cualquier build se puede descompilar.

Da voz a dos tipos de personaje:
- **Sospechosos y testigos**: responden tus preguntas escritas usando solo sus hechos y sus mentiras permitidas del guion. Las mentiras que ya descubriste no las sostienen.
- **ALEX**: parece un asistente útil y a veces ayuda de verdad con lo ya confirmado. Pero empuja con sutileza hacia su sospechoso objetivo (`CaseData.AlexTarget`) usando solo sus afirmaciones del guion. No puede inventar pruebas, negar un análisis que ya tienes ni sostener una afirmación que ya refutaste. Su tono cambia con la confianza (cálido, formal o frío).

La IA **nunca decide nada**: el juego valida cada respuesta, y la confianza, las contradicciones y los finales se calculan con la lógica del juego.

## Cómo usarlo

1. Consigue una clave de API en https://console.anthropic.com (sección *API Keys*). Necesita saldo o un plan con créditos.
2. Arranca el proxy (deja la ventana abierta mientras juegas):
   - **Fácil:** clic derecho en `Server/iniciar-proxy.ps1` → *Ejecutar con PowerShell*. Te pide la clave (oculta, no la guarda) e instala lo necesario la primera vez.
   - **A mano:** en PowerShell, `cd Server`, `npm install` (solo la primera vez), `$env:ANTHROPIC_API_KEY = "tu-clave"`, `node proxy.mjs`.
3. Juega normal (Editor o `ElCaso404.exe`). `AiSettings` ya apunta a `http://127.0.0.1:8790/dialogue` con *Use Offline* desmarcado.

Si el proxy está apagado, el juego lo detecta en 2 segundos y usa las frases del guion: se puede jugar completo sin IA.

## Variables de entorno

| Variable | Qué hace |
|---|---|
| `ANTHROPIC_API_KEY` | Clave de API. También sirve iniciar sesión con `ant auth login`. |
| `ANTHROPIC_MODEL` | Modelo (por defecto `claude-haiku-5-5`: rápido y económico para diálogos). |
| `PORT` | Puerto (por defecto `8790`; el 8787 lo usa `wrangler dev` en este equipo). |
| `CASO404_MOCK=1` | Modo de prueba: responde un texto fijo sin llamar a la IA (para probar la conexión). |

## Comprobar que funciona

- `http://127.0.0.1:8790/health` en el navegador debe mostrar `{"ok":true,...}`. `keyInEnv: true` indica que ve la clave.
- Si algo falla, la consola del proxy dice por qué en español (clave inválida, sin saldo o límite, sin conexión…). El juego, mientras tanto, usa el guion.

## Costo aproximado

Con `claude-haiku-5-5`, cada pregunta envía unos 1.000–2.000 tokens y recibe unos 100–300. Una partida con decenas de preguntas cuesta centavos de dólar.
