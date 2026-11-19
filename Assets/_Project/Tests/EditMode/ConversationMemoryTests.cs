using System.Linq;
using Caso404.AI;
using Caso404.Core;
using Caso404.Dialogue;
using NUnit.Framework;
using UnityEngine;

namespace Caso404.Tests
{
    /// <summary>
    /// Memoria de conversaciones: lo ya hablado con cada personaje se conserva aunque se cambie de escena
    /// (servicio nuevo) o se cargue la partida. Es solo historial: no cambia ningún dato del caso.
    /// </summary>
    public class ConversationMemoryTests
    {
        [Test]
        public void Memory_SurvivesNewSceneAndSaveLoad_AndMarksNewConversation()
        {
            var w = new Case1World();
            var laura = w.Case.FindSuspect("laura");
            var first = new DialogueService(w.State, w.Evidence, w.Case);
            first.Begin(laura);
            first.Ask(laura, DialogueTopic.Crime);

            // Guardar y cargar (JSON) y crear el servicio de nuevo, como al entrar a otra escena.
            var loaded = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(w.State.State));
            var state = new GameStateService(loaded);
            var second = new DialogueService(state, w.Evidence, w.Case);
            second.Begin(laura);
            second.Ask(laura, DialogueTopic.Crime);

            var history = state.GetConversation("laura");
            Assert.IsTrue(history.Contains("Rivera: " + DialogueService.QuestionFor(DialogueTopic.Crime)), "Recuerda la conversación anterior");
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
