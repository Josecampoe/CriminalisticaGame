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
        RectTransform _options;
        TextMeshProUGUI _sensValue;
        Button _invert;
        Button _fullscreen;
        Button _headBob;

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
            // Semitransparente: detrás se ve Valdora de noche (la arma el paso visual en la escena del menú).
            var bg = UIFactory.Panel(rt, "Bg", UITheme.WithAlpha(_theme.BgDark, 0.62f));
            UIFactory.Stretch(bg.rectTransform);

            _title = UIFactory.Text(rt, "Title", "EL CASO 404", _theme.TitleFont, 96, _theme.TextLight, TextAlignmentOptions.Center);
            UIFactory.Place(_title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 260), new Vector2(1400, 130));
            var sub = UIFactory.Text(rt, "Subtitle", "Valdora, 2031 — Unidad de Criminalística", _theme.MonoFont, _theme.SizeM, _theme.AlexCyan, TextAlignmentOptions.Center);
            UIFactory.Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(1400, 40));

            var buttons = UIFactory.Rect("Buttons", rt);
            UIFactory.Place(buttons, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(460, 340));
            UIFactory.VLayout(buttons.gameObject, 16);
            UIFactory.Button(buttons, "Nueva partida", _theme, _theme.Paper, _theme.Ink, StartNewGame, 64, _theme.SizeM);
            _continue = UIFactory.Button(buttons, "Continuar", _theme, _theme.Paper, _theme.Ink, ContinueGame, 64, _theme.SizeM);
            UIFactory.Button(buttons, "Opciones", _theme, _theme.Paper, _theme.Ink, () => _options.gameObject.SetActive(true), 56);
            UIFactory.Button(buttons, "Salir", _theme, _theme.Muted, _theme.TextLight, Quit, 56);
            _continue.interactable = _saves.TryLoad(out _);

            BuildOptions(rt);

            var hint = UIFactory.Text(rt, "Hint", "Proyecto universitario · Ingeniería de Software · Unity", _theme.BodyFont, _theme.SizeS, _theme.Muted, TextAlignmentOptions.Center);
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(1400, 30));
        }

        void BuildOptions(RectTransform root)
        {
            var bg = UIFactory.Panel(root, "Options", _theme.BgDark);
            _options = UIFactory.Stretch(bg.rectTransform);
            var box = UIFactory.Rect("Box", _options);
            UIFactory.Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 590));
            UIFactory.VLayout(box.gameObject, 18);
            var title = UIFactory.Text(box, "Title", "OPCIONES", _theme.TitleFont, _theme.SizeL, _theme.TextLight, TextAlignmentOptions.Center);
            UIFactory.Layout(title.gameObject, preferredHeight: 60);

            _sensValue = UIFactory.Text(box, "SensLabel", "", _theme.BodyFont, _theme.SizeBody, _theme.TextLight);
            UIFactory.Layout(_sensValue.gameObject, preferredHeight: 30);
            var sliderGo = DefaultControls.CreateSlider(new DefaultControls.Resources());
            sliderGo.transform.SetParent(box, false);
            UIFactory.Layout(sliderGo, minHeight: 30, preferredHeight: 30);
            foreach (var img in sliderGo.GetComponentsInChildren<Image>()) img.color = _theme.Paper;
            var fill = sliderGo.transform.Find("Fill Area/Fill");
            if (fill != null) fill.GetComponent<Image>().color = _theme.AlexCyan;
            var slider = sliderGo.GetComponent<Slider>();
            slider.minValue = GameSettings.MinSensitivity;
            slider.maxValue = GameSettings.MaxSensitivity;
            slider.value = GameSettings.Sensitivity;
            slider.onValueChanged.AddListener(v => { GameSettings.Sensitivity = v; RefreshOptions(); });

            _invert = UIFactory.Button(box, "", _theme, _theme.Paper, _theme.Ink, () => { GameSettings.InvertY = !GameSettings.InvertY; RefreshOptions(); }, 52);
            _headBob = UIFactory.Button(box, "", _theme, _theme.Paper, _theme.Ink, () => { GameSettings.HeadBob = !GameSettings.HeadBob; RefreshOptions(); }, 52);
            _fullscreen = UIFactory.Button(box, "", _theme, _theme.Paper, _theme.Ink, () => { GameSettings.Fullscreen = !GameSettings.Fullscreen; RefreshOptions(); }, 52);
            UIFactory.Button(box, "Volver", _theme, _theme.Muted, _theme.TextLight, () => _options.gameObject.SetActive(false), 52);
            RefreshOptions();
            _options.gameObject.SetActive(false);
        }

        void RefreshOptions()
        {
            _sensValue.text = $"Sensibilidad del mouse: {GameSettings.Sensitivity / GameSettings.DefaultSensitivity:0.0}x";
            UIFactory.SetLabel(_invert, (GameSettings.InvertY ? "[x]" : "[  ]") + "  Invertir eje Y");
            UIFactory.SetLabel(_headBob, (GameSettings.HeadBob ? "[x]" : "[  ]") + "  Balanceo de cámara al caminar");
            UIFactory.SetLabel(_fullscreen, (GameSettings.Fullscreen ? "[x]" : "[  ]") + "  Pantalla completa");
        }

        /// <summary>Empieza una partida nueva en la primera escena del caso 1.</summary>
        public void StartNewGame()
        {
            var c = _database.FindCase(_firstCaseId);
            // Antes del caso 1 se lee la carta de la Comisaria Lozano.
            if (!string.IsNullOrEmpty(c.IntroLetter)) CaseLetterView.Show(transform.parent, _theme, c.IntroLetter, BeginNewGame);
            else BeginNewGame();
        }

        void BeginNewGame()
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
