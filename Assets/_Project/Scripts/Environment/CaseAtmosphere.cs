using Caso404.Core;
using UnityEngine;

namespace Caso404.Environment
{
    /// <summary>
    /// Ambiente de una escena compartida entre casos (la Comisaría): de día en el caso 1 y de noche en el caso 2.
    /// Cambia cielo, niebla, luz ambiente, la luz exterior y enciende ventanas y faroles de la ciudad.
    /// </summary>
    public class CaseAtmosphere : MonoBehaviour
    {
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private GameContext _game;
        [SerializeField] private string _nightCaseId = "c2";
        [SerializeField] private Material _nightSkybox;
        [SerializeField] private Color _nightFog = new(0.06f, 0.07f, 0.11f);
        [SerializeField] private Color _nightAmbient = new(0.12f, 0.13f, 0.18f);
        [SerializeField] private Light _sun;
        [SerializeField] private Color _nightSunColor = new(0.4f, 0.45f, 0.65f);
        [SerializeField] private float _nightSunIntensity = 0.2f;
        [SerializeField] private Renderer[] _cityLights;
        [SerializeField] private Color _windowEmission = new(1.1f, 0.88f, 0.5f);
        [SerializeField] private Color _lampEmission = new(2.2f, 1.7f, 0.9f);
        [SerializeField] private Material _lampMaterial;

        void Start()
        {
            if (_game == null || _game.Case == null || _game.Case.Id != _nightCaseId) return;
            if (_nightSkybox != null) RenderSettings.skybox = _nightSkybox;
            RenderSettings.fogColor = _nightFog;
            RenderSettings.ambientLight = _nightAmbient;
            if (_sun != null) { _sun.color = _nightSunColor; _sun.intensity = _nightSunIntensity; }
            var block = new MaterialPropertyBlock();
            foreach (var r in _cityLights)
            {
                if (r == null) continue;
                bool lamp = r.sharedMaterial == _lampMaterial;
                r.GetPropertyBlock(block);
                block.SetColor(EmissionId, lamp ? _lampEmission : _windowEmission);
                if (!lamp) block.SetColor(BaseColorId, new Color(0.3f, 0.32f, 0.4f));
                r.SetPropertyBlock(block);
            }
            DynamicGI.UpdateEnvironment();
        }
    }
}
