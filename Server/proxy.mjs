// Proxy de IA para El Caso 404.
// El juego NUNCA conoce la clave de API: este servidor la lee del entorno, arma las instrucciones del personaje y
// llama al proveedor de IA con su SDK oficial:
//   - "gemini" (por defecto): Google Gemini, con plan gratuito. Clave en GEMINI_API_KEY (Google AI Studio).
//   - "anthropic": Claude, de pago. Clave en ANTHROPIC_API_KEY.
// Se elige con AI_PROVIDER; si no se indica, se usa Gemini (o Anthropic si solo hay clave de Anthropic).
// Devuelve el JSON que espera el juego: { reply, emotion, usedFactIds, usedLieIds, revealedSecretIds }.
//
// Dos modos (campo "mode" del pedido):
//   - "character": un sospechoso o testigo. Solo usa sus hechos y sus mentiras permitidas.
//   - "alex": el asistente ALEX. Ayuda de verdad con lo ya confirmado, pero empuja con sutileza hacia su sospechoso
//     objetivo usando solo sus afirmaciones del guion. Nunca decide nada: el juego valida y calcula los finales.
//
// Uso:  cd Server && npm install && node proxy.mjs      (puerto 8790)
// Variables: GEMINI_API_KEY, GEMINI_MODEL, ANTHROPIC_API_KEY, ANTHROPIC_MODEL, AI_PROVIDER, PORT, CASO404_MOCK=1

import http from "node:http";
import Anthropic from "@anthropic-ai/sdk";
import { GoogleGenAI, ApiError } from "@google/genai";
import { z } from "zod";
import { zodOutputFormat } from "@anthropic-ai/sdk/helpers/zod";
import { ReplySchema, REPLY_JSON_SCHEMA, RequestSchema, systemPrompt, userMessage } from "./prompts.mjs";

// 8790 y no 8787: en este equipo el 8787 lo usa `wrangler dev` (Cloudflare).
const PORT = Number(process.env.PORT ?? 8790);
// Proveedor: Gemini por defecto (gratis); Anthropic si se pide o si es la única clave disponible.
const PROVIDER = (process.env.AI_PROVIDER ??
  (!process.env.GEMINI_API_KEY && process.env.ANTHROPIC_API_KEY ? "anthropic" : "gemini")).toLowerCase();
// Modelos rápidos y económicos para diálogos cortos (skill dialogo-ia del proyecto).
const MODEL = PROVIDER === "anthropic"
  ? process.env.ANTHROPIC_MODEL ?? "claude-haiku-5-5"
  : process.env.GEMINI_MODEL ?? "gemini-3.5-flash-lite";

// Modo de prueba solo si se pide (CASO404_MOCK=1): responde un texto fijo para comprobar la conexión.
// Sin credenciales NO se usa: el proxy responde error y el juego usa las frases del guion, que se ven mejor.
const mock = process.env.CASO404_MOCK === "1";
const hasKeyInEnv = PROVIDER === "anthropic"
  ? Boolean(process.env.ANTHROPIC_API_KEY || process.env.ANTHROPIC_AUTH_TOKEN)
  : Boolean(process.env.GEMINI_API_KEY);
// Clientes con credenciales del entorno; nunca escritas en código.
const anthropic = !mock && PROVIDER === "anthropic" ? new Anthropic() : null;
const gemini = !mock && PROVIDER === "gemini" && process.env.GEMINI_API_KEY
  ? new GoogleGenAI({ apiKey: process.env.GEMINI_API_KEY })
  : null;

/** Pregunta a Gemini con salida JSON según el esquema, y valida el resultado con zod. */
async function askGemini(payload) {
  if (!gemini) throw new Error("no hay clave de Gemini: define GEMINI_API_KEY antes de arrancar el proxy");
  const response = await gemini.models.generateContent({
    model: MODEL,
    contents: userMessage(payload),
    config: {
      systemInstruction: systemPrompt(payload),
      responseMimeType: "application/json",
      responseJsonSchema: REPLY_JSON_SCHEMA,
      maxOutputTokens: 1024,
      temperature: 0.8,
    },
  });
  const text = response.text;
  if (!text) throw new Error(`respuesta vacía (${response.candidates?.[0]?.finishReason ?? "sin motivo"})`);
  return ReplySchema.parse(JSON.parse(text));
}

