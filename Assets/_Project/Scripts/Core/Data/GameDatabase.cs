using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Caso404.Data
{
    /// <summary>Punto de entrada a todos los datos del juego. Permite buscar por id (para el guardado).</summary>
    [CreateAssetMenu(menuName = "Caso404/Base de datos", fileName = "GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        [SerializeField] private List<CaseData> _cases = new();
        [SerializeField] private AlexProfile _alex;

        public IReadOnlyList<CaseData> Cases { get => _cases; internal set => _cases = new(value); }
        public AlexProfile Alex { get => _alex; internal set => _alex = value; }

        /// <summary>Busca un caso por id.</summary>
        public CaseData FindCase(string caseId) => _cases.FirstOrDefault(c => c != null && c.Id == caseId);

        /// <summary>Busca una evidencia en todos los casos.</summary>
        public EvidenceData FindEvidence(string id) => _cases.Select(c => c.FindEvidence(id)).FirstOrDefault(e => e != null);

        /// <summary>Busca una escena en todos los casos.</summary>
        public SceneData FindScene(string id) => _cases.Select(c => c.FindScene(id)).FirstOrDefault(s => s != null);

        /// <summary>Busca una mentira en todos los casos.</summary>
        public LieData FindLie(string id) => _cases.Select(c => c.FindLie(id)).FirstOrDefault(l => l != null);
    }
}
