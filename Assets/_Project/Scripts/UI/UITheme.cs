using TMPro;
using UnityEngine;

namespace Caso404.UI
{
    /// <summary>Tokens visuales del juego (skill diseno-ui): paleta noir, fuentes y escala tipográfica.</summary>
    [CreateAssetMenu(menuName = "Caso404/UI Theme", fileName = "UITheme")]
    public class UITheme : ScriptableObject
    {
        [Header("Paleta")]
        [SerializeField] private Color _bgDark = Hex("#14161B");
        [SerializeField] private Color _paper = Hex("#E9E2D0");
        [SerializeField] private Color _ink = Hex("#1E1E1E");
        [SerializeField] private Color _textLight = Hex("#EDEDED");
        [SerializeField] private Color _accentRed = Hex("#C0392B");
        [SerializeField] private Color _alexCyan = Hex("#4FD1C5");
        [SerializeField] private Color _warningAmber = Hex("#E0A526");
        [SerializeField] private Color _muted = Hex("#6B7280");

        [Header("Fuentes (TextMeshPro)")]
        [Tooltip("Títulos y expediente (ideal: Special Elite)")]
        [SerializeField] private TMP_FontAsset _titleFont;
        [Tooltip("Textura de papel para la carta y las fichas del expediente.")]
        [SerializeField] private Sprite _paperSprite;
        [Tooltip("Fondo de pantalla con líneas de barrido para el panel de ALEX.")]
        [SerializeField] private Sprite _screenSprite;
        [Tooltip("Interfaz y diálogos (ideal: Inter o IBM Plex Sans)")]
        [SerializeField] private TMP_FontAsset _bodyFont;
        [Tooltip("ALEX y datos técnicos (ideal: IBM Plex Mono)")]
        [SerializeField] private TMP_FontAsset _monoFont;

        [Header("Escala tipográfica (px a 1920×1080)")]
        [SerializeField] private float _sizeXL = 48;
        [SerializeField] private float _sizeL = 32;
        [SerializeField] private float _sizeM = 24;
        [SerializeField] private float _sizeBody = 18;
        [SerializeField] private float _sizeS = 14;

        public Color BgDark => _bgDark;
        public Color Paper => _paper;
        public Color Ink => _ink;
        public Color TextLight => _textLight;
        public Color AccentRed => _accentRed;
        public Color AlexCyan => _alexCyan;
        public Color WarningAmber => _warningAmber;
        public Color Muted => _muted;

        public TMP_FontAsset TitleFont { get => _titleFont != null ? _titleFont : BodyFont; internal set => _titleFont = value; }
        public TMP_FontAsset BodyFont { get => _bodyFont != null ? _bodyFont : TMP_Settings.defaultFontAsset; internal set => _bodyFont = value; }
        /// <summary>Papel envejecido (puede ser null: entonces se usa el color plano).</summary>
        public Sprite PaperSprite { get => _paperSprite; internal set => _paperSprite = value; }
        /// <summary>Pantalla con líneas de barrido (puede ser null).</summary>
        public Sprite ScreenSprite { get => _screenSprite; internal set => _screenSprite = value; }
        public TMP_FontAsset MonoFont { get => _monoFont != null ? _monoFont : BodyFont; internal set => _monoFont = value; }

        public float SizeXL => _sizeXL;
        public float SizeL => _sizeL;
        public float SizeM => _sizeM;
        /// <summary>Texto de diálogo: nunca por debajo de 18.</summary>
        public float SizeBody => _sizeBody;
        public float SizeS => _sizeS;

        /// <summary>Color con transparencia.</summary>
        public static Color WithAlpha(Color c, float a) => new(c.r, c.g, c.b, a);

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
    }
}
