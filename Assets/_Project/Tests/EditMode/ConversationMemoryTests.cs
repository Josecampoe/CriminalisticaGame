using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caso404.AI;
using Caso404.Core;
using Caso404.Dialogue;
using NUnit.Framework;
using UnityEngine;

namespace Caso404.Tests
{
    /// <summary>
    /// Memoria de conversaciones: la IA recibe lo ya hablado con cada personaje aunque se cambie de escena
    /// (servicio nuevo) o se cargue la partida. Es solo contexto: no cambia ningún dato del caso.
    /// </summary>
    public class ConversationMemoryTests
    {
        /// <summary>Cliente falso: guarda el último pedido y devuelve null (el diálogo usa el guion).</summary>
        class SpyAi : IAiClient
        {
            public AiRequest LastRequest;
            public bool IsOffline => false;
            public Task<AiReply> AskAsync(AiRequest request, CancellationToken ct)
            {
                LastRequest = request;
                return Task.FromResult<AiReply>(null);
            }
        }

        [Test]
        public void Memory_SurvivesNewSceneAndSaveLoad_AndMarksNewConversation()
        {
            var w = new Case1World();
            var laura = w.Case.FindSuspect("laura");
            var first = new DialogueService(w.State, w.Evidence, new OfflineAiClient(), w.Case);
            first.Begin(laura);
            first.AskAsync(laura, DialogueTopic.Crime, "¿Dónde estabas a las once?", CancellationToken.None).Wait();

            // Guardar y cargar (JSON) y crear el servicio de nuevo, como al entrar a otra escena.
            var loaded = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(w.State.State));
            var state = new GameStateService(loaded);
            var spy = new SpyAi();
            var second = new DialogueService(state, w.Evidence, spy, w.Case);
            second.Begin(laura);
            second.AskAsync(laura, DialogueTopic.Crime, "¿Recuerdas lo que me dijiste?", CancellationToken.None).Wait();

            var history = spy.LastRequest.History;
            Assert.IsTrue(history.Contains("Rivera: ¿Dónde estabas a las once?"), "Recuerda la conversación anterior");
            Assert.IsTrue(history.Contains(DialogueService.NewConversationMark), "Marca que es otra conversación");
            Assert.AreEqual(0, state.GetConversation("andres").Count, "Cada personaje tiene su propia memoria");
        }

        [Test]
        public void Memory_IsCapped()
        {
            var state = new GameStateService();
            for (int i = 0; i < 40; i++) state.RememberConversation("laura", $"línea {i}");
            var lines = state.GetConversation("laura");
            Assert.AreEqual(GameStateService.MaxRememberedLines, lines.Count);
            Assert.AreEqual("línea 39", lines.Last());
        }
    }
}
