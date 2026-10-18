using System;
using System.Collections.Generic;
using UnityEngine;

namespace Caso404.Data
{
    /// <summary>Un hecho (f_) que el personaje conoce.</summary>
    [Serializable]
    public class FactEntry
    {
        [SerializeField] private string _id;
        [SerializeField, TextArea] private string _text;

        public FactEntry() { }
        public FactEntry(string id, string text) { _id = id; _text = text; }
        public string Id => _id;
        public string Text => _text;
    }

    /// <summary>Línea de respaldo (modo sin conexión) y los hechos que revela al decirla.</summary>
    [Serializable]
    public class FallbackLine
    {
        [SerializeField, TextArea(2, 4)] private string _text;
        [SerializeField] private List<string> _revealsFactIds = new();

        public FallbackLine() { }
        public FallbackLine(string text, params string[] reveals) { _text = text; _revealsFactIds = new List<string>(reveals); }
        public string Text => _text;
        public IReadOnlyList<string> RevealsFactIds => _revealsFactIds;
    }

    /// <summary>Las cuatro frases de respaldo del guion: Crimen, Otra persona, Evidencia, Presionar.</summary>
    [Serializable]
    public class FallbackLines
    {
        [SerializeField] private FallbackLine _crime = new();
        [SerializeField] private FallbackLine _otherPerson = new();
        [SerializeField] private FallbackLine _evidence = new();
        [SerializeField] private FallbackLine _press = new();

        public FallbackLine Crime { get => _crime; internal set => _crime = value; }
        public FallbackLine OtherPerson { get => _otherPerson; internal set => _otherPerson = value; }
        public FallbackLine Evidence { get => _evidence; internal set => _evidence = value; }
        public FallbackLine Press { get => _press; internal set => _press = value; }
    }

    /// <summary>Secreto que el personaje oculta hasta que se cumple una condición.</summary>
    [Serializable]
    public class SecretEntry
    {
        [SerializeField] private string _factId;
        [SerializeField, TextArea(2, 5)] private string _revealText;
        [SerializeField] private Condition _unlockCondition = new();
        [SerializeField] private bool _requiresCalmApproach;
        [SerializeField] private bool _onlyInEnding;
        [SerializeField] private List<FlagData> _setsFlags = new();
        [SerializeField] private EvidenceData _revealedByEvidence;
        [SerializeField] private List<string> _alsoRevealsFactIds = new();
        [SerializeField] private DecisionData _triggersDecision;

        public string FactId { get => _factId; internal set => _factId = value; }
        /// <summary>Lo que dice el personaje al revelarlo.</summary>
        public string RevealText { get => _revealText; internal set => _revealText = value; }
        public Condition UnlockCondition { get => _unlockCondition; internal set => _unlockCondition = value; }
        /// <summary>Solo se revela con la opción "Hablar con calma" (y si no se le presionó en esa conversación).</summary>
        public bool RequiresCalmApproach { get => _requiresCalmApproach; internal set => _requiresCalmApproach = value; }
        /// <summary>Solo se revela en un final (ej. la confesión de Andrés).</summary>
        public bool OnlyInEnding { get => _onlyInEnding; internal set => _onlyInEnding = value; }
        public IReadOnlyList<FlagData> SetsFlags { get => _setsFlags; internal set => _setsFlags = new(value); }
        /// <summary>Se revela al presentarle esta evidencia analizada (ej. el chat de Kairos a Sebastián).</summary>
        public EvidenceData RevealedByEvidence { get => _revealedByEvidence; internal set => _revealedByEvidence = value; }
        /// <summary>Otros hechos que cuenta al revelar el secreto (ej. la coartada del bar).</summary>
        public IReadOnlyList<string> AlsoRevealsFactIds { get => _alsoRevealsFactIds; internal set => _alsoRevealsFactIds = new(value); }
        public DecisionData TriggersDecision { get => _triggersDecision; internal set => _triggersDecision = value; }
    }

    /// <summary>Tema extra de conversación que aparece al cumplirse una condición (ej. "Preguntar por la llave maestra").</summary>
    [Serializable]
    public class ExtraTopic
    {
        [SerializeField] private string _id;
        [SerializeField] private string _label;
        [SerializeField] private Condition _condition = new();
        [SerializeField] private FallbackLine _line = new();

        public string Id { get => _id; internal set => _id = value; }
        /// <summary>Texto del botón.</summary>
        public string Label { get => _label; internal set => _label = value; }
        public Condition Condition { get => _condition; internal set => _condition = value; }
        public FallbackLine Line { get => _line; internal set => _line = value; }
    }

    /// <summary>Variante de actitud según el estado (ej. Elena según el final del caso 1).</summary>
    [Serializable]
    public class AttitudeVariant
    {
        [SerializeField] private Condition _condition = new();
        [SerializeField, TextArea] private string _text;
        [SerializeField] private int _alexTrustDelta;

        public Condition Condition { get => _condition; internal set => _condition = value; }
        public string Text { get => _text; internal set => _text = value; }
        /// <summary>Cambio de confianza en ALEX la primera vez que se muestra (ej. −5 por el cuaderno).</summary>
        public int AlexTrustDelta { get => _alexTrustDelta; internal set => _alexTrustDelta = value; }
    }

    /// <summary>Perfil de un personaje: lo que sabe, lo que miente y cómo habla. La IA solo le da voz.</summary>
    [CreateAssetMenu(menuName = "Caso404/Personaje", fileName = "chr_")]
    public class CharacterProfile : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private int _age;
        [SerializeField] private string _role;
        [SerializeField] private string _caseId;
        [SerializeField, TextArea(3, 6)] private string _personality;
        [SerializeField] private Sprite _portrait;
        [SerializeField] private Color _placeholderColor = Color.gray;
        [SerializeField] private List<FactEntry> _knownFacts = new();
        [SerializeField] private List<LieData> _lies = new();
        [SerializeField] private List<SecretEntry> _secrets = new();
        [SerializeField] private FallbackLines _fallback = new();
        [SerializeField] private List<AttitudeVariant> _attitudeVariants = new();
        [SerializeField] private List<ExtraTopic> _extraTopics = new();

        public string Id { get => _id; internal set => _id = value; }
        public string DisplayName { get => _displayName; internal set => _displayName = value; }
        public int Age { get => _age; internal set => _age = value; }
        public string Role { get => _role; internal set => _role = value; }
        public string CaseId { get => _caseId; internal set => _caseId = value; }
        public string Personality { get => _personality; internal set => _personality = value; }
        public Sprite Portrait { get => _portrait; internal set => _portrait = value; }
        /// <summary>Color del modelo placeholder en el greybox.</summary>
        public Color PlaceholderColor { get => _placeholderColor; internal set => _placeholderColor = value; }
        public IReadOnlyList<FactEntry> KnownFacts { get => _knownFacts; internal set => _knownFacts = new(value); }
        public IReadOnlyList<LieData> Lies { get => _lies; internal set => _lies = new(value); }
        public IReadOnlyList<SecretEntry> Secrets { get => _secrets; internal set => _secrets = new(value); }
        public FallbackLines Fallback { get => _fallback; internal set => _fallback = value; }
        public IReadOnlyList<AttitudeVariant> AttitudeVariants { get => _attitudeVariants; internal set => _attitudeVariants = new(value); }
        public IReadOnlyList<ExtraTopic> ExtraTopics { get => _extraTopics; internal set => _extraTopics = new(value); }

        /// <summary>Busca el texto de un hecho propio por id (null si no lo conoce).</summary>
        public FactEntry FindFact(string factId) => _knownFacts.Find(f => f.Id == factId);
    }
}
