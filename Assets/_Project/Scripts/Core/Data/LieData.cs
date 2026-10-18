using System.Collections.Generic;
using UnityEngine;

namespace Caso404.Data
{
    /// <summary>Cuándo dice ALEX su afirmación falsa.</summary>
    public enum AlexClaimTrigger { None, OnEvidenceFound, OnEvidenceAnalyzed }

    /// <summary>
    /// Una mentira (lie_) de un personaje o de ALEX. Aquí vive también qué la refuta:
    /// es la única fuente para el sistema de contradicciones y para el validador de diseño justo.
    /// </summary>
    [CreateAssetMenu(menuName = "Caso404/Mentira", fileName = "lie_")]
    public class LieData : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _caseId;
        [SerializeField] private string _ownerId;
        [SerializeField] private bool _isAlexLie;
        [SerializeField, TextArea(2, 5)] private string _statement;
        [SerializeField] private List<EvidenceData> _refutedByEvidence = new();
        [SerializeField] private List<string> _refutedByFactIds = new();
        [SerializeField, TextArea(2, 5)] private string _exposedResponse;
        [SerializeField] private List<string> _revealsFactIdsOnExposed = new();
        [SerializeField] private List<FlagData> _setsFlagsOnExposed = new();
        [SerializeField] private List<EvidenceData> _grantsEvidenceOnExposed = new();
        [SerializeField] private AlexClaimTrigger _alexTrigger;
        [SerializeField] private EvidenceData _alexTriggerEvidence;

        public string Id { get => _id; internal set => _id = value; }
        public string CaseId { get => _caseId; internal set => _caseId = value; }
        /// <summary>Id del personaje que la dice ("carlos", "alex"...).</summary>
        public string OwnerId { get => _ownerId; internal set => _ownerId = value; }
        public bool IsAlexLie { get => _isAlexLie; internal set => _isAlexLie = value; }
        /// <summary>Lo que dice el personaje.</summary>
        public string Statement { get => _statement; internal set => _statement = value; }
        /// <summary>Cualquiera de estas evidencias (analizada) la refuta.</summary>
        public IReadOnlyList<EvidenceData> RefutedByEvidence { get => _refutedByEvidence; internal set => _refutedByEvidence = new(value); }
        /// <summary>Testimonios (hechos de otros personajes) que también la refutan.</summary>
        public IReadOnlyList<string> RefutedByFactIds { get => _refutedByFactIds; internal set => _refutedByFactIds = new(value); }
        /// <summary>Qué dice el personaje cuando lo descubren.</summary>
        public string ExposedResponse { get => _exposedResponse; internal set => _exposedResponse = value; }
        public IReadOnlyList<string> RevealsFactIdsOnExposed { get => _revealsFactIdsOnExposed; internal set => _revealsFactIdsOnExposed = new(value); }
        public IReadOnlyList<FlagData> SetsFlagsOnExposed { get => _setsFlagsOnExposed; internal set => _setsFlagsOnExposed = new(value); }
        /// <summary>Evidencias que el personaje entrega si se le refuta con una evidencia (no con un testimonio).</summary>
        public IReadOnlyList<EvidenceData> GrantsEvidenceOnExposed { get => _grantsEvidenceOnExposed; internal set => _grantsEvidenceOnExposed = new(value); }
        public AlexClaimTrigger AlexTrigger { get => _alexTrigger; internal set => _alexTrigger = value; }
        public EvidenceData AlexTriggerEvidence { get => _alexTriggerEvidence; internal set => _alexTriggerEvidence = value; }

        /// <summary>True si la evidencia refuta esta mentira.</summary>
        public bool IsRefutedBy(EvidenceData evidence) => evidence != null && _refutedByEvidence.Exists(e => e != null && e.Id == evidence.Id);

        /// <summary>True si el hecho (testimonio) refuta esta mentira.</summary>
        public bool IsRefutedByFact(string factId) => !string.IsNullOrEmpty(factId) && _refutedByFactIds.Contains(factId);
    }
}
