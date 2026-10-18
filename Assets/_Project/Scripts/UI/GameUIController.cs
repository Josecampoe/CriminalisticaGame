using System;
using System.Linq;
using Caso404.Core;
using Caso404.Data;
using Caso404.Decisions;
using Caso404.Player;
using UnityEngine;

namespace Caso404.UI
{
    /// <summary>
    /// Conecta las pantallas entre sí: atajos (Tab, Q, H), intro, decisión tras un secreto, resultado de una decisión,
    /// fin de escena, paso a la siguiente escena con fundido, acusación y final. No contiene reglas de juego: llama a servicios.
    /// </summary>
    public class GameUIController : MonoBehaviour
    {
        public const string MainMenuScene = "MainMenu";

        [SerializeField] private GameContext _game;
        [SerializeField] private InputReader _input;
        [SerializeField] private PlayerContext _player;
        [SerializeField] private UIManager _ui;
        [SerializeField] private UITheme _theme;
        [SerializeField] private HUDView _hud;
        [SerializeField] private JournalView _journal;
        [SerializeField] private AlexPanelView _alex;
        [SerializeField] private HypothesisView _hypothesis;
        [SerializeField] private AccusationView _accusation;
        [SerializeField] private DecisionView _decision;
        [SerializeField] private EndingView _ending;
        [SerializeField] private DialogueView _dialogue;
        [SerializeField] private MessageView _message;
        [SerializeField] private ScreenFader _fader;
        [SerializeField] private bool _showIntro = true;

        DecisionData _pendingDecision;

        /// <summary>Id de la escena que se está cargando (para pruebas).</summary>
        public string LoadingSceneId { get; private set; }

        void OnEnable()
        {
            if (_input != null)
            {
                _input.JournalPressed += OnJournal;
                _input.AlexPressed += OnAlex;
                _input.HypothesisPressed += OnHypothesis;
            }
            if (_player != null)
            {
                _player.SceneCloseRequested += OpenHypothesis;
                _player.AccusationRequested += OpenAccusation;
                _player.AlexRequested += OnAlex;
            }
            if (_hypothesis != null)
            {
                _hypothesis.DecisionRequested += ShowDecision;
                _hypothesis.SceneFinishRequested += FinishScene;
                _hypothesis.AccusationRequested += OpenAccusation;
            }
            if (_accusation != null) _accusation.Accused += ShowEnding;
            if (_decision != null) _decision.Resolved += OnDecisionResolved;
            if (_dialogue != null) _dialogue.Closed += OnDialogueClosed;
            if (_game != null && _game.Dialogue != null) _game.Dialogue.DecisionTriggered += OnDecisionTriggered;
        }

        void OnDisable()
        {
            if (_input != null)
            {
                _input.JournalPressed -= OnJournal;
                _input.AlexPressed -= OnAlex;
                _input.HypothesisPressed -= OnHypothesis;
            }
            if (_player != null)
            {
                _player.SceneCloseRequested -= OpenHypothesis;
                _player.AccusationRequested -= OpenAccusation;
                _player.AlexRequested -= OnAlex;
            }
            if (_hypothesis != null)
            {
                _hypothesis.DecisionRequested -= ShowDecision;
                _hypothesis.SceneFinishRequested -= FinishScene;
                _hypothesis.AccusationRequested -= OpenAccusation;
            }
            if (_accusation != null) _accusation.Accused -= ShowEnding;
            if (_decision != null) _decision.Resolved -= OnDecisionResolved;
            if (_dialogue != null) _dialogue.Closed -= OnDialogueClosed;
            if (_game != null && _game.Dialogue != null) _game.Dialogue.DecisionTriggered -= OnDecisionTriggered;
        }

        void Start()
        {
            var scene = _game.Scene;
            var record = _game.State.State.accusations.FirstOrDefault(a => a.caseId == _game.Case.Id);
            if (scene.HasAccusation && record != null)
            {
                var ending = _game.Case.Endings.FirstOrDefault(e => e.Id == record.endingId);
                _message.Show("Caso cerrado", $"Este caso ya terminó con el final «{(ending != null ? ending.Title : record.endingId)}».", null, false,
                    ("Menú principal", GoToMenu));
                return;
            }
            bool firstVisit = !_game.State.IsSceneCompleted(scene.Id) && !scene.Evidence.Any(e => _game.State.IsEvidenceFound(e.Id));
            if (_showIntro && firstVisit && !string.IsNullOrEmpty(scene.IntroText))
            {
                var body = (_game.Case.Scenes[0] == scene ? _game.Database.IntroText + "\n\n" : "") + scene.IntroText;
                _message.Show($"{CaseLabel()} — {_game.Case.Title}", body, _game.Case.AlexIntroLine, false,
                    ("Empezar", () => _hud.ShowSceneTitle(scene.DisplayName)));
            }
            else
            {
                _hud.ShowSceneTitle(scene.DisplayName);
            }
        }

