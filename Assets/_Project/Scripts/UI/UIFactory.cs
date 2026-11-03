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

        /// <summary>Le pone esquinas redondeadas a una imagen (sprite 9-slice del tema).</summary>
        public static Image Round(Image img, UITheme theme, float radiusScale = 1f)
        {
            if (theme.RoundSprite == null) return img;
            img.sprite = theme.RoundSprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1.6f / Mathf.Max(0.1f, radiusScale); // radio ~10 px a 1920×1080
            return img;
        }

        /// <summary>Tarjeta: panel redondeado con una sombra suave debajo.</summary>
        public static Image Card(Transform parent, string name, Color color, UITheme theme, bool shadow = true)
        {
            var img = Round(Panel(parent, name, color), theme);
            if (shadow)
            {
                var s = img.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.35f);
                s.effectDistance = new Vector2(0f, -4f);
            }
            return img;
        }

        /// <summary>Marco redondeado de 2 px sobre un elemento (no participa del layout ni bloquea clics).</summary>
        public static Image Frame(Transform target, UITheme theme, Color color)
        {
            var img = Panel(target, "Marco", color);
            Stretch(img.rectTransform);
            img.raycastTarget = false;
            if (theme.FrameSprite != null)
            {
                img.sprite = theme.FrameSprite;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1.6f;
            }
            else img.color = UITheme.WithAlpha(color, 0f);
            Ignore(img.gameObject);
            return img;
        }

        /// <summary>Fondo de pantalla completa: velo oscuro y viñeta en los bordes (enfoca la vista en el centro).</summary>
        public static Image Backdrop(RectTransform parent, UITheme theme, float alpha)
        {
            var dim = Panel(parent, "Dim", UITheme.WithAlpha(theme.BgDark, alpha));
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            if (theme.VignetteSprite != null)
            {
                var v = Panel(dim.transform, "Vineta", new Color(0f, 0f, 0f, 0.85f));
                v.sprite = theme.VignetteSprite;
                Stretch(v.rectTransform);
                v.raycastTarget = false;
            }
            return dim;
        }

        /// <summary>Línea divisoria horizontal.</summary>
        public static Image Divider(Transform parent, Color color, float height = 2f)
        {
            var img = Panel(parent, "Divisor", color);
            img.raycastTarget = false;
            Layout(img.gameObject, minHeight: height, preferredHeight: height);
            return img;
        }

        /// <summary>Texto pequeño en mayúsculas con espaciado (rótulo de sección: "OBJETIVO", "FINAL"...).</summary>
        public static TextMeshProUGUI Kicker(Transform parent, string text, UITheme theme, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var t = Text(parent, "Rotulo", text.ToUpperInvariant(), theme.MonoFont, theme.SizeS, color, align);
            t.characterSpacing = 8f;
            t.fontStyle = FontStyles.Bold;
            Layout(t.gameObject, preferredHeight: 22);
            return t;
        }

        /// <summary>Etiqueta tipo píldora (estado, emoción, "recomendado por ALEX"). Se ajusta al ancho del texto.</summary>
        public static TextMeshProUGUI Chip(Transform parent, string text, UITheme theme, Color color, float fontSize = -1f)
        {
            var bg = Round(Panel(parent, "Chip", UITheme.WithAlpha(color, 0.16f)), theme, 1.4f);
            bg.raycastTarget = false;
            var h = HLayout(bg.gameObject, 0, 0, false);
            h.padding = new RectOffset(14, 14, 4, 5);
            h.childAlignment = TextAnchor.MiddleCenter;
            var fit = bg.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var t = Text(bg.transform, "Texto", text, theme.MonoFont, fontSize > 0 ? fontSize : theme.SizeS, Color.Lerp(color, Color.white, 0.15f), TextAlignmentOptions.Center);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.fontStyle = FontStyles.Bold;
            Frame(bg.transform, theme, UITheme.WithAlpha(color, 0.55f));
            return t;
        }

        /// <summary>True si el jugador está escribiendo en un campo de texto (los atajos de teclado no deben actuar).</summary>
        public static bool IsTyping()
        {
            var selected = UnityEngine.EventSystems.EventSystem.current != null ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject : null;
            return selected != null && selected.TryGetComponent<TMP_InputField>(out var field) && field.isFocused;
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
            var stamp = Text(parent, "Sello", text, theme.TitleFont, size, UITheme.WithAlpha(theme.AccentRed, 0.6f), TextAlignmentOptions.Center);
            stamp.characterSpacing = 6f;
            stamp.raycastTarget = false;
            stamp.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            var frame = Frame(stamp.transform, theme, UITheme.WithAlpha(theme.AccentRed, 0.55f));
            frame.rectTransform.offsetMin = new Vector2(-6, -2);
            frame.rectTransform.offsetMax = new Vector2(6, 2);
            Ignore(stamp.gameObject);
            return stamp;
        }

        /// <summary>Texto TextMeshPro con el tema.</summary>
        public static TextMeshProUGUI Text(Transform parent, string name, string text, TMP_FontAsset font, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            // Registra la fuente para que las etiquetas <font="..."> la encuentren aunque no esté en Resources.
            if (font != null) MaterialReferenceManager.AddFontAsset(font);
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.text = text;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>
        /// Botón redondeado con fondo de color y etiqueta en seminegrita. Si el fondo es casi transparente
        /// se dibuja un borde del color del texto (botón "fantasma"). Crece un poco al pasar el mouse.
        /// </summary>
        public static Button Button(Transform parent, string label, UITheme theme, Color background, Color foreground, Action onClick,
            float height = 48f, float fontSize = -1f)
        {
            var img = Round(Panel(parent, $"Btn_{label}", background), theme);
            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            // Reposo un poco apagado; al pasar el mouse toma su color pleno (el tinte no puede pasar de 1).
            colors.normalColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            var txt = Text(img.transform, "Label", label, theme.BodyFont, fontSize > 0 ? fontSize : theme.SizeBody, foreground, TextAlignmentOptions.Center);
            txt.fontStyle = FontStyles.Bold;
            Stretch(txt.rectTransform, 16, 4, 16, 4);
            if (background.a < 0.5f) Frame(img.transform, theme, UITheme.WithAlpha(foreground, 0.45f));
            img.gameObject.AddComponent<ButtonFx>();
            Layout(img.gameObject, minHeight: height, preferredHeight: height);
            return btn;
        }

        /// <summary>Botón secundario: fondo translúcido del color de acento, borde y texto claro.</summary>
        public static Button Ghost(Transform parent, string label, UITheme theme, Color accent, Action onClick, float height = 48f, float fontSize = -1f)
        {
            var b = Button(parent, label, theme, UITheme.WithAlpha(accent, 0.12f), Color.Lerp(accent, Color.white, 0.35f), onClick, height, fontSize);
            return b;
        }

        /// <summary>
        /// Fila con casilla (✓) para elegir opciones: hipótesis, pruebas de la acusación, opciones del menú.
        /// onPaper: estilo para fondo de papel (tinta oscura) o para fondo oscuro (texto claro).
        /// </summary>
        public static Button CheckRow(Transform parent, string label, UITheme theme, bool on, bool enabled, Action onClick, bool onPaper,
            float height = 52f, float fontSize = -1f)
        {
            Color text = onPaper ? theme.Ink : theme.TextLight;
            Color bg = on ? UITheme.WithAlpha(theme.WarningAmber, onPaper ? 0.55f : 0.22f)
                     : onPaper ? UITheme.WithAlpha(theme.Ink, 0.06f) : UITheme.WithAlpha(theme.TextLight, 0.05f);
            var b = Button(parent, label, theme, bg, enabled ? text : UITheme.WithAlpha(text, 0.45f), onClick, height, fontSize);
            b.interactable = enabled;
            var label_ = b.GetComponentInChildren<TextMeshProUGUI>();
            label_.alignment = TextAlignmentOptions.MidlineLeft;
            label_.fontStyle = FontStyles.Normal;
            label_.rectTransform.offsetMin = new Vector2(60, 4);
            label_.richText = true;
            var marco = b.transform.Find("Marco");
            if (marco != null) UnityEngine.Object.Destroy(marco.gameObject);
            if (on) Frame(b.transform, theme, UITheme.WithAlpha(theme.WarningAmber, 0.9f));

            var box = Round(Panel(b.transform, "Casilla", on ? theme.WarningAmber : UITheme.WithAlpha(text, 0.08f)), theme, 0.6f);
            Place(box.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18, 0), new Vector2(28, 28));
            box.raycastTarget = false;
            Ignore(box.gameObject);
            if (!on) Frame(box.transform, theme, UITheme.WithAlpha(text, enabled ? 0.55f : 0.25f));
            if (on && theme.CheckSprite != null)
            {
                var check = Panel(box.transform, "Check", theme.Ink);
                check.sprite = theme.CheckSprite;
                Stretch(check.rectTransform, 4, 4, 4, 4);
                check.raycastTarget = false;
            }
            return b;
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

        /// <summary>Saca un elemento del layout del padre (decoraciones superpuestas).</summary>
        public static void Ignore(GameObject go)
        {
            if (!go.TryGetComponent<LayoutElement>(out var le)) le = go.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
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

        /// <summary>
        /// Campo de texto oscuro con borde del color de acento (cian para ALEX, ámbar para los interrogatorios),
        /// cursor del mismo color y marcador en cursiva.
        /// </summary>
        public static TMP_InputField Input(Transform parent, UITheme theme, string placeholder, int charLimit, Color? accent = null)
        {
            var color = accent ?? theme.WarningAmber;
            var go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            go.name = "Campo";
            go.transform.SetParent(parent, false);
            var field = go.GetComponent<TMP_InputField>();
            field.characterLimit = charLimit;
            var bg = go.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = new Color(0.03f, 0.035f, 0.045f, 0.92f);
                Round(bg, theme);
                var colors = field.colors;
                colors.normalColor = new Color(0.8f, 0.8f, 0.8f, 1f);
                colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
                colors.selectedColor = Color.white;
                colors.colorMultiplier = 1f;
                field.colors = colors;
            }
            Frame(go.transform, theme, UITheme.WithAlpha(color, 0.6f));
            var area = go.transform.Find("Text Area") as RectTransform;
            if (area != null) { area.offsetMin = new Vector2(18, 4); area.offsetMax = new Vector2(-18, -4); }
            if (field.textComponent is TextMeshProUGUI t)
            {
                t.font = theme.BodyFont; t.fontSize = theme.SizeBody; t.color = theme.TextLight;
                t.alignment = TextAlignmentOptions.MidlineLeft;
            }
            if (field.placeholder is TextMeshProUGUI p)
            {
                p.font = theme.BodyFont; p.fontSize = theme.SizeBody; p.text = placeholder;
                p.color = UITheme.WithAlpha(theme.TextLight, 0.4f); p.fontStyle = FontStyles.Italic;
                p.alignment = TextAlignmentOptions.MidlineLeft;
            }
            field.pointSize = theme.SizeBody;
            field.customCaretColor = true;
            field.caretColor = color;
            field.caretWidth = 2;
            field.selectionColor = UITheme.WithAlpha(color, 0.35f);

            // Contador "12/200" a la derecha: se vuelve ámbar cerca del límite.
            var counter = Text(go.transform, "Contador", "", theme.MonoFont, theme.SizeS - 1, UITheme.WithAlpha(theme.TextLight, 0.35f), TextAlignmentOptions.MidlineRight);
            var crt = counter.rectTransform;
            crt.anchorMin = new Vector2(1, 0); crt.anchorMax = new Vector2(1, 1); crt.pivot = new Vector2(1, 0.5f);
            crt.sizeDelta = new Vector2(80, 0); crt.anchoredPosition = new Vector2(-14, 0);
            Ignore(counter.gameObject);
            if (area != null) area.offsetMax = new Vector2(-96, -4);
            void UpdateCounter(string value)
            {
                int n = value?.Length ?? 0;
                counter.text = n == 0 ? "" : $"{n}/{charLimit}";
                counter.color = n > charLimit * 0.9f ? theme.WarningAmber : UITheme.WithAlpha(theme.TextLight, 0.35f);
            }
            field.onValueChanged.AddListener(UpdateCounter);
            // El borde se ilumina mientras se escribe.
            var frame = go.transform.Find("Marco")?.GetComponent<Image>();
            if (frame != null)
            {
                field.onSelect.AddListener(_ => frame.color = UITheme.WithAlpha(color, 1f));
                field.onDeselect.AddListener(_ => frame.color = UITheme.WithAlpha(color, 0.6f));
            }
            Layout(go, minHeight: 48, preferredHeight: 48, flexibleWidth: 1);
            return field;
        }

        /// <summary>Borra los hijos de un contenedor (para reconstruir listas).</summary>
        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }

        /// <summary>Cuadrado redondeado sin borde (puntos de estado, retícula).</summary>
        public static Image Dot(Transform parent, string name, Color color, UITheme theme, float size)
        {
            var img = Round(Panel(parent, name, color), theme, size / 20f);
            img.raycastTarget = false;
            Layout(img.gameObject, minHeight: size, preferredHeight: size, preferredWidth: size);
            return img;
        }
    }
}