async function askClaude(payload) {
  const response = await anthropic.messages.parse({
    model: MODEL,
    max_tokens: 2048,
    system: systemPrompt(payload),
    messages: [{ role: "user", content: userMessage(payload) }],
    // Diálogo corto: esfuerzo bajo (rápido y barato) y JSON con esquema fijo.
    output_config: { effort: "low", format: zodOutputFormat(ReplySchema) },
  });
  // Una negativa o una respuesta cortada no sirve: el juego usará la frase de respaldo del guion.
  if (response.stop_reason === "refusal" || response.stop_reason === "max_tokens")
    throw new Error(`stop_reason ${response.stop_reason}`);
  if (!response.parsed_output) throw new Error("respuesta sin JSON válido");
  return response.parsed_output;
}

function mockReply(payload) {
  const who = payload.mode === "alex" ? "ALEX" : payload.characterName ?? "El personaje";
  return {
    reply: `(modo de prueba del proxy) ${who} escucha tu pregunta, pero no hay IA conectada.`,
    emotion: "neutral",
    usedFactIds: [],
    usedLieIds: [],
    revealedSecretIds: [],
  };
}

function send(res, status, obj) {
  res.writeHead(status, { "content-type": "application/json; charset=utf-8" });
  res.end(JSON.stringify(obj));
}

/** Mensaje claro en la consola según el tipo de error (sin mostrar la clave). */
function describe(e) {
  if (e instanceof ApiError) {
    if (e.status === 429) return "Gemini: se alcanzó el límite gratuito (por minuto o por día); espera un poco";
    if (e.status === 400 && /api key/i.test(e.message)) return "Gemini: la clave no es válida (revisa GEMINI_API_KEY)";
    if (e.status === 403) return "Gemini: la clave no tiene permiso (¿la API está habilitada en tu proyecto?)";
    if (e.status === 404) return `Gemini: el modelo ${MODEL} no existe o no está disponible; prueba GEMINI_MODEL=gemini-3.8-flash`;
    return `Gemini: error ${e.status}: ${e.message}`;
  }
  if (e instanceof z.ZodError || e instanceof SyntaxError) return "la IA devolvió un JSON que no cumple el formato";
  if (e instanceof Anthropic.AuthenticationError) return "la clave de API no es válida (revisa ANTHROPIC_API_KEY)";
  if (e instanceof Anthropic.PermissionDeniedError) return "la clave no tiene permiso para este modelo";
  if (e instanceof Anthropic.NotFoundError) return `el modelo ${MODEL} no existe o no está disponible`;
  if (e instanceof Anthropic.RateLimitError) return "límite de uso alcanzado; espera un momento";
  if (e instanceof Anthropic.BadRequestError) return `petición rechazada: ${e.message}`;
  if (e instanceof Anthropic.APIConnectionError) return "sin conexión con la API de Anthropic";
  if (e instanceof Anthropic.APIError) return `error de la API (${e.status}): ${e.message}`;
  if (/authentication method/i.test(e.message)) return "no hay clave de API: define ANTHROPIC_API_KEY antes de arrancar el proxy";
  return e.message;
}

const server = http.createServer(async (req, res) => {
  if (req.method === "GET" && req.url === "/health") return send(res, 200, { ok: true, mock, provider: PROVIDER, model: MODEL, keyInEnv: hasKeyInEnv });
  if (req.method !== "POST" || req.url !== "/dialogue") return send(res, 404, { error: "no encontrado" });
  let body = "";
  req.on("data", (chunk) => {
    body += chunk;
    if (body.length > 64_000) req.destroy();
  });
  req.on("end", async () => {
    let payload;
    try {
      payload = RequestSchema.parse(JSON.parse(body));
    } catch {
      return send(res, 400, { error: "pedido inválido" });
    }
    try {
      const reply = mock ? mockReply(payload) : PROVIDER === "anthropic" ? await askClaude(payload) : await askGemini(payload);
      return send(res, 200, reply);
    } catch (e) {
      console.error("[proxy]", describe(e));
      return send(res, 502, { error: "la IA no respondió" });
    }
  });
});

server.listen(PORT, () => {
  console.log(`[proxy] El Caso 404 escuchando en http://localhost:${PORT}/dialogue  (${PROVIDER}, modelo ${MODEL}${mock ? ", MODO DE PRUEBA sin IA" : ""})`);
  if (!mock && !hasKeyInEnv)
    console.warn(`[proxy] Aviso: no veo la clave (${PROVIDER === "anthropic" ? "ANTHROPIC_API_KEY" : "GEMINI_API_KEY"}). Las respuestas fallarán y el juego usará las frases del guion.`);
});
