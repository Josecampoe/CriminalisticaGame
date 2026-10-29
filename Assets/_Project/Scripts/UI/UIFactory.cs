using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Caso404.UI
{
    /// <summary>
    /// Ayudantes para construir la UI por código con los tokens del UITheme.
    /// Así cada vista arma su jerarquía sin prefabs que haya que enlazar a mano.
    /// </summary>
    public static class UIFactory
    {
        /// <summary>Crea un hijo con RectTransform.</summary>
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Estira el rect a su padre con márgenes (izq, abajo, der, arriba).</summary>
        public static RectTransform Stretch(RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>Ancla el rect en un punto del padre con un tamaño fijo.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>Rect anclado entre dos puntos relativos del padre (0..1).</summary>
        public static RectTransform Region(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>Panel de color plano.</summary>
        public static Image Panel(Transform parent, string name, Color color)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        /// <summary>Convierte un panel en una hoja de papel: textura envejecida y sombra (parece un objeto sobre la mesa).</summary>
        public static Image AsPaper(Image img, UITheme theme)
        {
            if (theme.PaperSprite != null)
            {
                img.sprite = theme.PaperSprite;
                img.type = Image.Type.Simple;
                img.color = Color.white;
            }
            var shadow = img.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(10f, -12f);
            return img;
        }

        /// <summary>Convierte un panel en una pantalla: líneas de barrido y un borde con brillo del color dado.</summary>
        public static Image AsScreen(Image img, UITheme theme, Color glow)
        {
            if (theme.ScreenSprite != null)
            {
                img.sprite = theme.ScreenSprite;
                img.type = Image.Type.Simple;
                img.color = new Color(1f, 1f, 1f, img.color.a);
            }
            var outline = img.gameObject.AddComponent<Outline>();
            outline.effectColor = UITheme.WithAlpha(glow, 0.35f);
            outline.effectDistance = new Vector2(3f, 3f);
            return img;
        }

        /// <summary>Sello de tinta rotado (ej. "EVIDENCIA"), como los de un expediente real.</summary>
        public static TextMeshProUGUI Stamp(Transform parent, string text, UITheme theme, float size, float angle)
        {
            var stamp = Text(parent, "Sello", text, theme.TitleFont, size, UITheme.WithAlpha(theme.AccentRed, 0.55f), TextAlignmentOptions.Center);
            stamp.fontStyle = FontStyles.Bold;
            stamp.characterSpacing = 6f;
            stamp.raycastTarget = false;
            stamp.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            var border = stamp.gameObject.AddComponent<Outline>();
            border.effectColor = UITheme.WithAlpha(theme.AccentRed, 0.3f);
            var ignore = stamp.gameObject.AddComponent<LayoutElement>();
            ignore.ignoreLayout = true;
            return stamp;
        }

        /// <summary>Texto TextMeshPro con el tema.</summary>
        public static TextMeshProUGUI Text(Transform parent, string name, string text, TMP_FontAsset font, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.text = text;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Botón con fondo de color y etiqueta.</summary>
        public static Button Button(Transform parent, string label, UITheme theme, Color background, Color foreground, Action onClick,
            float height = 48f, float fontSize = -1f)
        {
            var img = Panel(parent, $"Btn_{label}", background);
            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.selectedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            var txt = Text(img.transform, "Label", label, theme.BodyFont, fontSize > 0 ? fontSize : theme.SizeBody, foreground, TextAlignmentOptions.Center);
            Stretch(txt.rectTransform, 12, 4, 12, 4);
            Layout(img.gameObject, minHeight: height, preferredHeight: height);
            return btn;
        }

        /// <summary>Cambia el texto de un botón creado con <see cref="Button"/>.</summary>
        public static void SetLabel(Button button, string label)
        {
            var t = button.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) t.text = label;
        }

        public static VerticalLayoutGroup VLayout(GameObject go, float spacing = 8f, int padding = 0, bool expandHeight = false)
        {
            var l = go.AddComponent<VerticalLayoutGroup>();
            l.spacing = spacing;
            l.padding = new RectOffset(padding, padding, padding, padding);
            l.childControlWidth = true;
            l.childControlHeight = true;
            l.childForceExpandWidth = true;
            l.childForceExpandHeight = expandHeight;
            return l;
        }

        public static HorizontalLayoutGroup HLayout(GameObject go, float spacing = 8f, int padding = 0, bool expandWidth = true)
        {
            var l = go.AddComponent<HorizontalLayoutGroup>();
            l.spacing = spacing;
            l.padding = new RectOffset(padding, padding, padding, padding);
            l.childControlWidth = true;
            l.childControlHeight = true;
            l.childForceExpandWidth = expandWidth;
            l.childForceExpandHeight = false;
            return l;
        }

        public static LayoutElement Layout(GameObject go, float minHeight = -1, float preferredHeight = -1, float flexibleWidth = -1,
            float flexibleHeight = -1, float preferredWidth = -1)
        {
            if (!go.TryGetComponent<LayoutElement>(out var le)) le = go.AddComponent<LayoutElement>();
            le.minHeight = minHeight;
            le.preferredHeight = preferredHeight;
            le.flexibleWidth = flexibleWidth;
            le.flexibleHeight = flexibleHeight;
            le.preferredWidth = preferredWidth;
            return le;
        }

        /// <summary>Lista con scroll vertical (rueda del mouse). Devuelve el contenedor donde agregar filas.</summary>
        public static RectTransform ScrollList(Transform parent, string name, float spacing = 8f, int padding = 8)
        {
            var root = Rect(name, parent);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            var viewport = Stretch(Rect("Viewport", root));
            viewport.gameObject.AddComponent<RectMask2D>();
            var raycastCatcher = viewport.gameObject.AddComponent<Image>();
            raycastCatcher.color = new Color(0, 0, 0, 0);
            var content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            VLayout(content.gameObject, spacing, padding);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            return content;
        }

        /// <summary>Campo de texto TextMeshPro.</summary>
        public static TMP_InputField Input(Transform parent, UITheme theme, string placeholder, int charLimit)
        {
            var go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            go.transform.SetParent(parent, false);
            var field = go.GetComponent<TMP_InputField>();
            field.characterLimit = charLimit;
            var bg = go.GetComponent<Image>();
            if (bg != null) bg.color = theme.Paper;
            if (field.textComponent is TextMeshProUGUI t) { t.font = theme.BodyFont; t.fontSize = theme.SizeBody; t.color = theme.Ink; }
            if (field.placeholder is TextMeshProUGUI p) { p.font = theme.BodyFont; p.fontSize = theme.SizeBody; p.text = placeholder; p.color = theme.Muted; }
            field.pointSize = theme.SizeBody;
            Layout(go, minHeight: 48, preferredHeight: 48, flexibleWidth: 1);
            return field;
        }

        /// <summary>Borra los hijos de un contenedor (para reconstruir listas).</summary>
        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }

    }
}
