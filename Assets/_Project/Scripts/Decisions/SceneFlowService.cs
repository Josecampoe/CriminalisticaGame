using System.Collections.Generic;
using System.Linq;
using Caso404.Core;
using Caso404.Data;

namespace Caso404.Decisions
{
    /// <summary>Orden de las escenas de un caso: qué escenas se pueden visitar y cuáles faltan.</summary>
    public class SceneFlowService
    {
        readonly GameStateService _state;
        readonly CaseData _case;

        public SceneFlowService(GameStateService state, CaseData caseData)
        {
            _state = state;
            _case = caseData;
        }

        /// <summary>True si ya se completaron las escenas que exige (la Comisaría exige s2 y s3).</summary>
        public bool CanEnter(SceneData scene) =>
            scene != null && scene.RequiredCompletedSceneIds.All(_state.IsSceneCompleted);

        /// <summary>Escenas del caso a las que se puede ir ahora (no completadas, sin contar la actual).</summary>
        public IEnumerable<SceneData> AvailableNextScenes() =>
            _case.Scenes.Where(s => s != null && s.Id != _state.State.currentSceneId && !_state.IsSceneCompleted(s.Id) && CanEnter(s));

        /// <summary>Marca la escena actual como completada.</summary>
        public void CompleteCurrentScene() => _state.CompleteScene(_state.State.currentSceneId);
    }
}
