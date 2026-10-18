using System;
using System.Collections.Generic;
using System.Linq;
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

        public AlexService(GameStateService state, CaseData caseData, AlexProfile alex, EvidenceService evidence)
        {
            _state = state;
            _case = caseData;
            _alex = alex;
            _evidence = evidence;
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
            if (evidence == null || !_evidence.IsAnalyzed(evidence) || !claim.IsRefutedBy(evidence))
                return new QuestionOutcome(false, _alex.QuestionedFail);

            _state.AddContradiction(_case.Id, claim.Id);
            _state.ExposeLie(claim.Id);
            _state.ChangeTrust(_alex.QuestionSuccessTrustDelta);
            ClaimRefuted?.Invoke(claim);
            var response = Trust < _alex.LowTrustThreshold ? _alex.QuestionedSuccessLow : _alex.QuestionedSuccessHigh;
            return new QuestionOutcome(true, response);
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
