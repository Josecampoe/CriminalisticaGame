// Servidor de IA de El Caso 404 en internet (Cloudflare Workers, plan gratuito).
// Hace lo mismo que proxy.mjs, pero publicado para que cualquier copia del juego tenga IA sin instalar nada.
//
// - La clave de Gemini vive solo en Cloudflare como secreto (`npx wrangler secret put GEMINI_API_KEY`); nunca en git.
// - Usa las mismas instrucciones que el proxy local (prompts.mjs).
// - Protecciones para que nadie gaste el cupo usándolo como chatbot: solo acepta pedidos con la forma de los del
//   juego, con topes de tamaño, y limita las preguntas por jugador y por minuto.
// - Pregunta a la vez al mejor modelo y a uno de respaldo (GEMINI_MODELS); usa el mejor si llega a tiempo. Si los dos fallan, responde error y el
//   juego usa las frases del guion: nunca se queda sin poder jugar.
//
// Publicar:  cd Server && npx wrangler deploy
// Ver errores en vivo:  npx wrangler tail

import { ReplySchema, REPLY_JSON_SCHEMA, RequestSchema, systemPrompt, userMessage } from "./prompts.mjs";

const GEMINI_URL = "https://generativelanguage.googleapis.com/v1beta/models";
const MAX_BODY = 64_000;
// Tiempo máximo por modelo (el juego espera como mucho 15 s).
const MODEL_TIMEOUT_MS = 10_000;
// Cuánto se espera al mejor modelo antes de usar la respuesta del de respaldo.
const PREFERRED_WAIT_MS = 6_000;
// Respaldo por si el límite de Cloudflare no está disponible: cuenta por instancia del servidor.
const LOCAL_LIMIT = 20;
const localHits = new Map();

const json = (status, obj) =>
  new Response(JSON.stringify(obj), { status, headers: { "content-type": "application/json; charset=utf-8" } });

const modelsOf = (env) =>
  (env.GEMINI_MODELS ?? "gemini-3.8-flash,gemini-3.5-flash-lite").split(",").map((m) => m.trim()).filter(Boolean);

/** True si este jugador (por IP) todavía puede preguntar este minuto. */
async function allowed(request, env) {
  const ip = request.headers.get("cf-connecting-ip") ?? "anon";
  if (env.LIMITER) return (await env.LIMITER.limit({ key: ip })).success;
  const minute = Math.floor(Date.now() / 60_000);
  const key = `${ip}:${minute}`;
  const hits = (localHits.get(key) ?? 0) + 1;
  localHits.set(key, hits);
  if (localHits.size > 5000) localHits.clear();
  return hits <= LOCAL_LIMIT;
}

/** Una llamada a Gemini con salida JSON según el esquema. Devuelve la respuesta validada o lanza un error con status. */
async function askGemini(model, payload, env, withThinkingLevel = true) {
  const generationConfig = {
    responseMimeType: "application/json",
    responseJsonSchema: REPLY_JSON_SCHEMA,
    maxOutputTokens: 2048,
    temperature: 0.8,
  };
  // Diálogo corto: que piense poco para responder rápido (el juego espera como mucho 15 s).
  if (withThinkingLevel) generationConfig.thinkingConfig = { thinkingLevel: "low" };
  const response = await fetch(`${GEMINI_URL}/${encodeURIComponent(model)}:generateContent`, {
    method: "POST",
    headers: { "content-type": "application/json", "x-goog-api-key": env.GEMINI_API_KEY },
    body: JSON.stringify({
      systemInstruction: { parts: [{ text: systemPrompt(payload) }] },
      contents: [{ role: "user", parts: [{ text: userMessage(payload) }] }],
      generationConfig,
    }),
    // Si un modelo tarda, se corta y se prueba el siguiente a tiempo.
    signal: AbortSignal.timeout(MODEL_TIMEOUT_MS),
  });
  // Un modelo que no acepta el nivel de razonamiento se reintenta sin él.
  if (response.status === 400 && withThinkingLevel) {
    const detail = await response.text();
    if (/thinking/i.test(detail)) return askGemini(model, payload, env, false);
    const error = new Error(`Gemini ${model}: 400 ${detail.slice(0, 300)}`);
    error.status = 400;
    throw error;
  }
  if (!response.ok) {
    const error = new Error(`Gemini ${model}: ${response.status} ${(await response.text()).slice(0, 300)}`);
    error.status = response.status;
    throw error;
  }
  const data = await response.json();
  const parts = data.candidates?.[0]?.content?.parts ?? [];
  const text = parts.filter((p) => !p.thought && p.text).map((p) => p.text).join("");
  if (!text) throw new Error(`Gemini ${model}: respuesta vacía (${data.candidates?.[0]?.finishReason ?? "sin motivo"})`);
  return ReplySchema.parse(JSON.parse(text));
}

/**
 * Pregunta al mejor modelo y, a la vez, al de respaldo. Si el mejor responde en PREFERRED_WAIT_MS se usa su
 * respuesta; si no (saturado, sin cupo o lento), la del respaldo, que para entonces ya suele estar lista.
 * Así la espera del jugador no pasa de unos segundos aunque el mejor modelo esté saturado.
 */
async function answer(payload, env) {
  const [best, backup] = modelsOf(env);
  if (!best) throw new Error("sin modelos configurados");
  const settle = (model) => askGemini(model, payload, env).then(
    (reply) => ({ ok: true, reply }),
    (error) => { console.error(error.message); return { ok: false, error }; });
  const bestTry = settle(best);
  const backupTry = backup ? settle(backup) : null;
  const first = await Promise.race([bestTry, new Promise((r) => setTimeout(() => r(null), PREFERRED_WAIT_MS))]);
  if (first?.ok) return first.reply;
  if (backupTry) {
    const b = await backupTry;
    if (b.ok) return b.reply;
  }
  // El respaldo falló: se espera al mejor hasta su tiempo máximo.
  const late = await bestTry;
  if (late.ok) return late.reply;
  throw late.error;
}

export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    if (request.method === "GET" && url.pathname === "/health")
      // ok solo si hay clave: sin ella el juego pasa directo a las frases del guion.
      return json(200, { ok: Boolean(env.GEMINI_API_KEY), provider: "gemini", models: modelsOf(env), keyInEnv: Boolean(env.GEMINI_API_KEY) });
    if (request.method !== "POST" || url.pathname !== "/dialogue") return json(404, { error: "no encontrado" });
    // Solo el juego manda esta cabecera. No es un secreto (el juego se puede descompilar), pero filtra el tráfico casual.
    if (request.headers.get("x-caso404-client") == null) return json(403, { error: "cliente no reconocido" });
    if (!env.GEMINI_API_KEY) return json(503, { error: "el servidor no tiene clave configurada" });
    if (!(await allowed(request, env))) return json(429, { error: "demasiadas preguntas; espera un minuto" });

    const body = await request.text();
    if (body.length > MAX_BODY) return json(413, { error: "pedido demasiado grande" });
    let payload;
    try {
      payload = RequestSchema.parse(JSON.parse(body));
    } catch {
      return json(400, { error: "pedido inválido" });
    }
    try {
      return json(200, await answer(payload, env));
    } catch {
      return json(502, { error: "la IA no respondió" });
    }
  },
};
