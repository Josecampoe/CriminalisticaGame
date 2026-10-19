using System;
using Caso404.Core;
using Caso404.Data;

namespace Caso404.Decisions
{
    /// <summary>Resultado de tomar una decisión.</summary>
    public class DecisionOutcome
    {
        public bool Applied;
        public DecisionData Decision;
        public DecisionOption Option;
        public string NextSceneId;
        public bool LetPlayerChooseNextScene;
    }

    /// <summary>Aplica decisiones: flags, confianza en ALEX y escena siguiente. Cada decisión se toma una sola vez.</summary>
    public class DecisionService
    {
        public event Action<DecisionData, DecisionOption> DecisionMade;

        readonly GameStateService _state;

        public DecisionService(GameStateService state) => _state = state;

        public bool HasTaken(DecisionData decision) => decision != null && _state.GetDecisionOption(decision.Id) != null;

        /// <summary>True si la opción se puede elegir ahora.</summary>
        public bool IsAvailable(DecisionOption option) => option != null && option.Condition.Evaluate(_state);

        /// <summary>Toma la decisión. No hace nada si ya se tomó o si la opción no está disponible.</summary>
        public DecisionOutcome Choose(DecisionData decision, DecisionOption option)
        {
            var outcome = new DecisionOutcome { Decision = decision, Option = option };
            if (decision == null || option == null || HasTaken(decision) || !IsAvailable(option)) return outcome;

            _state.RecordDecision(decision.Id, option.Id);
            _state.AddFlags(option.SetsFlags);
            _state.ChangeTrust(option.AlexTrustDelta);
            outcome.Applied = true;
            outcome.NextSceneId = option.NextSceneId;
            outcome.LetPlayerChooseNextScene = option.LetPlayerChooseNextScene;
            DecisionMade?.Invoke(decision, option);
            return outcome;
        }
    }
}