        string CaseLabel() => _game.Case.Id == "c1" ? "Caso 1" : _game.Case.Id == "c2" ? "Caso 2" : "Caso";

        // ---------- Atajos: solo funcionan si no hay otro panel encima (por ejemplo, mientras se escribe) ----------
        void OnJournal() => Toggle(_journal);
        void OnAlex() => Toggle(_alex);
        void OnHypothesis() => Toggle(_hypothesis);

        void Toggle(UIPanel panel)
        {
            if (panel == null) return;
            if (_ui.Top == panel) { panel.Close(); return; }
            if (_ui.Top != null) return;
            panel.Open();
        }

        void OpenHypothesis()
        {
            if (_ui.Top == null) _hypothesis.Open();
        }

        void OpenAccusation()
        {
            if (!_game.Scene.HasAccusation || _ui.Top != null) return;
            if (_game.State.State.accusations.Any(a => a.caseId == _game.Case.Id)) return;
            _accusation.Open();
        }

        // ---------- Decisiones ----------
        void ShowDecision(DecisionData decision) => _decision.Show(decision);

        void OnDecisionTriggered(DecisionData decision) => _pendingDecision = decision;

        void OnDialogueClosed()
        {
            if (_pendingDecision == null) return;
            var d = _pendingDecision;
            _pendingDecision = null;
            _decision.Show(d);
        }

        void OnDecisionResolved(DecisionOutcome outcome)
        {
            var option = outcome.Option;
            bool changesScene = !string.IsNullOrEmpty(outcome.NextSceneId) || outcome.LetPlayerChooseNextScene;
            if (!string.IsNullOrEmpty(option.ResultText))
            {
                string alexLine = option.ResultSpokenByAlex ? option.ResultText : null;
                string body = option.ResultSpokenByAlex ? "" : option.ResultText;
                _message.Show(option.Text, body, alexLine, false, ("Continuar", () => AfterDecision(outcome, changesScene)));
            }
            else
            {
                AfterDecision(outcome, changesScene);
            }
        }

        void AfterDecision(DecisionOutcome outcome, bool changesScene)
        {
            if (!changesScene) return;
            if (outcome.LetPlayerChooseNextScene) FinishScene();
            else
            {
                _game.SceneFlow.CompleteCurrentScene();
                GoTo(outcome.NextSceneId);
            }
        }

        /// <summary>Termina la escena actual y deja elegir la siguiente entre las disponibles.</summary>
        public void FinishScene()
        {
            _game.SceneFlow.CompleteCurrentScene();
            var next = _game.SceneFlow.AvailableNextScenes().ToList();
            if (next.Count == 0)
            {
                _message.Show("Sin escenas pendientes", "No quedan lugares por visitar en este caso.", null, false, ("Menú principal", GoToMenu));
                return;
            }
            if (next.Count == 1)
            {
                var only = next[0];
                _message.Show("Siguiente paso", $"Vamos a: {only.DisplayName}.", null, false, (only.DisplayName, (Action)(() => GoTo(only.Id))));
                return;
            }
            var choices = next.Select(s => (s.DisplayName, (Action)(() => GoTo(s.Id)))).ToArray();
            _message.Show("¿A dónde vamos?", "Elige la siguiente escena de la investigación.", null, false, choices);
        }

        /// <summary>Guarda y pasa a otra escena del caso con fundido a negro.</summary>
        public void GoTo(string sceneId)
        {
            var next = _game.Case.FindScene(sceneId);
            if (next == null || !SceneLoader.CanLoad(next.UnitySceneName))
            {
                _game.SaveGame();
                string name = next != null ? next.DisplayName : sceneId;
                _message.Show("Escena no disponible", $"La escena «{name}» todavía no está construida.\n\nLa partida quedó guardada.", null, false,
                    ("Menú principal", GoToMenu));
                return;
            }
            _game.State.SetCurrentScene(_game.Case.Id, next.Id); // para que "Continuar" abra la escena nueva
            _game.SaveGame();
            LoadingSceneId = next.Id;
            if (_fader != null) _fader.FadeOutThen(() => SceneLoader.TryLoad(next.UnitySceneName));
            else SceneLoader.TryLoad(next.UnitySceneName);
        }

        // ---------- Acusación y final ----------
        void ShowEnding(EndingResult result)
        {
            _ending.Show(result, () =>
                _message.Show($"Fin del {CaseLabel().ToLowerInvariant()}",
                    "El caso 2, «Protocolo 404», todavía no está disponible en esta versión.\n\nTu partida quedó guardada.", null, false,
                    ("Menú principal", GoToMenu)));
        }

        /// <summary>Vuelve al menú principal.</summary>
        public void GoToMenu()
        {
            _game.SaveGame();
            if (_fader != null) _fader.FadeOutThen(() => SceneLoader.TryLoad(MainMenuScene));
            else SceneLoader.TryLoad(MainMenuScene);
        }
    }
}
