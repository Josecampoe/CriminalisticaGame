using Caso404.AI;
using Caso404.Core.Events;
using Caso404.Data;
using Caso404.Decisions;
using Caso404.Dialogue;
using Caso404.Evidence;
using Caso404.Save;
using UnityEngine;

namespace Caso404.Core
{
    /// <summary>
    /// Raíz de composición de la escena: crea los servicios del juego (clases C# puras) y los reparte
    /// por [SerializeField] a quien los necesite. Carga o crea el GameState y guarda en los momentos clave.
    /// Se ejecuta antes que los demás scripts para que los servicios existan en su OnEnable/Start.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameContext : MonoBehaviour
    {
        [SerializeField] private GameDatabase _database;
        [SerializeField] private SceneData _scene;
        [Tooltip("Otras versiones de esta escena para otros casos (la Comisaría es la misma escena en los dos casos).")]
        [SerializeField] private System.Collections.Generic.List<SceneData> _alternativeScenes = new();
        [Tooltip("Si está activo, empieza una partida nueva al entrar (útil para la primera escena y para probar).")]
        [SerializeField] private bool _startNewGame = true;
        [SerializeField] private AiSettings _aiSettings;
        [SerializeField] private StringEventChannel _evidenceCollectedChannel;
        [SerializeField] private StringEventChannel _decisionMadeChannel;

        public GameDatabase Database => _database;
        public SceneData Scene { get; private set; }
        public CaseData Case { get; private set; }
        public GameStateService State { get; private set; }
        public EvidenceService Evidence { get; private set; }
        public AlexService Alex { get; private set; }
        public DecisionService Decisions { get; private set; }
        public HypothesisService Hypotheses { get; private set; }
        public AccusationService Accusation { get; private set; }
        public SceneFlowService SceneFlow { get; private set; }
        public SaveService Saves { get; private set; }
        public IAiClient AiClient { get; private set; }
        public DialogueService Dialogue { get; private set; }

        void Awake()
        {
            if (_database == null || _scene == null)
            {
                GameLog.Error($"{name}: asigna GameDatabase y SceneData en el Inspector.", this);
                enabled = false;
                return;
            }
            Saves = new SaveService();

            GameState state = null;
            if (!_startNewGame && Saves.TryLoad(out var loaded)) state = loaded;
            // Escena compartida: se usa la versión del caso en curso (ej. c2_s4 en la Comisaría durante el caso 2).
            Scene = _scene;
            if (state != null && state.currentCaseId != _scene.CaseId)
                Scene = _alternativeScenes.Find(s => s != null && s.CaseId == state.currentCaseId) ?? _scene;
            Case = _database.FindCase(Scene.CaseId);
            state ??= new GameState { alexTrust = _database.Alex != null ? _database.Alex.StartTrust : GameState.DefaultAlexTrust };

            State = new GameStateService(state);
            Evidence = new EvidenceService(State, Case);
            Alex = new AlexService(State, Case, _database.Alex, Evidence);
            Decisions = new DecisionService(State);
            Hypotheses = new HypothesisService(State);
            Accusation = new AccusationService(State, Case);
            SceneFlow = new SceneFlowService(State, Case);
            AiClient = AiClientFactory.Create(_aiSettings);
            Dialogue = new DialogueService(State, Evidence, AiClient, Case);

            State.SetCurrentScene(Case.Id, Scene.Id);
            Evidence.EvidenceCollected += OnEvidenceCollected;
            Decisions.DecisionMade += OnDecisionMade;
            GameLog.Info($"Escena {Scene.Id} lista. Confianza en ALEX: {State.AlexTrust}");
        }

        void OnDestroy()
        {
            if (Evidence != null) Evidence.EvidenceCollected -= OnEvidenceCollected;
            if (Decisions != null) Decisions.DecisionMade -= OnDecisionMade;
            Alex?.Dispose();
        }

        /// <summary>Guarda la partida ahora.</summary>
        public void SaveGame() => Saves?.Save(State.State);

        void OnEvidenceCollected(EvidenceData ev)
        {
            if (_evidenceCollectedChannel != null) _evidenceCollectedChannel.Raise(ev.Id);
        }

        void OnDecisionMade(DecisionData decision, DecisionOption option)
        {
            foreach (var ev in option.GrantsEvidence) Evidence.Grant(ev); // ej. la copia espejo (si sigue disponible)
            SaveGame(); // se guarda al tomar una decisión (y al cambiar de escena)
            if (_decisionMadeChannel != null) _decisionMadeChannel.Raise($"{decision.Id}:{option.Id}");
        }
    }
}
