using System.Collections;
using System.Linq;
using System.Threading;
using Caso404.AI;
using Caso404.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Caso404.Tests
{
    /// <summary>
    /// Prueba de integración con el proxy (Server/proxy.mjs) en modo de prueba. Se corre a mano con el proxy encendido:
    /// node Server/proxy.mjs  (sin ANTHROPIC_API_KEY)  y luego -testFilter Caso404.Tests.HttpProxyTests.
    /// </summary>
    [Explicit("Necesita el proxy encendido en localhost:8787")]
    public class HttpProxyTests
    {
        [UnityTest]
        public IEnumerator HttpClient_GetsAValidReplyFromTheProxy()
        {
            var settings = ScriptableObject.CreateInstance<AiSettings>();
            settings.UseOffline = false;
            settings.ProxyUrl = "http://localhost:8787/dialogue";
            var client = AiClientFactory.Create(settings);
            Assert.IsInstanceOf<HttpAiClient>(client);

#if UNITY_EDITOR
            var db = UnityEditor.AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/_Project/Data/GameDatabase.asset");
#else
            GameDatabase db = null;
            Assert.Ignore("Solo en el Editor");
#endif
            var laura = db.FindCase("c1").FindSuspect("laura");
            var request = new AiRequest { Character = laura, Topic = DialogueTopic.Crime, Question = "¿Qué pasó anoche?", SceneId = "c1_s1" };
            var task = client.AskAsync(request, CancellationToken.None);
            for (float t = 0; t < 20f && !task.IsCompleted; t += Time.unscaledDeltaTime) yield return null;

            Assert.IsTrue(task.IsCompleted, "El proxy no respondió");
            var reply = task.Result;
            Assert.IsNotNull(reply, "Respuesta nula: ¿está encendido el proxy?");
            StringAssert.Contains("modo de prueba", reply.reply);
            Assert.IsTrue(AiReplyValidator.IsValid(reply, laura, new Core.GameStateService()), "La respuesta del proxy debe pasar la validación del juego");
        }
    }
}
