using Caso404.Core;
using Caso404.Data;
using Caso404.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Caso404.UI
{
    /// <summary>
    /// Modo inspección: fondo oscurecido, el objeto 3D al centro (imagen de la cámara de inspección),
    /// nombre arriba y botones "Guardar en expediente" y "Soltar".
    /// </summary>
    public class InspectionView : UIPanel
    {
        [SerializeField] private InspectionController _inspection;
        [SerializeField] private GameContext _game;

        RawImage _image;
        TextMeshProUGUI _title;
        TextMeshProUGUI _description;
        TextMeshProUGUI _detail;
        Button _keep;
        Button _drop;

        protected override void Build()
        {
            var rt = UIFactory.Stretch((RectTransform)transform);
            var dim = UIFactory.Panel(rt, "Dim", UITheme.WithAlpha(_theme.BgDark, 0.9f));
            UIFactory.Stretch(dim.rectTransform);

            _title = UIFactory.Text(rt, "Title", "", _theme.TitleFont, _theme.SizeL, _theme.TextLight, TextAlignmentOptions.Center);
            UIFactory.Place(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(1400, 56));

            _image = UIFactory.Rect("View", rt).gameObject.AddComponent<RawImage>();
            UIFactory.Place(_image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(620, 620));
            _image.raycastTarget = false;

            _description = UIFactory.Text(rt, "Description", "", _theme.BodyFont, _theme.SizeBody, _theme.TextLight, TextAlignmentOptions.Center);
            UIFactory.Place(_description.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 200), new Vector2(1200, 60));
            _detail = UIFactory.Text(rt, "Detail", "", _theme.BodyFont, _theme.SizeBody, _theme.WarningAmber, TextAlignmentOptions.Center);
            UIFactory.Place(_detail.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 160), new Vector2(1200, 40));

            var hint = UIFactory.Text(rt, "Hint", "Mantén clic izquierdo y mueve el mouse para girar el objeto", _theme.BodyFont, _theme.SizeS,
                _theme.Muted, TextAlignmentOptions.Center);
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 128), new Vector2(1200, 28));

            var buttons = UIFactory.Rect("Buttons", rt);
            UIFactory.Place(buttons, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 48), new Vector2(640, 56));
            UIFactory.HLayout(buttons.gameObject, 16);
            _keep = UIFactory.Button(buttons, "Guardar en expediente", _theme, _theme.WarningAmber, _theme.Ink, OnKeep);
            _drop = UIFactory.Button(buttons, "Soltar", _theme, _theme.Muted, _theme.TextLight, OnDrop);
        }

        void OnEnable()
        {
            if (_inspection == null) return;
            _inspection.Started += OnStarted;
            _inspection.Ended += OnEnded;
            _inspection.DetailRevealed += OnDetail;
        }

        void OnDisable()
        {
            if (_inspection == null) return;
            _inspection.Started -= OnStarted;
            _inspection.Ended -= OnEnded;
            _inspection.DetailRevealed -= OnDetail;
        }

        void OnStarted(EvidenceData ev)
        {
            _image.texture = _inspection.Texture;
            _title.text = ev.DisplayName;
            _description.text = ev.Description;
            _detail.text = "";
            bool found = _game.Evidence.IsFound(ev);
            _keep.gameObject.SetActive(!found);
            UIFactory.SetLabel(_drop, found ? "Volver" : "Soltar");
            Open();
        }

        void OnDetail(EvidenceData ev) => _detail.text = ev.HiddenDetailText;

        void OnEnded() => Close();

        void OnKeep() => _inspection.Keep();

        void OnDrop() => _inspection.End();

        protected override void OnHidden()
        {
            if (_inspection != null && _inspection.IsActive) _inspection.End(); // cerrado con Escape
        }
    }
}
