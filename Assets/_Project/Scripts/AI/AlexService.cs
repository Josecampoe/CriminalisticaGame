using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caso404.Core;
using Caso404.Data;
using Caso404.Evidence;

namespace Caso404.AI
{
    /// <summary>Tono de ALEX según alexTrust (GUION §1).</summary>
    public enum AlexTone { Warm, Formal, Cold }

    /// <summary>Resultado de pulsar Cuestionar.</summary>
    public readonly struct QuestionOutcome
    {
        public readonly bool Success;
        public readonly string Response;
        public QuestionOutcome(bool success, string response) { Success = success; Response = response; }
    }

    /// <summary>
    /// Agenda de ALEX: lanza sus afirmaciones (mentiras) cuando se encuentra o analiza la evidencia que las dispara,
    /// y resuelve Aceptar / Cuestionar. Es lógica determinista: la IA nunca decide si hubo contradicción.
    /// </summary>
    public class AlexService : IDisposable
    {
        public event Action<LieData> ClaimRaised;
        public event Action<LieData> ClaimRefuted;

        readonly GameStateService _state;
        readonly CaseData _case;
        readonly AlexProfile _alex;
        readonly EvidenceService _evidence;
        readonly IAiClient _ai;
        /// <summary>Clave de ALEX en la memoria de conversaciones del GameState.</summary>
        public const string MemoryId = "alex";

        /// <summary>Longitud máxima de una pregunta escrita a ALEX (skill dialogo-ia).</summary>
        public const int MaxQuestionLength = 200;

        public AlexService(GameStateService state, CaseData caseData, AlexProfile alex, EvidenceService evidence, IAiClient ai = null)
        {
            _state = state;
            _case = caseData;
            _alex = alex;
            _evidence = evidence;
            _ai = ai ?? new OfflineAiClient();
            _evidence.EvidenceCollected += OnEvidenceCollected;
            _evidence.EvidenceAnalyzed += OnEvidenceAnalyzed;
        }

        public void Dispose()
        {
            _evidence.EvidenceCollected -= OnEvidenceCollected;
            _evidence.EvidenceAnalyzed -= OnEvidenceAnalyzed;
        }

        public AlexProfile Profile => _alex;
        public int Trust => _state.AlexTrust;
        public int ContradictionCount => _state.GetContradictionCount(_case.Id);

        /// <summary>Tono actual: cálido (≥60), formal (30–59) o frío (&lt;30).</summary>
        public AlexTone Tone => Trust >= _alex.HighTrustThreshold ? AlexTone.Warm
            : Trust >= _alex.LowTrustThreshold ? AlexTone.Formal : AlexTone.Cold;

        /// <summary>Afirmaciones que ALEX ya hizo en este caso, en orden.</summary>
        public IEnumerable<LieData> RaisedClaims =>
            _state.State.raisedAlexClaimIds.Select(id => _case.FindLie(id)).Where(l => l != null && l.IsAlexLie);

        public bool IsRefuted(LieData claim) => claim != null && _state.IsContradictionDetected(_case.Id, claim.Id);
        public bool IsAccepted(LieData claim) => claim != null && _state.IsAlexClaimAccepted(claim.Id);

        /// <summary>El jugador acepta la sugerencia: sube la confianza una sola vez.</summary>
        public string Accept(LieData claim)
        {
            if (claim == null || IsRefuted(claim) || IsAccepted(claim)) return _alex.AcceptedLine;
            _state.MarkAlexClaimAccepted(claim.Id);
            _state.ChangeTrust(_alex.AcceptTrustDelta);
            return _alex.AcceptedLine;
        }

        /// <summary>
        /// El jugador cuestiona la afirmación presentando una evidencia analizada.
        /// Si la refuta, cuenta como contradicción y baja la confianza. Aunque antes la hubiera aceptado, se puede cuestionar.
        /// </summary>
        public QuestionOutcome Question(LieData claim, EvidenceData evidence)
        {
            if (claim == null || !claim.IsAlexLie) return new QuestionOutcome(false, _alex.QuestionedFail);
            if (IsRefuted(claim)) return new QuestionOutcome(true, _alex.QuestionedSuccessLow);
            if (evidence == null || !_evidence.IsAnalyzed(evidence) || !claim.IsRefutedBy(evidence, _state))
                return new QuestionOutcome(false, _alex.QuestionedFail);

            _state.AddContradiction(_case.Id, claim.Id);
            _state.ExposeLie(claim.Id);
            _state.ChangeTrust(_alex.QuestionSuccessTrustDelta);
            ClaimRefuted?.Invoke(claim);
            var response = Trust < _alex.LowTrustThreshold ? _alex.QuestionedSuccessLow : _alex.QuestionedSuccessHigh;
            return new QuestionOutcome(true, response);
        }

        /// <summary>True si las preguntas libres pueden ir a la IA (si no, ALEX responde con frases del guion).</summary>
        public bool UsesAi => !_ai.IsOffline;

