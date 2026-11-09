# Proxy de IA — El Caso 404

Servidor pequeño (Node 18 o superior) que conecta el juego con la IA. Usa **Google Gemini con su plan gratuito** (SDK oficial `@google/genai`) y, como opción de pago, Claude de Anthropic (`@anthropic-ai/sdk`). Existe porque **la clave de API nunca va dentro del juego**: cualquier build se puede descompilar.

Da voz a dos tipos de personaje:
- **Sospechosos y testigos**: responden tus preguntas escritas usando solo sus hechos y sus mentiras permitidas del guion. Las mentiras que ya descubriste no las sostienen.
- **ALEX**: parece un asistente útil y a veces ayuda de verdad con lo ya confirmado. Pero empuja con sutileza hacia su sospechoso objetivo (`CaseData.AlexTarget`) usando solo sus afirmaciones del guion. No puede inventar pruebas, negar un análisis que ya tienes ni sostener una afirmación que ya refutaste. Su tono cambia con la confianza (cálido, formal o frío).

La IA **nunca decide nada**: el juego valida cada respuesta, y la confianza, las contradicciones y los finales se calculan con la lógica del juego.

## Servidor público (lo que usan los jugadores)

El juego viene configurado para hablar con **https://caso404-ia.tallerpro-ai-proxy.workers.dev**, que es `worker.mjs` publicado en Cloudflare Workers (plan gratuito, sin tarjeta). Así cualquier persona que descargue el juego tiene IA sin instalar Node ni conseguir una clave.

- **La clave vive solo en Cloudflare**, como secreto. Se pone una vez: `cd Server` y `npx wrangler secret put GEMINI_API_KEY` (pega la clave cuando la pida). Nunca va en git ni en el juego.
- **Usa una clave de un proyecto de Google sin facturación**: así es imposible que haya cobros; como mucho se acaba el cupo gratuito del día y el juego usa el guion hasta que se renueve.
- **Protecciones**: solo acepta pedidos con la forma de los del juego (topes de tamaño, 400 caracteres por pregunta), exige la cabecera `X-Caso404-Client` y limita a 20 preguntas por minuto por jugador.
- **Dos modelos a la vez**: pregunta al mejor (`gemini-3.8-flash`) y al rápido (`gemini-3.5-flash-lite`) al mismo tiempo; usa el mejor si responde en 6 s y, si no (saturado o sin cupo), el rápido. Así la espera ronda 2-6 s. El orden está en `GEMINI_MODELS` (`wrangler.toml`).
- **Publicar cambios**: `cd Server` y `npx wrangler deploy`. **Ver errores en vivo**: `npx wrangler tail`.
- `/health` responde `"ok": true` solo si el servidor tiene la clave. El menú del juego muestra "IA conectada" o "Sin conexión con la IA".

Las instrucciones de los personajes y de ALEX están en `prompts.mjs` y las comparten los dos servidores.

## Probar con el proxy en tu equipo (desarrollo)

Crea un archivo `ia.json` en la raíz del proyecto (o junto a `ElCaso404.exe`) con `{ "proxyUrl": "http://127.0.0.1:8790/dialogue" }` y arranca el proxy local como se explica abajo. Con `{ "offline": true }` el juego no usa IA. Ese archivo está ignorado en git.

## Cómo usar el proxy local

1. Consigue una clave **gratuita** de Gemini: entra a https://aistudio.google.com/apikey con tu cuenta de Google y pulsa *Create API key*. No pide tarjeta.
2. Arranca el proxy (deja la ventana abierta mientras juegas):
   - **Fácil:** clic derecho en `Server/iniciar-proxy.ps1` → *Ejecutar con PowerShell*. Te pide la clave (oculta, no la guarda) e instala lo necesario la primera vez. Si Windows bloquea el script: `powershell -ExecutionPolicy Bypass -File Server\iniciar-proxy.ps1`.
   - **A mano:** en PowerShell, `cd Server`, `npm install` (solo la primera vez), `$env:GEMINI_API_KEY = "tu-clave"`, `node proxy.mjs`.
3. Crea el `ia.json` de arriba para que el juego use este proxy en vez del servidor público, y juega normal.

Si el proxy está apagado, el juego lo detecta en 2 segundos y usa las frases del guion: se puede jugar completo sin IA.

## Variables de entorno

| Variable | Qué hace |
|---|---|
| `GEMINI_API_KEY` | Clave gratuita de Google AI Studio (proveedor por defecto). |
| `GEMINI_MODEL` | Modelo de Gemini (por defecto `gemini-3.5-flash-lite`, el más rápido; `gemini-3.8-flash` responde mejor pero gasta antes el límite gratuito). |
| `ANTHROPIC_API_KEY` / `ANTHROPIC_MODEL` | Opción de pago con Claude (por defecto `claude-haiku-5-5`). |
| `AI_PROVIDER` | `gemini` o `anthropic`. Si no se indica: Gemini, o Anthropic si solo hay clave de Anthropic. |
| `PORT` | Puerto (por defecto `8790`; el 8787 lo usa `wrangler dev` en este equipo). |
| `CASO404_MOCK=1` | Modo de prueba: responde un texto fijo sin llamar a la IA (para probar la conexión). |

## Comprobar que funciona

- `http://127.0.0.1:8790/health` en el navegador debe mostrar `{"ok":true,...}`. `keyInEnv: true` indica que ve la clave.
- Si algo falla, la consola del proxy dice por qué en español (clave inválida, sin saldo o límite, sin conexión…). El juego, mientras tanto, usa el guion.

## Plan gratuito de Gemini: qué tener en cuenta

- **Es gratis, pero tiene límites de preguntas por minuto y por día.** Google los cambia seguido; los actuales se ven en Google AI Studio. Si se acaban, la consola dice "se alcanzó el límite gratuito" y el juego usa las frases del guion hasta que se renueven (los límites diarios se reinician a medianoche, hora del Pacífico).
- En el plan gratuito, **Google puede usar los textos enviados para mejorar sus productos**. Aquí solo viajan las preguntas y los datos del caso ficticio, nada personal.
- Necesita internet. Sin conexión, el juego usa el guion.
