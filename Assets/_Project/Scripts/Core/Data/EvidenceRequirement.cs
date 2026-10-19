using System;
using UnityEngine;

namespace Caso404.Data
{
    /// <summary>
    /// Una evidencia que solo vale si existe un flag. Ej.: la cámara del parqueadero solo identifica el carro de Valeria
    /// tras el análisis externo; el portátil de Tomás solo refuta a ALEX abierto con la contraseña.
    /// </summary>
    [Serializable]
    public class EvidenceRequirement
    {
        [SerializeField] private EvidenceData _evidence;
        [SerializeField] private FlagData _requiredFlag;

        public EvidenceRequirement() { }
        public EvidenceRequirement(EvidenceData evidence, FlagData requiredFlag) { _evidence = evidence; _requiredFlag = requiredFlag; }

        public EvidenceData Evidence { get => _evidence; internal set => _evidence = value; }
        public FlagData RequiredFlag { get => _requiredFlag; internal set => _requiredFlag = value; }

        /// <summary>True si este requisito no aplica a la evidencia o si su flag existe.</summary>
        public bool Allows(EvidenceData evidence, IGameStateReader state) =>
            _evidence == null || evidence == null || _evidence.Id != evidence.Id || _requiredFlag == null || state.HasFlag(_requiredFlag.Id);
    }
}
