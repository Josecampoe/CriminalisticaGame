using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Caso404.Data
{
    /// <summary>Requisitos de una acusación para un final o una variante. -1 en un máximo = sin límite.</summary>
    [Serializable]
    public class AccusationRequirement
    {
        [SerializeField] private List<CharacterProfile> _accusedAnyOf = new();
        [SerializeField] private int _minSolidProofs;
        [SerializeField] private int _maxSolidProofs = -1;
        [SerializeField] private int _minAlexContradictions;
        [SerializeField] private int _maxAlexContradictions = -1;
        [SerializeField] private Condition _extra = new();

        public IReadOnlyList<CharacterProfile> AccusedAnyOf { get => _accusedAnyOf; internal set => _accusedAnyOf = new(value); }
        public int MinSolidProofs { get => _minSolidProofs; internal set => _minSolidProofs = value; }
        public int MaxSolidProofs { get => _maxSolidProofs; internal set => _maxSolidProofs = value; }
        public int MinAlexContradictions { get => _minAlexContradictions; internal set => _minAlexContradictions = value; }
        public int MaxAlexContradictions { get => _maxAlexContradictions; internal set => _maxAlexContradictions = value; }
        public Condition Extra { get => _extra; internal set => _extra = value; }

        /// <summary>Evalúa el requisito. Lógica pura.</summary>
        public bool IsMet(string accusedId, int solidProofs, int alexContradictions, IGameStateReader state)
        {
            if (_accusedAnyOf.Count > 0 && !_accusedAnyOf.Any(c => c != null && c.Id == accusedId)) return false;
            if (solidProofs < _minSolidProofs) return false;
            if (_maxSolidProofs >= 0 && solidProofs > _maxSolidProofs) return false;
            if (alexContradictions < _minAlexContradictions) return false;
            if (_maxAlexContradictions >= 0 && alexContradictions > _maxAlexContradictions) return false;
            return _extra == null || _extra.Evaluate(state);
        }
    }

    /// <summary>Texto alternativo dentro de un mismo final (ej. las tres versiones del final incompleto).</summary>
    [Serializable]
    public class EndingVariant
    {
        [SerializeField] private string _id;
        [SerializeField] private AccusationRequirement _requirement = new();
        [SerializeField, TextArea(3, 8)] private string _text;

        public string Id { get => _id; internal set => _id = value; }
        public AccusationRequirement Requirement { get => _requirement; internal set => _requirement = value; }
        public string Text { get => _text; internal set => _text = value; }
    }

    /// <summary>Final (end_). Se evalúan por prioridad (1 primero) y gana el primero que se cumple.</summary>
    [CreateAssetMenu(menuName = "Caso404/Final", fileName = "end_")]
    public class EndingData : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _caseId;
        [SerializeField] private int _priority;
        [SerializeField] private string _title;
        [SerializeField] private AccusationRequirement _requirement = new();
        [SerializeField, TextArea(3, 8)] private string _epilogue;
        [SerializeField] private List<EndingVariant> _variants = new();
        [SerializeField, TextArea] private string _closingText;
        [SerializeField] private DecisionData _followUpDecision;

        public string Id { get => _id; internal set => _id = value; }
        public string CaseId { get => _caseId; internal set => _caseId = value; }
        public int Priority { get => _priority; internal set => _priority = value; }
        public string Title { get => _title; internal set => _title = value; }
        public AccusationRequirement Requirement { get => _requirement; internal set => _requirement = value; }
        /// <summary>Texto principal (se usa si ninguna variante aplica).</summary>
        public string Epilogue { get => _epilogue; internal set => _epilogue = value; }
        public IReadOnlyList<EndingVariant> Variants { get => _variants; internal set => _variants = new(value); }
        /// <summary>Texto de cierre común (ej. "Tres meses después...").</summary>
        public string ClosingText { get => _closingText; internal set => _closingText = value; }
        /// <summary>Decisión que se abre tras este final (ej. dec_c2_alex).</summary>
        public DecisionData FollowUpDecision { get => _followUpDecision; internal set => _followUpDecision = value; }
    }
}
