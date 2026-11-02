using System;
using System.Collections.Generic;
using System.Linq;
using Caso404.Core;
using Caso404.Data;
using Caso404.Decisions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Caso404.UI
{
    /// <summary>
    /// Acusación final: el jugador elige al culpable y 3 pruebas entre las evidencias analizadas (GUION §3.6).
    /// La acción principal es "Presentar la acusación", con confirmación porque es irreversible.
    /// El final lo calcula AccusationService con lógica determinista.
    /// </summary>
    public class AccusationView : UIPanel
    {
        /// <summary>Se presentó la acusación y ya hay final.</summary>
        public event Action<EndingResult> Accused;

        [SerializeField] private GameContext _game;

        RectTransform _suspects;
        RectTransform _proofs;
        TextMeshProUGUI _status;
        TextMeshProUGUI _confirmText;
        RectTransform _confirm;
        Button _present;
        string _suspectId;
        readonly List<string> _selected = new();
        bool _confirming;

        public override bool ClosesWithEscape => !_confirming;

        protected override void Build()
        {
            var rt = UIFactory.Stretch((RectTransform)transform);
            UIFactory.Backdrop(rt, _theme, 0.92f);
            TextMeshProUGUI title;
            var head = UIFactory.Rect("Encabezado", rt);
            UIFactory.Place(head, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60, -30), new Vector2(1300, 84));
            UIFactory.VLayout(head.gameObject, 2);
            UIFactory.Kicker(head, "Expediente para la Fiscalía", _theme, _theme.WarningAmber);
            title = UIFactory.Text(head, "Titulo", "Acusación", _theme.TitleFont, 40, _theme.TextLight);
            var close = UIFactory.Ghost(rt, "Cerrar   Esc", _theme, _theme.Muted, Close, 48);
            UIFactory.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60, -40), new Vector2(200, 48));

            title.color = _theme.TextLight;

            var q1 = UIFactory.Text(rt, "Q1", $"<color={UITheme.Hex(_theme.AccentRed)}>1</color>   ¿Quién es el culpable?", _theme.TitleFont, 30, _theme.TextLight);
            UIFactory.Place(q1.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60, -140), new Vector2(900, 44));
            _suspects = UIFactory.Rect("Suspects", rt);
            UIFactory.Place(_suspects, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -196), new Vector2(1800, 96));
            UIFactory.HLayout(_suspects.gameObject, 16);

            var q2 = UIFactory.Text(rt, "Q2", $"<color={UITheme.Hex(_theme.AccentRed)}>2</color>   Elige 3 pruebas <size=60%><font=\"{_theme.BodyFont.name}\"><color={UITheme.Hex(UITheme.WithAlpha(_theme.TextLight, 0.5f))}>solo evidencias analizadas</color></font></size>",
                _theme.TitleFont, 30, _theme.TextLight);
            UIFactory.Place(q2.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60, -320), new Vector2(1200, 44));
            var proofsBg = UIFactory.Round(UIFactory.Panel(rt, "ProofsBg", UITheme.WithAlpha(_theme.TextLight, 0.03f)), _theme);
            UIFactory.Region(proofsBg.rectTransform, new Vector2(0.03f, 0.14f), new Vector2(0.97f, 0.665f));
            UIFactory.Frame(proofsBg.transform, _theme, UITheme.WithAlpha(_theme.TextLight, 0.08f));
            _proofs = UIFactory.ScrollList(proofsBg.transform, "Proofs", 8, 14);
            UIFactory.Stretch((RectTransform)_proofs.parent.parent);

            _status = UIFactory.Text(rt, "Status", "", _theme.BodyFont, _theme.SizeBody, _theme.TextLight, TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(_status.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(60, 40), new Vector2(1200, 50));
            _present = UIFactory.Button(rt, "Presentar la acusación", _theme, _theme.AccentRed, _theme.TextLight, AskConfirm, 60, _theme.SizeM - 2);
            UIFactory.Place((RectTransform)_present.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-60, 40), new Vector2(420, 60));

            var confirmBg = UIFactory.Backdrop(rt, _theme, 0.9f);
            confirmBg.gameObject.name = "Confirm";
            _confirm = confirmBg.rectTransform;
            var boxBg = UIFactory.Card(_confirm, "Caja", new Color(0.06f, 0.065f, 0.08f, 0.98f), _theme);
            UIFactory.Frame(boxBg.transform, _theme, UITheme.WithAlpha(_theme.AccentRed, 0.6f));
            var box = boxBg.rectTransform;
            UIFactory.Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 460));
            var bv = UIFactory.VLayout(box.gameObject, 16, 40);
            UIFactory.Kicker(box, "Confirmar acusación", _theme, _theme.AccentRed, TextAlignmentOptions.Center);
            _confirmText = UIFactory.Text(box, "Text", "", _theme.BodyFont, _theme.SizeM, _theme.TextLight, TextAlignmentOptions.Center);
            UIFactory.Layout(_confirmText.gameObject, preferredHeight: 200);
            UIFactory.Button(box, "Confirmar la acusación", _theme, _theme.AccentRed, _theme.TextLight, Confirm, 64, _theme.SizeM);
            UIFactory.Ghost(box, "Volver", _theme, _theme.Muted, CancelConfirm, 52);
            _confirm.gameObject.SetActive(false);
        }

        protected override void OnShown()
        {
            _confirming = false;
            _confirm.gameObject.SetActive(false);
            _selected.RemoveAll(id => !_game.State.IsEvidenceAnalyzed(id));
            Rebuild();
        }

        void Rebuild()
        {
            UIFactory.Clear(_suspects);
            foreach (var s in _game.Case.Suspects)
            {
                var id = s.Id;
                bool chosen = id == _suspectId;
                var mutedColor = chosen ? UITheme.WithAlpha(_theme.TextLight, 0.8f) : UITheme.WithAlpha(_theme.Ink, 0.6f);
                var label = $"<font=\"{_theme.TitleFont.name}\"><size=120%>{s.DisplayName}</size></font>\n<size=70%><color={UITheme.Hex(mutedColor)}>{(chosen ? "ACUSADO(A)" : s.Role)}</color></size>";
                var b = UIFactory.Button(_suspects, label, _theme, chosen ? _theme.AccentRed : _theme.Paper, chosen ? _theme.TextLight : _theme.Ink,
                    () => { _suspectId = id; Rebuild(); }, 92, _theme.SizeBody);
                var t = b.GetComponentInChildren<TextMeshProUGUI>();
                t.fontStyle = FontStyles.Normal;
                t.overflowMode = TextOverflowModes.Ellipsis;
            }

            UIFactory.Clear(_proofs);
            var analyzed = _game.Evidence.AnalyzedEvidence.ToList();
            if (analyzed.Count == 0)
                UIFactory.Text(_proofs, "Empty", "No tienes evidencias analizadas.", _theme.BodyFont, _theme.SizeBody, _theme.TextLight);
            foreach (var ev in analyzed)
            {
                var id = ev.Id;
                bool on = _selected.Contains(id);
                var label = $"<b>{ev.DisplayName}</b>\n<size=80%><color={UITheme.Hex(UITheme.WithAlpha(_theme.TextLight, 0.6f))}>{ev.AnalysisResult}</color></size>";
                bool full = !on && _selected.Count >= _game.Case.ProofsToChoose;
                var b = UIFactory.CheckRow(_proofs, label, _theme, on, !full, () => ToggleProof(id), false, 70, _theme.SizeBody);
                b.GetComponentInChildren<TextMeshProUGUI>().overflowMode = TextOverflowModes.Ellipsis;
            }

            int need = _game.Case.ProofsToChoose;
            var suspect = _game.Case.FindSuspect(_suspectId);
            string Ok(bool ok) => UITheme.Hex(ok ? _theme.AlexCyan : _theme.WarningAmber);
            _status.text = $"<color={UITheme.Hex(UITheme.WithAlpha(_theme.TextLight, 0.55f))}>CULPABLE</color>  <color={Ok(suspect != null)}>{(suspect != null ? suspect.DisplayName : "sin elegir")}</color>" +
                           $"        <color={UITheme.Hex(UITheme.WithAlpha(_theme.TextLight, 0.55f))}>PRUEBAS</color>  <color={Ok(_selected.Count == need)}>{_selected.Count} de {need}</color>";
            _present.interactable = suspect != null && _selected.Count == need;
        }

        void ToggleProof(string id)
        {
            if (!_selected.Remove(id) && _selected.Count < _game.Case.ProofsToChoose) _selected.Add(id);
            Rebuild();
        }

        void AskConfirm()
        {
            var suspect = _game.Case.FindSuspect(_suspectId);
            if (suspect == null) return;
            var names = _selected.Select(id => _game.Case.FindEvidence(id)?.DisplayName);
            _confirmText.text = $"Acusas a <b><color={UITheme.Hex(_theme.AccentRed)}>{suspect.DisplayName}</color></b> con:\n<size=85%>{string.Join("\n", names.Select(n => "· " + n))}</size>\n\n<size=80%><color={UITheme.Hex(UITheme.WithAlpha(_theme.TextLight, 0.6f))}>La acusación no se puede deshacer.</color></size>";
            _confirming = true;
            _confirm.gameObject.SetActive(true);
        }

        void CancelConfirm()
        {
            _confirming = false;
            _confirm.gameObject.SetActive(false);
        }

        void Confirm() => Accuse(_suspectId, _selected.ToList());

        /// <summary>Presenta la acusación (también lo usan las pruebas automáticas). Devuelve null si no es válida.</summary>
        public EndingResult Accuse(string suspectId, IReadOnlyCollection<string> proofIds)
        {
            var result = _game.Accusation.Accuse(suspectId, proofIds);
            if (result == null)
            {
                _game.Accusation.AreProofsValid(proofIds, out var reason);
                _status.text = reason;
                CancelConfirm();
                return null;
            }
            _confirming = false;
            _game.SaveGame();
            Close();
            Accused?.Invoke(result);
            return result;
        }
    }
}
