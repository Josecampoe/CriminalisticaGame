using System.Collections.Generic;
using System.Linq;
using Caso404.Data;

namespace Caso404.Decisions
{
    /// <summary>Final calculado para una acusación.</summary>
    public class EndingResult
    {
        public EndingData Ending;
        public EndingVariant Variant;
        public int SolidProofs;
        public int AlexContradictions;

        /// <summary>Texto a mostrar: la variante si aplica, si no el epílogo general.</summary>
        public string Text => Variant != null ? Variant.Text : Ending?.Epilogue;
    }

    /// <summary>
    /// Calcula el final de un caso. Lógica pura y determinista (regla de oro: el final nunca depende de la IA).
    /// Recorre los finales por prioridad (1 primero) y devuelve el primero cuyo requisito se cumple.
    /// </summary>
    public static class EndingEvaluator
    {
        /// <summary>Cuenta las pruebas sólidas entre las elegidas (solo cuentan si están analizadas).</summary>
        public static int CountSolidProofs(CaseData caseData, IEnumerable<string> proofIds, IGameStateReader state) =>
            (proofIds ?? Enumerable.Empty<string>()).Distinct()
                .Count(id => caseData.IsSolidProof(id) && state.IsEvidenceAnalyzed(id));

        /// <summary>Evalúa el final de una acusación.</summary>
        public static EndingResult Evaluate(CaseData caseData, string accusedId, IEnumerable<string> proofIds,
            int alexContradictions, IGameStateReader state)
        {
            int solid = CountSolidProofs(caseData, proofIds, state);
            var result = new EndingResult { SolidProofs = solid, AlexContradictions = alexContradictions };
            foreach (var ending in caseData.Endings.Where(e => e != null).OrderBy(e => e.Priority))
            {
                if (!ending.Requirement.IsMet(accusedId, solid, alexContradictions, state)) continue;
                result.Ending = ending;
                result.Variant = ending.Variants.FirstOrDefault(v => v.Requirement.IsMet(accusedId, solid, alexContradictions, state));
                return result;
            }
            return result;
        }
    }
}