        /// <summary>
        /// Pregunta escrita a ALEX. Nunca falla: si la IA no responde o su respuesta no pasa la validación, ALEX usa
        /// una frase del guion. La respuesta solo es texto: no cambia la confianza, no da pistas ni decide nada.
        /// </summary>
        public async Task<string> AskAsync(string question, CancellationToken ct)
        {
            question = (question ?? "").Trim();
            if (question.Length == 0) return "";
            if (question.Length > MaxQuestionLength) question = question.Substring(0, MaxQuestionLength);

            AiReply reply = null;
            if (!_ai.IsOffline)
            {
                try { reply = await _ai.AskAsync(BuildRequest(question), ct); }
                catch (OperationCanceledException) { throw; }
                catch (Exception e) { GameLog.Warn($"ALEX sin respuesta de la IA, se usa el guion: {e.Message}"); }
            }
            var text = IsValidReply(reply) ? reply.reply.Trim() : FallbackAnswer(question);
            _state.RememberConversation(MemoryId, $"Rivera: {question}");
            _state.RememberConversation(MemoryId, $"ALEX: {text}");
            return text;
        }

        /// <summary>
        /// Una respuesta de la IA vale si tiene texto razonable y solo usa afirmaciones de ALEX ya hechas y no refutadas
        /// (si la IA intenta sostener una mentira que el jugador ya desmintió, se descarta).
        /// </summary>
        public bool IsValidReply(AiReply reply)
        {
            if (reply == null || string.IsNullOrWhiteSpace(reply.reply) || reply.reply.Length > 600) return false;
            var allowed = RaisedClaims.Where(c => !IsRefuted(c)).Select(c => c.Id).ToHashSet();
            return reply.usedLieIds == null || reply.usedLieIds.All(allowed.Contains);
        }

        /// <summary>Frase del guion según el tema de la pregunta (modo sin conexión o si la IA falla).</summary>
        public string FallbackAnswer(string question)
        {
            var q = (question ?? "").ToLowerInvariant();
            var lines = _case.AlexLines;
            string Pick(FallbackLine l) => string.IsNullOrEmpty(l?.Text) ? _case.AlexIntroLine : l.Text;
            if (ContainsAny(q, "error", "404", "borr", "archivo", "fragmento", "corrupt", "mient", "manipul", "confi", "ocult", "por qué tú", "por que tu"))
                return Pick(lines.Press);
            if (ContainsAny(q, "prueba", "evidencia", "huella", "ventana", "colilla", "cámara", "camara", "video", "análisis", "analisis", "recibo", "llave"))
                return Pick(lines.Evidence);
            return Pick(lines.Crime);
        }

        AiRequest BuildRequest(string question)
        {
            string Status(LieData c) => IsRefuted(c) ? "refutada" : IsAccepted(c) ? "aceptada" : "pendiente";
            var style = new[] { _case.AlexIntroLine, _case.AlexLines.Crime?.Text, _case.AlexLines.Evidence?.Text }
                .Where(t => !string.IsNullOrEmpty(t)).ToList();
            return new AiRequest
            {
                Character = _alex,
                Topic = DialogueTopic.FreeText,
                Question = question,
                SceneId = _state.State.currentSceneId,
                AlexTrust = _state.AlexTrust,
                AnalyzedEvidence = _evidence.AnalyzedEvidence.Select(e => $"{e.Id}: {e.AnalysisResult}").ToList(),
                History = _state.GetConversation(MemoryId).TakeLast(8).ToList(),
                IsAlex = true,
                AlexClaims = RaisedClaims.Select(c => $"{c.Id}: {c.Statement} [{Status(c)}]").ToList(),
                AlexTone = Tone.ToString(),
                AlexTarget = _case.AlexTarget != null ? _case.AlexTarget.DisplayName : "el sospechoso más evidente",
                Suspects = _case.AllCharacters.Select(c => $"{c.DisplayName}: {c.Role}").ToList(),
                CaseTitle = _case.Title,
                StyleExamples = style,
            };
        }

        static bool ContainsAny(string text, params string[] words)
        {
            foreach (var w in words) if (text.Contains(w)) return true;
            return false;
        }

        void OnEvidenceCollected(EvidenceData ev) => RaiseClaimsFor(ev, AlexClaimTrigger.OnEvidenceFound);
        void OnEvidenceAnalyzed(EvidenceData ev) => RaiseClaimsFor(ev, AlexClaimTrigger.OnEvidenceAnalyzed);

        void RaiseClaimsFor(EvidenceData ev, AlexClaimTrigger trigger)
        {
            foreach (var claim in _case.AlexClaims)
            {
                if (claim.AlexTrigger != trigger || claim.AlexTriggerEvidence == null || claim.AlexTriggerEvidence.Id != ev.Id) continue;
                if (_state.MarkAlexClaimRaised(claim.Id)) ClaimRaised?.Invoke(claim);
            }
        }
    }
}
