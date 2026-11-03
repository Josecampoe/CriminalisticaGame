using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Caso404.AI
{
    /// <summary>
    /// IA por HTTP a través de un servidor propio (carpeta Server/: worker.mjs publicado en Cloudflare, o proxy.mjs
    /// en este equipo). El juego nunca conoce la clave de API: vive solo en el servidor.
    /// Antes de preguntar comprueba que el servidor responda (GET /health); si no, devuelve null al instante durante
    /// 30 s y quien llama usa la frase de respaldo del guion, así el juego nunca se queda esperando.
    /// </summary>
    public class HttpAiClient : IAiClient
    {
        [Serializable]
        class Payload
        {
            public string mode;
            public string characterId;
            public string characterName;
            public string role;
            public string personality;
            public List<string> facts;
            public List<string> lies;
            public List<string> exposedLies;
            public List<string> styleExamples;
            public string topic;
            public string question;
            public string sceneId;
            public int alexTrust;
            public string model;
            public List<string> analyzedEvidence;
            public List<string> knownFacts;
            public List<string> history;
            // ALEX
            public List<string> alexClaims;
            public string alexTone;
            public string targetSuspect;
            public List<string> suspects;
            public string caseTitle;
        }

        const float HealthCacheSeconds = 30f;
        readonly AiSettings _settings;
        bool? _proxyUp;
        float _checkedAt = -999f;

        public HttpAiClient(AiSettings settings) => _settings = settings;

        public bool IsOffline => false;

        /// <summary>True si la última comprobación encontró el proxy encendido (null si aún no se comprobó).</summary>
        public bool? ProxyAvailable => _proxyUp;

        public async Task<AiReply> AskAsync(AiRequest request, CancellationToken ct)
        {
            if (!await IsProxyUpAsync(ct)) return null;
            var json = JsonUtility.ToJson(BuildPayload(request));
            // Un reintento si falla la red.
            for (int attempt = 0; attempt < 2; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                var text = await SendAsync(UnityWebRequest.kHttpVerbPOST, _settings.ProxyUrl, json, Mathf.Max(1, _settings.TimeoutSeconds), ct);
                if (text == null) continue;
                try { return JsonUtility.FromJson<AiReply>(text); }
                catch (Exception) { return null; }
            }
            return null;
        }

        Payload BuildPayload(AiRequest request)
        {
            var c = request.Character;
            var payload = new Payload
            {
                mode = request.IsAlex ? "alex" : "character",
                characterId = c.Id,
                characterName = c.DisplayName,
                role = c.Role,
                personality = c.Personality,
                styleExamples = request.StyleExamples.Count > 0 ? request.StyleExamples
                    : new List<string> { c.Fallback.Crime.Text, c.Fallback.OtherPerson.Text, c.Fallback.Evidence.Text }
                        .Where(t => !string.IsNullOrEmpty(t)).ToList(),
                topic = request.Topic.ToString(),
                question = Truncate(request.Question, _settings.MaxQuestionLength),
                sceneId = request.SceneId,
                alexTrust = request.AlexTrust,
                model = _settings.Model,
                analyzedEvidence = request.AnalyzedEvidence,
                knownFacts = request.KnownFacts,
                history = request.History,
            };
            if (request.IsAlex)
            {
                // ALEX no recibe la verdad del caso: solo sus afirmaciones, lo ya analizado y a quién debe empujar.
                payload.facts = new List<string>();
                payload.lies = new List<string>();
                payload.exposedLies = new List<string>();
                payload.alexClaims = request.AlexClaims;
                payload.alexTone = request.AlexTone;
                payload.targetSuspect = request.AlexTarget;
                payload.suspects = request.Suspects;
                payload.caseTitle = request.CaseTitle;
                return payload;
            }
            // Solo lo que el personaje sabe; sus secretos únicamente si el jugador ya los descubrió.
            payload.facts = c.KnownFacts
                .Where(f => c.Secrets.All(s => s.FactId != f.Id) || request.KnownFacts.Contains(f.Id))
                .Select(f => $"{f.Id}: {f.Text}").ToList();
            payload.lies = c.Lies.Where(l => l != null && !request.ExposedLieIds.Contains(l.Id)).Select(l => $"{l.Id}: {l.Statement}").ToList();
            payload.exposedLies = c.Lies.Where(l => l != null && request.ExposedLieIds.Contains(l.Id)).Select(l => $"{l.Id}: {l.Statement}").ToList();
            return payload;
        }

        /// <summary>True si el servidor responde y tiene la IA lista (GET /health, con caché de 30 s).</summary>
        public Task<bool> CheckAsync(CancellationToken ct) => IsProxyUpAsync(ct);

        /// <summary>Comprueba (con caché de 30 s) que el proxy responda en /health.</summary>
        async Task<bool> IsProxyUpAsync(CancellationToken ct)
        {
            if (_proxyUp.HasValue && Time.realtimeSinceStartup - _checkedAt < HealthCacheSeconds) return _proxyUp.Value;
            var url = _settings.ProxyUrl.EndsWith("/dialogue") ? _settings.ProxyUrl.Substring(0, _settings.ProxyUrl.Length - "/dialogue".Length) + "/health" : _settings.ProxyUrl;
            var text = await SendAsync(UnityWebRequest.kHttpVerbGET, url, null, Mathf.Max(1, _settings.HealthTimeoutSeconds), ct);
            _proxyUp = text != null && text.Contains("\"ok\":true");
            _checkedAt = Time.realtimeSinceStartup;
            if (!_proxyUp.Value) Core.GameLog.Warn("Proxy de IA apagado o inalcanzable: se usan las frases del guion.");
            return _proxyUp.Value;
        }

        static async Task<string> SendAsync(string method, string url, string json, int timeoutSeconds, CancellationToken ct)
        {
            using var req = new UnityWebRequest(url, method) { downloadHandler = new DownloadHandlerBuffer(), timeout = timeoutSeconds };
            req.SetRequestHeader(AiSettings.ClientHeader, Application.version);
            if (json != null)
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                req.SetRequestHeader("Content-Type", "application/json");
            }
            var op = req.SendWebRequest();
            var tcs = new TaskCompletionSource<bool>();
            op.completed += _ => tcs.TrySetResult(true);
            using (ct.Register(() => { req.Abort(); tcs.TrySetCanceled(); }))
                await tcs.Task;
            return req.result == UnityWebRequest.Result.Success ? req.downloadHandler.text : null;
        }

        static string Truncate(string s, int max) => string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max);
    }
}
