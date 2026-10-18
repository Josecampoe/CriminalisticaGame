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
    /// IA por HTTP a través de un servidor proxy propio (carpeta Server/, fuera de Assets).
    /// El juego nunca conoce la clave de API: el proxy la lee de una variable de entorno.
    /// Preparado pero no usado en esta versión (AiSettings.UseOffline = true).
    /// </summary>
    public class HttpAiClient : IAiClient
    {
        [Serializable]
        class Payload
        {
            public string characterId;
            public string characterName;
            public string role;
            public string personality;
            public List<string> facts;
            public List<string> lies;
            public List<string> styleExamples;
            public string topic;
            public string question;
            public string sceneId;
            public int alexTrust;
            public string model;
            public List<string> analyzedEvidence;
            public List<string> knownFacts;
            public List<string> history;
        }

        readonly AiSettings _settings;

        public HttpAiClient(AiSettings settings) => _settings = settings;

        public bool IsOffline => false;

        public async Task<AiReply> AskAsync(AiRequest request, CancellationToken ct)
        {
            var payload = new Payload
            {
                characterId = request.Character.Id,
                characterName = request.Character.DisplayName,
                role = request.Character.Role,
                personality = request.Character.Personality,
                // Solo lo que el personaje sabe; sus secretos únicamente si el jugador ya los descubrió.
                facts = request.Character.KnownFacts
                    .Where(f => request.Character.Secrets.All(s => s.FactId != f.Id) || request.KnownFacts.Contains(f.Id))
                    .Select(f => $"{f.Id}: {f.Text}").ToList(),
                lies = request.Character.Lies.Where(l => l != null).Select(l => $"{l.Id}: {l.Statement}").ToList(),
                styleExamples = new List<string> { request.Character.Fallback.Crime.Text, request.Character.Fallback.OtherPerson.Text, request.Character.Fallback.Evidence.Text },
                topic = request.Topic.ToString(),
                question = Truncate(request.Question, _settings.MaxQuestionLength),
                sceneId = request.SceneId,
                alexTrust = request.AlexTrust,
                model = _settings.Model,
                analyzedEvidence = request.AnalyzedEvidence,
                knownFacts = request.KnownFacts,
                history = request.History
            };
            var json = JsonUtility.ToJson(payload);
            // Un reintento si falla la red.
            for (int attempt = 0; attempt < 2; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                var text = await PostAsync(json, ct);
                if (text == null) continue;
                try { return JsonUtility.FromJson<AiReply>(text); }
                catch (Exception) { return null; }
            }
            return null;
        }

        async Task<string> PostAsync(string json, CancellationToken ct)
        {
            using var req = new UnityWebRequest(_settings.ProxyUrl, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = Mathf.Max(1, _settings.TimeoutSeconds)
            };
            req.SetRequestHeader("Content-Type", "application/json");
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
