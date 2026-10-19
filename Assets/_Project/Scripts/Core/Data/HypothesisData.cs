using System;
using System.Collections.Generic;
using UnityEngine;

namespace Caso404.Data
{
    /// <summary>Opción de una pregunta de hipótesis. Bloqueada hasta cumplir su condición: se deduce, no se adivina.</summary>
    [Serializable]
    public class HypothesisOption
    {
        [SerializeField] private string _id;
        [SerializeField] private string _text;
        [SerializeField] private Condition _unlockCondition = new();
        [SerializeField] private bool _isCorrect;

        public HypothesisOption() { }
        public HypothesisOption(string id, string text, bool isCorrect, Condition unlock = null)
        { _id = id; _text = text; _isCorrect = isCorrect; _unlockCondition = unlock ?? new Condition(); }

        public string Id => _id;
        public string Text => _text;
        public Condition UnlockCondition => _unlockCondition;
        public bool IsCorrect => _isCorrect;
    }

    /// <summary>Pregunta del tablero (¿Cómo entró? ¿Quién estuvo?).</summary>
    [Serializable]
    public class HypothesisQuestion
    {
        [SerializeField] private string _id;
        [SerializeField] private string _text;
        [SerializeField] private bool _multiSelect;
        [SerializeField] private List<HypothesisOption> _options = new();
        [SerializeField] private List<FlagData> _onCorrectSetFlags = new();

        public HypothesisQuestion() { }
        public HypothesisQuestion(string id, string text, bool multiSelect, IEnumerable<HypothesisOption> options, IEnumerable<FlagData> onCorrect = null)
        {
            _id = id; _text = text; _multiSelect = multiSelect; _options = new List<HypothesisOption>(options);
            _onCorrectSetFlags = onCorrect != null ? new List<FlagData>(onCorrect) : new List<FlagData>();
        }

        public string Id => _id;
        public string Text => _text;
        /// <summary>Si es de selección múltiple, la respuesta correcta es el conjunto exacto de opciones correctas.</summary>
        public bool MultiSelect => _multiSelect;
        public IReadOnlyList<HypothesisOption> Options => _options;
        public IReadOnlyList<FlagData> OnCorrectSetFlags => _onCorrectSetFlags;
    }

    /// <summary>Hipótesis de una escena (hyp_c1_s1...).</summary>
    [CreateAssetMenu(menuName = "Caso404/Hipótesis", fileName = "hyp_")]
    public class HypothesisData : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _sceneId;
        [SerializeField] private List<HypothesisQuestion> _questions = new();

        public string Id { get => _id; internal set => _id = value; }
        public string SceneId { get => _sceneId; internal set => _sceneId = value; }
        public IReadOnlyList<HypothesisQuestion> Questions { get => _questions; internal set => _questions = new(value); }
    }
}
