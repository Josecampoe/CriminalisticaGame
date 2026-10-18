using System.Collections;
using Caso404.Core;
using Caso404.Data;
using Caso404.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Caso404.UI
{
    /// <summary>
    /// Menú principal: título con efecto de máquina de escribir, Nueva partida, Continuar (si hay partida guardada) y Salir.
    /// Nueva partida crea un GameState limpio y lo guarda; cada escena lo carga desde el guardado.
    /// </summary>
    public class MainMenuView : MonoBehaviour
    {
        [SerializeField] private UITheme _theme;
        [SerializeField] private GameDatabase _database;
        [SerializeField] private ScreenFader _fader;
        [SerializeField] private string _firstCaseId = "c1";

        SaveService _saves;
        TextMeshProUGUI _title;
        Button _continue;

        void Awake()
        {
            _saves = new SaveService();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Build();
        }

        void Start() => StartCoroutine(TypeTitle());

        void Build()
        {
            var rt = UIFactory.Stretch((RectTransform)transform);
            var bg = UIFactory.Panel(rt, "Bg", _theme.BgDark);
            UIFactory.Stretch(bg.rectTransform);

            _title = UIFactory.Text(rt, "Title", "EL CASO 404", _theme.TitleFont, 96, _theme.TextLight, TextAlignmentOptions.Center);
            UIFactory.Place(_title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 260), new Vector2(1400, 130));
            var sub = UIFactory.Text(rt, "Subtitle", "Valdora, 2031 — Unidad de Criminalística", _theme.MonoFont, _theme.SizeM, _theme.AlexCyan, TextAlignmentOptions.Center);
            UIFactory.Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(1400, 40));

            var buttons = UIFactory.Rect("Buttons", rt);
            UIFactory.Place(buttons, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(460, 260));
            UIFactory.VLayout(buttons.gameObject, 16);
            UIFactory.Button(buttons, "Nueva partida", _theme, _theme.Paper, _theme.Ink, StartNewGame, 64, _theme.SizeM);
            _continue = UIFactory.Button(buttons, "Continuar", _theme, _theme.Paper, _theme.Ink, ContinueGame, 64, _theme.SizeM);
            UIFactory.Button(buttons, "Salir", _theme, _theme.Muted, _theme.TextLight, Quit, 56);
            _continue.interactable = _saves.TryLoad(out _);

            var hint = UIFactory.Text(rt, "Hint", "Proyecto universitario · Ingeniería de Software · Unity", _theme.BodyFont, _theme.SizeS, _theme.Muted, TextAlignmentOptions.Center);
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(1400, 30));
        }

        /// <summary>Empieza una partida nueva en la primera escena del caso 1.</summary>
        public void StartNewGame()
        {
            var c = _database.FindCase(_firstCaseId);
            var first = c.Scenes[0];
            var state = new GameState
            {
                alexTrust = _database.Alex != null ? _database.Alex.StartTrust : GameState.DefaultAlexTrust,
                currentCaseId = c.Id,
                currentSceneId = first.Id
            };
            _saves.Save(state);
            Load(first.UnitySceneName);
        }

        /// <summary>Continúa en la escena guardada.</summary>
        public void ContinueGame()
        {
            if (!_saves.TryLoad(out var state)) { StartNewGame(); return; }
            var scene = _database.FindScene(state.currentSceneId) ?? _database.FindCase(_firstCaseId).Scenes[0];
            Load(scene.UnitySceneName);
        }

        void Load(string unityScene)
        {
            if (_fader != null) _fader.FadeOutThen(() => SceneLoader.TryLoad(unityScene));
            else SceneLoader.TryLoad(unityScene);
        }

        IEnumerator TypeTitle()
        {
            _title.ForceMeshUpdate();
            int total = _title.textInfo.characterCount;
            for (int i = 0; i <= total; i++)
            {
                _title.maxVisibleCharacters = i;
                yield return new WaitForSecondsRealtime(0.09f);
            }
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
