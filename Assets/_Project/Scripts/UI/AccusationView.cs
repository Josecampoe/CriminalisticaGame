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
            var dim = UIFactory.Panel(rt, "Dim", UITheme.WithAlpha(_theme.BgDark, 0.96f));
            UIFactory.Stretch(dim.rectTransform);
            var title = UIFactory.Text(rt, "Title", "ACUSACIÓN", _theme.TitleFont, _theme.SizeXL, _theme.TextLight);
            UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60, -30), new Vector2(1200, 64));
            var close = UIFactory.Button(rt, "Cerrar (Esc)", _theme, _theme.Muted, _theme.TextLight, Close);
            UIFactory.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60, -36), new Vector2(200, 48));

            var q1 = UIFactory.Text(rt, "Q1", "1. ¿Quién es el culpable?", _theme.TitleFont, _theme.SizeM, _theme.WarningAmber);
            UIFactory.Place(q1.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60, -120), new Vector2(900, 40));
            _suspects = UIFactory.Rect("Suspects", rt);
            UIFactory.Place(_suspects, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(1800, 64));
            UIFactory.HLayout(_suspects.gameObject, 16);

            var q2 = UIFactory.Text(rt, "Q2", "2. Elige 3 pruebas (solo evidencias analizadas)", _theme.TitleFont, _theme.SizeM, _theme.WarningAmber);
            UIFactory.Place(q2.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60, -260), new Vector2(1200, 40));
            var proofsBg = UIFactory.Panel(rt, "ProofsBg", UITheme.WithAlpha(_theme.Paper, 0.06f));
            UIFactory.Region(proofsBg.rectTransform, new Vector2(0.03f, 0.14f), new Vector2(0.97f, 0.7f));
            _proofs = UIFactory.ScrollList(proofsBg.transform, "Proofs", 8, 12);
            UIFactory.Stretch((RectTransform)_proofs.parent.parent);

            _status = UIFactory.Text(rt, "Status", "", _theme.BodyFont, _theme.SizeBody, _theme.TextLight, TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(_status.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(60, 40), new Vector2(1200, 50));
            _present = UIFactory.Button(rt, "Presentar la acusación", _theme, _theme.AccentRed, _theme.TextLight, AskConfirm);
            UIFactory.Place((RectTransform)_present.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-60, 40), new Vector2(420, 60));

            var confirmBg = UIFactory.Panel(rt, "Confirm", UITheme.WithAlpha(_theme.BgDark, 0.98f));
            _confirm = confirmBg.rectTransform;
            UIFactory.Stretch(_confirm);
            var box = UIFactory.Rect("Box", _confirm);
            UIFactory.Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 340));
            UIFactory.VLayout(box.gameObject, 18);
            _confirmText = UIFactory.Text(box, "Text", "", _theme.BodyFont, _theme.SizeM, _theme.TextLight, TextAlignmentOptions.Center);
            UIFactory.Layout(_confirmText.gameObject, preferredHeight: 180);
            UIFactory.Button(box, "Confirmar la acusación", _theme, _theme.AccentRed, _theme.TextLight, Confirm, 64, _theme.SizeM);
            UIFactory.Button(box, "Volver", _theme, _theme.Muted, _theme.TextLight, CancelConfirm, 52);
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
                UIFactory.Button(_suspects, s.DisplayName, _theme, chosen ? _theme.AccentRed : _theme.Paper, chosen ? _theme.TextLight : _theme.Ink,
                    () => { _suspectId = id; Rebuild(); }, 60, _theme.SizeM);
            }

            UIFactory.Clear(_proofs);
            var analyzed = _game.Evidence.AnalyzedEvidence.ToList();
            if (analyzed.Count == 0)
                UIFactory.Text(_proofs, "Empty", "No tienes evidencias analizadas.", _theme.BodyFont, _theme.SizeBody, _theme.TextLight);
            foreach (var ev in analyzed)
            {
                var id = ev.Id;
                bool on = _selected.Contains(id);
                var label = $"{(on ? "[x]" : "[  ]")}  {ev.DisplayName}   <size=70%>{ev.AnalysisResult}</size>";
                var b = UIFactory.Button(_proofs, label, _theme, on ? _theme.WarningAmber : _theme.Paper, _theme.Ink, () => ToggleProof(id), 64, _theme.SizeS);
                var t = b.GetComponentInChildren<TextMeshProUGUI>();
                t.alignment = TextAlignmentOptions.MidlineLeft;
            }

            int need = _game.Case.ProofsToChoose;
            var suspect = _game.Case.FindSuspect(_suspectId);
            _status.text = $"Culpable: {(suspect != null ? suspect.DisplayName : "—")}     Pruebas: {_selected.Count}/{need}";
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
            _confirmText.text = $"Acusas a <b>{suspect.DisplayName}</b> con:\n{string.Join("\n", names)}\n\n¿Seguro? La acusación no se puede deshacer.";
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
