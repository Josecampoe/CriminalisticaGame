using System;
using System.Collections.Generic;
using System.Linq;
using Caso404.Core;
using Caso404.Data;

namespace Caso404.Decisions
{
    /// <summary>
    /// Tablero de hipótesis: las opciones se desbloquean con evidencias analizadas.
    /// Responder mal está permitido (y se puede corregir); acertar activa los flags de la pregunta.
    /// </summary>
    public class HypothesisService
    {
        public event Action<HypothesisQuestion, bool> QuestionAnswered;

        readonly GameStateService _state;

        public HypothesisService(GameStateService state) => _state = state;

        public bool IsUnlocked(HypothesisOption option) => option != null && option.UnlockCondition.Evaluate(_state);

        public string LockReason(HypothesisOption option) => option.UnlockCondition.DescribeMissing(_state);

        /// <summary>Responde una pregunta. Devuelve si la respuesta es correcta. Ignora opciones bloqueadas o desconocidas.</summary>
        public bool Submit(HypothesisQuestion question, IEnumerable<string> optionIds)
        {
            if (question == null) return false;
            var chosen = (optionIds ?? Enumerable.Empty<string>())
                .Distinct()
                .Select(id => question.Options.FirstOrDefault(o => o.Id == id))
                .Where(o => o != null && IsUnlocked(o))
                .ToList();
            if (chosen.Count == 0 || (!question.MultiSelect && chosen.Count > 1)) return false;

            var correctIds = question.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
            var chosenIds = chosen.Select(o => o.Id).ToHashSet();
            bool correct = question.MultiSelect ? chosenIds.SetEquals(correctIds) : chosen[0].IsCorrect;

            _state.SetHypothesisAnswer(question.Id, chosenIds, correct);
            if (correct) _state.AddFlags(question.OnCorrectSetFlags);
            QuestionAnswered?.Invoke(question, correct);
            return correct;
        }

        public bool IsAnswered(HypothesisQuestion q) => _state.GetHypothesisAnswer(q.Id) != null;

        public bool IsCorrect(HypothesisQuestion q) => _state.GetHypothesisAnswer(q.Id)?.correct ?? false;

        /// <summary>True si todas las preguntas tienen respuesta (correcta o no): así se habilita la decisión de cierre.</summary>
        public bool IsComplete(HypothesisData hyp) => hyp == null || hyp.Questions.All(IsAnswered);
    }
}
