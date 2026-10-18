using System;
using System.Collections.Generic;
using UnityEngine;

namespace Caso404.Data
{
    /// <summary>Opción de una decisión: qué flags activa, cuánto cambia la confianza en ALEX y a qué escena lleva.</summary>
    [Serializable]
    public class DecisionOption
    {
        [SerializeField] private string _id;
        [SerializeField] private string _text;
        [SerializeField] private Condition _condition = new();
        [SerializeField] private List<FlagData> _setsFlags = new();
        [SerializeField] private int _alexTrustDelta;
        [SerializeField] private string _nextSceneId;
        [SerializeField] private bool _letPlayerChooseNextScene;
        [SerializeField, TextArea(2, 5)] private string _resultText;
        [SerializeField] private bool _resultSpokenByAlex;

        public string Id { get => _id; internal set => _id = value; }
        public string Text { get => _text; internal set => _text = value; }
        public Condition Condition { get => _condition; internal set => _condition = value; }
        public IReadOnlyList<FlagData> SetsFlags { get => _setsFlags; internal set => _setsFlags = new(value); }
        public int AlexTrustDelta { get => _alexTrustDelta; internal set => _alexTrustDelta = value; }
        /// <summary>Escena siguiente (vacío = la decisión no cambia de escena).</summary>
        public string NextSceneId { get => _nextSceneId; internal set => _nextSceneId = value; }
        /// <summary>El jugador elige entre las escenas pendientes (ej. dec_c1_s1 A).</summary>
        public bool LetPlayerChooseNextScene { get => _letPlayerChooseNextScene; internal set => _letPlayerChooseNextScene = value; }
        /// <summary>Texto que se muestra tras elegir (ej. la nota del cuaderno).</summary>
        public string ResultText { get => _resultText; internal set => _resultText = value; }
        public bool ResultSpokenByAlex { get => _resultSpokenByAlex; internal set => _resultSpokenByAlex = value; }
    }

    /// <summary>Decisión (dec_). Las que afectan el final son irreversibles y piden confirmación.</summary>
    [CreateAssetMenu(menuName = "Caso404/Decisión", fileName = "dec_")]
    public class DecisionData : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField, TextArea] private string _prompt;
        [SerializeField] private bool _irreversible = true;
        [SerializeField] private string _alexRecommendedOptionId;
        [SerializeField, TextArea] private string _alexRecommendationText;
        [SerializeField] private List<DecisionOption> _options = new();

        public string Id { get => _id; internal set => _id = value; }
        public string Prompt { get => _prompt; internal set => _prompt = value; }
        public bool Irreversible { get => _irreversible; internal set => _irreversible = value; }
        public string AlexRecommendedOptionId { get => _alexRecommendedOptionId; internal set => _alexRecommendedOptionId = value; }
        public string AlexRecommendationText { get => _alexRecommendationText; internal set => _alexRecommendationText = value; }
        public IReadOnlyList<DecisionOption> Options { get => _options; internal set => _options = new(value); }

        /// <summary>Busca una opción por id.</summary>
        public DecisionOption FindOption(string optionId) => _options.Find(o => o.Id == optionId);
    }
}
