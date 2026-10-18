using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caso404.AI;
using Caso404.Core;
using Caso404.Data;
using Caso404.Evidence;

namespace Caso404.Dialogue
{
    /// <summary>Una línea de diálogo ya resuelta.</summary>
    public class DialogueLine
    {
        public string Speaker;
        public string Text;
        public string Emotion = "neutral";
        /// <summary>True si la línea descubrió una mentira (la UI lo marca en rojo).</summary>
        public bool ExposedLie;
    }

    /// <summary>Algo que el jugador puede presentar a un personaje: una evidencia analizada o un testimonio conocido.</summary>
    public class PresentableItem
    {
        public EvidenceData Evidence;
        public string FactId;
        public string Label;
    }

    /// <summary>
    /// Conversaciones con los personajes. La IA (o el modo sin conexión) solo pone la voz;
    /// descubrir mentiras, revelar secretos y entregar evidencias lo deciden reglas deterministas con los datos.
    /// </summary>
    public class DialogueService
    {
        /// <summary>Un personaje reveló un secreto (ej. Laura entrega el cuaderno). La UI abre la decisión asociada.</summary>
        public event Action<CharacterProfile, SecretEntry> SecretRevealed;
        public event Action<CharacterProfile, LieData> LieExposed;
        public event Action<CharacterProfile> ConversationEnded;

        readonly GameStateService _state;
        readonly EvidenceService _evidence;
        readonly IAiClient _ai;
        readonly CaseData _case;
        readonly List<string> _history = new();
        bool _pressedThisConversation;

        public CharacterProfile Current { get; private set; }

        public DialogueService(GameStateService state, EvidenceService evidence, IAiClient ai, CaseData caseData)
        {
            _state = state;
            _evidence = evidence;
            _ai = ai ?? new OfflineAiClient();
            _case = caseData;
        }

        /// <summary>Empieza una conversación nueva.</summary>
        public void Begin(CharacterProfile character)
        {
            Current = character;
            _pressedThisConversation = false;
            _history.Clear();
        }

        /// <summary>Termina la conversación actual.</summary>
        public void End()
        {
            var c = Current;
            Current = null;
            if (c != null) ConversationEnded?.Invoke(c);
        }

        /// <summary>
        /// "Hablarle con calma": aparece cuando el personaje tiene un secreto de ese tipo desbloqueado,
        /// no lo ha revelado y no se le presionó en esta conversación.
        /// </summary>
        public bool CanUseCalmApproach(CharacterProfile c) => !_pressedThisConversation && FindCalmSecret(c) != null;

        /// <summary>Pregunta por un tema (o con texto libre). Nunca falla: si la IA no responde, usa el respaldo.</summary>
        public async Task<DialogueLine> AskAsync(CharacterProfile c, DialogueTopic topic, string question, CancellationToken ct)
        {
            if (topic == DialogueTopic.Calm) return Calm(c);
            if (topic == DialogueTopic.Press) return Press(c);

            AiReply reply = null;
            try { reply = await _ai.AskAsync(BuildRequest(c, topic, question), ct); }
            catch (OperationCanceledException) { throw; }
            catch (Exception e) { GameLog.Warn($"IA sin respuesta, se usa el respaldo: {e.Message}"); }

            if (!AiReplyValidator.IsValid(reply, c, _state)) reply = OfflineAiClient.BuildReply(c, topic, question);
            _state.LearnFacts(reply.usedFactIds.Where(f => c.Secrets.All(s => s.FactId != f)));
            return Remember(new DialogueLine { Speaker = c.DisplayName, Text = reply.reply, Emotion = reply.emotion }, question);
        }

