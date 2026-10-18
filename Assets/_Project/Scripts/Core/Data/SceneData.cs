using System;
using System.Collections.Generic;
using UnityEngine;

namespace Caso404.Data
{
    /// <summary>Personaje presente en una escena si se cumple la condición.</summary>
    [Serializable]
    public class ScenePresence
    {
        [SerializeField] private CharacterProfile _character;
        [SerializeField] private Condition _condition = new();

        public ScenePresence() { }
        public ScenePresence(CharacterProfile character, Condition condition = null) { _character = character; _condition = condition ?? new Condition(); }
        public CharacterProfile Character => _character;
        public Condition Condition => _condition;
    }

    /// <summary>Datos de una escena jugable (c1_s1...).</summary>
    [CreateAssetMenu(menuName = "Caso404/Escena", fileName = "c1_s")]
    public class SceneData : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _caseId;
        [SerializeField] private string _unitySceneName;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _ambience;
        [SerializeField, TextArea(2, 6)] private string _introText;
        [SerializeField] private List<EvidenceData> _evidence = new();
        [SerializeField] private List<ScenePresence> _characters = new();
        [SerializeField] private HypothesisData _hypothesis;
        [SerializeField] private DecisionData _closingDecision;
        [SerializeField] private List<string> _requiredCompletedSceneIds = new();

        public string Id { get => _id; internal set => _id = value; }
        public string CaseId { get => _caseId; internal set => _caseId = value; }
        public string UnitySceneName { get => _unitySceneName; internal set => _unitySceneName = value; }
        public string DisplayName { get => _displayName; internal set => _displayName = value; }
        public string Ambience { get => _ambience; internal set => _ambience = value; }
        public string IntroText { get => _introText; internal set => _introText = value; }
        public IReadOnlyList<EvidenceData> Evidence { get => _evidence; internal set => _evidence = new(value); }
        public IReadOnlyList<ScenePresence> Characters { get => _characters; internal set => _characters = new(value); }
        public HypothesisData Hypothesis { get => _hypothesis; internal set => _hypothesis = value; }
        public DecisionData ClosingDecision { get => _closingDecision; internal set => _closingDecision = value; }
        /// <summary>Escenas que hay que completar antes de entrar (la Comisaría exige s2 y s3).</summary>
        public IReadOnlyList<string> RequiredCompletedSceneIds { get => _requiredCompletedSceneIds; internal set => _requiredCompletedSceneIds = new(value); }
    }
}
