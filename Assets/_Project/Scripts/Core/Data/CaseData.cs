using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Caso404.Data
{
    /// <summary>Un caso completo. Agrupa todos sus datos y define culpable, pruebas sólidas y finales.</summary>
    [CreateAssetMenu(menuName = "Caso404/Caso", fileName = "c")]
    public class CaseData : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _title;
        [SerializeField, TextArea(3, 6)] private string _introText;
        [SerializeField, TextArea] private string _alexIntroLine;
        [SerializeField] private FallbackLines _alexLines = new();
        [SerializeField] private List<SceneData> _scenes = new();
        [SerializeField] private List<CharacterProfile> _suspects = new();
        [SerializeField] private CharacterProfile _culprit;
        [SerializeField] private CharacterProfile _alexTarget;
        [SerializeField] private List<EvidenceData> _evidence = new();
        [SerializeField] private List<LieData> _lies = new();
        [SerializeField] private List<FlagData> _flags = new();
        [SerializeField] private List<HypothesisData> _hypotheses = new();
        [SerializeField] private List<DecisionData> _decisions = new();
        [SerializeField] private List<EndingData> _endings = new();
        [SerializeField] private List<EvidenceData> _solidProofs = new();
        [SerializeField] private int _proofsToChoose = 3;

        public string Id { get => _id; internal set => _id = value; }
        public string Title { get => _title; internal set => _title = value; }
        public string IntroText { get => _introText; internal set => _introText = value; }
        public string AlexIntroLine { get => _alexIntroLine; internal set => _alexIntroLine = value; }
        /// <summary>Frases de respaldo de ALEX en este caso (diálogo libre sin conexión).</summary>
        public FallbackLines AlexLines { get => _alexLines; internal set => _alexLines = value; }
        public IReadOnlyList<SceneData> Scenes { get => _scenes; internal set => _scenes = new(value); }
        /// <summary>Personajes que se pueden acusar.</summary>
        public IReadOnlyList<CharacterProfile> Suspects { get => _suspects; internal set => _suspects = new(value); }
        public CharacterProfile Culprit { get => _culprit; internal set => _culprit = value; }
        public CharacterProfile AlexTarget { get => _alexTarget; internal set => _alexTarget = value; }
        public IReadOnlyList<EvidenceData> Evidence { get => _evidence; internal set => _evidence = new(value); }
        public IReadOnlyList<LieData> Lies { get => _lies; internal set => _lies = new(value); }
        public IReadOnlyList<FlagData> Flags { get => _flags; internal set => _flags = new(value); }
        public IReadOnlyList<HypothesisData> Hypotheses { get => _hypotheses; internal set => _hypotheses = new(value); }
        public IReadOnlyList<DecisionData> Decisions { get => _decisions; internal set => _decisions = new(value); }
        public IReadOnlyList<EndingData> Endings { get => _endings; internal set => _endings = new(value); }
        /// <summary>Pruebas sólidas contra el culpable (GUION §3.6).</summary>
        public IReadOnlyList<EvidenceData> SolidProofs { get => _solidProofs; internal set => _solidProofs = new(value); }
        public int ProofsToChoose { get => _proofsToChoose; internal set => _proofsToChoose = value; }

        public EvidenceData FindEvidence(string id) => _evidence.FirstOrDefault(e => e != null && e.Id == id);
        public LieData FindLie(string id) => _lies.FirstOrDefault(l => l != null && l.Id == id);
        public SceneData FindScene(string id) => _scenes.FirstOrDefault(s => s != null && s.Id == id);
        public DecisionData FindDecision(string id) => _decisions.FirstOrDefault(d => d != null && d.Id == id);
        public CharacterProfile FindSuspect(string id) => _suspects.FirstOrDefault(c => c != null && c.Id == id);
        public bool IsSolidProof(string evidenceId) => _solidProofs.Any(e => e != null && e.Id == evidenceId);
        /// <summary>Mentiras de ALEX en este caso (sus "afirmaciones").</summary>
        public IEnumerable<LieData> AlexClaims => _lies.Where(l => l != null && l.IsAlexLie);
    }
}