        /// <summary>Evidencias analizadas y testimonios que se le pueden presentar a un personaje.</summary>
        public IEnumerable<PresentableItem> GetPresentableItems()
        {
            foreach (var ev in _evidence.AnalyzedEvidence)
                yield return new PresentableItem { Evidence = ev, Label = ev.DisplayName };
            foreach (var suspect in _case.Suspects)
                foreach (var fact in suspect.KnownFacts)
                    if (_state.KnowsFact(fact.Id) && suspect.Secrets.All(s => s.FactId != fact.Id))
                        yield return new PresentableItem { FactId = fact.Id, Label = $"Testimonio ({suspect.DisplayName}): {fact.Text}" };
        }

        /// <summary>Presenta una evidencia o un testimonio. Si refuta una mentira del personaje, la descubre.</summary>
        public DialogueLine Present(CharacterProfile c, PresentableItem item)
        {
            var lie = c.Lies.FirstOrDefault(l => l != null && !_state.IsLieExposed(l.Id) &&
                ((item.Evidence != null && l.IsRefutedBy(item.Evidence)) || (item.FactId != null && l.IsRefutedByFact(item.FactId))));
            if (lie == null)
                return Remember(new DialogueLine { Speaker = c.DisplayName, Text = c.Fallback.Evidence.Text, Emotion = "defensivo" }, item.Label);

            _state.ExposeLie(lie.Id);
            _state.LearnFacts(lie.RevealsFactIdsOnExposed);
            _state.AddFlags(lie.SetsFlagsOnExposed);
            if (item.Evidence != null)
                foreach (var granted in lie.GrantsEvidenceOnExposed) _evidence.Grant(granted);
            LieExposed?.Invoke(c, lie);
            var text = $"«{lie.Statement}»... {lie.ExposedResponse}";
            return Remember(new DialogueLine { Speaker = c.DisplayName, Text = text, Emotion = "nervioso", ExposedLie = true }, item.Label);
        }

        DialogueLine Press(CharacterProfile c)
        {
            _pressedThisConversation = true;
            bool anyExposed = c.Lies.Any(l => l != null && _state.IsLieExposed(l.Id));
            var text = anyExposed ? c.Fallback.Press.Text : c.Fallback.Crime.Text;
            return Remember(new DialogueLine { Speaker = c.DisplayName, Text = text, Emotion = anyExposed ? "nervioso" : "enojado" }, "Presionar");
        }

        DialogueLine Calm(CharacterProfile c)
        {
            var secret = _pressedThisConversation ? null : FindCalmSecret(c);
            if (secret == null)
                return Remember(new DialogueLine { Speaker = c.DisplayName, Text = c.Fallback.Crime.Text, Emotion = "triste" }, "Hablar con calma");
            _state.LearnFact(secret.FactId);
            _state.AddFlags(secret.SetsFlags);
            SecretRevealed?.Invoke(c, secret);
            return Remember(new DialogueLine { Speaker = c.DisplayName, Text = secret.RevealText, Emotion = "triste" }, "Hablar con calma");
        }

        SecretEntry FindCalmSecret(CharacterProfile c) => c?.Secrets.FirstOrDefault(s =>
            s.RequiresCalmApproach && !s.OnlyInEnding && !_state.KnowsFact(s.FactId) && s.UnlockCondition.Evaluate(_state));

        AiRequest BuildRequest(CharacterProfile c, DialogueTopic topic, string question) => new()
        {
            Character = c,
            Topic = topic,
            Question = question,
            SceneId = _state.State.currentSceneId,
            AlexTrust = _state.AlexTrust,
            AnalyzedEvidence = _evidence.AnalyzedEvidence.Select(e => $"{e.Id}: {e.AnalysisResult}").ToList(),
            KnownFacts = _state.State.knownFactIds.ToList(),
            History = _history.TakeLast(8).ToList()
        };

        DialogueLine Remember(DialogueLine line, string playerSaid)
        {
            if (!string.IsNullOrEmpty(playerSaid)) _history.Add($"Rivera: {playerSaid}");
            _history.Add($"{line.Speaker}: {line.Text}");
            return line;
        }
    }
}
