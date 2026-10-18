using System;
using System.Collections.Generic;

namespace Caso404.Core
{
    /// <summary>Contradicciones de ALEX detectadas en un caso (alexContradictions_c1 = lieIds.Count).</summary>
    [Serializable]
    public class CaseContradictions
    {
        public string caseId;
        public List<string> lieIds = new();
    }

    /// <summary>Respuesta del jugador a una pregunta de hipótesis.</summary>
    [Serializable]
    public class HypothesisAnswer
    {
        public string questionId;
        public List<string> optionIds = new();
        public bool correct;
    }

    /// <summary>Acusación final de un caso y el final que produjo.</summary>
    [Serializable]
    public class AccusationRecord
    {
        public string caseId;
        public string suspectId;
        public List<string> proofIds = new();
        public string endingId;
        public string variantId;
    }

    /// <summary>
    /// Estado único y serializable de la partida. Solo IDs (nunca referencias a assets),
    /// y solo listas porque JsonUtility no serializa diccionarios.
    /// Únicamente <see cref="GameStateService"/> lo modifica.
    /// </summary>
    [Serializable]
    public class GameState
    {
        public const int CurrentSaveVersion = 1;
        public const int DefaultAlexTrust = 60;

        public int saveVersion = CurrentSaveVersion;
        public string currentCaseId = "c1";
        public string currentSceneId = "";
        public List<string> completedSceneIds = new();

        public List<string> foundEvidenceIds = new();
        public List<string> analyzedEvidenceIds = new();
        public List<string> inspectedDetailEvidenceIds = new();
        public List<string> extraAnalysisDoneIds = new();

        public List<string> knownFactIds = new();
        public List<string> exposedLieIds = new();
        public List<string> flags = new();

        public int alexTrust = DefaultAlexTrust;
        public List<string> raisedAlexClaimIds = new();
        public List<string> acceptedAlexClaimIds = new();
        public List<CaseContradictions> alexContradictions = new();

        public List<HypothesisAnswer> hypothesisAnswers = new();
        public List<string> decisionsTaken = new();   // "dec_c1_s1:A"
        public List<AccusationRecord> accusations = new();
    }
}
