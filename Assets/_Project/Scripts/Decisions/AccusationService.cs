using System.Collections.Generic;
using System.Linq;
using Caso404.Core;
using Caso404.Data;

namespace Caso404.Decisions
{
    /// <summary>Acusación final: valida las pruebas elegidas, calcula el final y lo registra en el GameState.</summary>
    public class AccusationService
    {
        readonly GameStateService _state;
        readonly CaseData _case;

        public AccusationService(GameStateService state, CaseData caseData)
        {
            _state = state;
            _case = caseData;
        }

        /// <summary>True si las pruebas son válidas: exactamente las requeridas, distintas y analizadas.</summary>
        public bool AreProofsValid(IReadOnlyCollection<string> proofIds, out string reason)
        {
            reason = "";
            var distinct = proofIds?.Distinct().ToList() ?? new List<string>();
            if (distinct.Count != _case.ProofsToChoose) { reason = $"Elige {_case.ProofsToChoose} pruebas distintas."; return false; }
            if (distinct.Any(id => !_state.IsEvidenceAnalyzed(id))) { reason = "Solo puedes presentar evidencias analizadas."; return false; }
            return true;
        }

        /// <summary>Acusa a un sospechoso con sus pruebas. Devuelve null si la acusación no es válida.</summary>
        public EndingResult Accuse(string suspectId, IReadOnlyCollection<string> proofIds)
        {
            if (_case.FindSuspect(suspectId) == null || !AreProofsValid(proofIds, out _)) return null;
            var result = EndingEvaluator.Evaluate(_case, suspectId, proofIds, _state.GetContradictionCount(_case.Id), _state);
            _state.RecordAccusation(new AccusationRecord
            {
                caseId = _case.Id,
                suspectId = suspectId,
                proofIds = proofIds.ToList(),
                endingId = result.Ending != null ? result.Ending.Id : "",
                variantId = result.Variant != null ? result.Variant.Id : ""
            });
            if (result.Ending != null)
            {
                _state.AddFlag(result.Ending.Id); // los end_c1_* pasan al caso 2 como flags
                _state.LearnFacts(result.Ending.RevealsFactIds);
            }
            return result;
        }
    }
}
