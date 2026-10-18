using System;
using System.Collections.Generic;
using UnityEngine;

namespace Caso404.Data
{
    /// <summary>De dónde sale la evidencia.</summary>
    public enum EvidenceSource
    {
        /// <summary>Objeto que se recoge en la escena.</summary>
        Scene,
        /// <summary>La entrega un personaje en un diálogo (ej. el celular de Andrés).</summary>
        Dialogue,
        /// <summary>Aparece sola al cumplirse su condición (ej. la grabación original al analizar el DVR).</summary>
        Unlock
    }

    /// <summary>Tipo de análisis adicional, distinto del análisis normal.</summary>
    public enum ExtraAnalysisKind { None, Deep, External, Password }

    /// <summary>Análisis adicional (profundo, laboratorio externo o con contraseña).</summary>
    [Serializable]
    public class ExtraAnalysis
    {
        [SerializeField] private ExtraAnalysisKind _kind;
        [SerializeField] private string _buttonLabel;
        [SerializeField] private string _password;
        [SerializeField, TextArea(2, 6)] private string _resultText;
        [SerializeField, TextArea] private string _alexComment;
        [SerializeField] private List<FlagData> _setsFlags = new();

        public ExtraAnalysisKind Kind { get => _kind; internal set => _kind = value; }
        public string ButtonLabel { get => _buttonLabel; internal set => _buttonLabel = value; }
        public string Password { get => _password; internal set => _password = value; }
        public string ResultText { get => _resultText; internal set => _resultText = value; }
        /// <summary>Lo que dice ALEX al ver el resultado (opcional).</summary>
        public string AlexComment { get => _alexComment; internal set => _alexComment = value; }
        public IReadOnlyList<FlagData> SetsFlags { get => _setsFlags; internal set => _setsFlags = new(value); }
    }

    /// <summary>Una evidencia del caso. La verdad del análisis está aquí, no en la IA.</summary>
    [CreateAssetMenu(menuName = "Caso404/Evidencia", fileName = "ev_")]
    public class EvidenceData : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _caseId;
        [SerializeField] private string _sceneId;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private GameObject _inspectPrefab;
        [SerializeField] private EvidenceSource _source;
        [SerializeField] private Condition _spawnCondition = new();
        [SerializeField] private bool _hasHiddenDetail;
        [SerializeField, TextArea] private string _hiddenDetailText;
        [SerializeField, TextArea(2, 6)] private string _analysisResult;
        [SerializeField] private bool _analyzedByAlex;
        [SerializeField] private bool _canBeAnalyzed = true;
        [SerializeField] private List<string> _revealsFactIds = new();
        [SerializeField] private List<FlagData> _setsFlagsOnAnalyze = new();
        [SerializeField] private ExtraAnalysis _extraAnalysis = new();

        public string Id { get => _id; internal set => _id = value; }
        public string CaseId { get => _caseId; internal set => _caseId = value; }
        public string SceneId { get => _sceneId; internal set => _sceneId = value; }
        public string DisplayName { get => _displayName; internal set => _displayName = value; }
        /// <summary>Lo que el jugador ve antes de analizar.</summary>
        public string Description { get => _description; internal set => _description = value; }
        public Sprite Icon { get => _icon; internal set => _icon = value; }
        public GameObject InspectPrefab { get => _inspectPrefab; internal set => _inspectPrefab = value; }
        public EvidenceSource Source { get => _source; internal set => _source = value; }
        /// <summary>Si no se cumple, la evidencia no aparece (ej. el overol si Carlos fue detenido).</summary>
        public Condition SpawnCondition { get => _spawnCondition; internal set => _spawnCondition = value; }
        /// <summary>El detalle clave solo se ve al girar el objeto en el modo inspección.</summary>
        public bool HasHiddenDetail { get => _hasHiddenDetail; internal set => _hasHiddenDetail = value; }
        public string HiddenDetailText { get => _hiddenDetailText; internal set => _hiddenDetailText = value; }
        public string AnalysisResult { get => _analysisResult; internal set => _analysisResult = value; }
        /// <summary>El análisis lo entrega ALEX (se muestra en cian; puede ser un ERROR 404).</summary>
        public bool AnalyzedByAlex { get => _analyzedByAlex; internal set => _analyzedByAlex = value; }
        /// <summary>False para objetos sin valor como prueba (ej. las cenizas).</summary>
        public bool CanBeAnalyzed { get => _canBeAnalyzed; internal set => _canBeAnalyzed = value; }
        public IReadOnlyList<string> RevealsFactIds { get => _revealsFactIds; internal set => _revealsFactIds = new(value); }
        public IReadOnlyList<FlagData> SetsFlagsOnAnalyze { get => _setsFlagsOnAnalyze; internal set => _setsFlagsOnAnalyze = new(value); }
        public ExtraAnalysis ExtraAnalysis { get => _extraAnalysis; internal set => _extraAnalysis = value; }
        public bool HasExtraAnalysis => _extraAnalysis != null && _extraAnalysis.Kind != ExtraAnalysisKind.None;
    }
}
