using UnityEngine;

namespace Caso404.Interaction
{
    /// <summary>
    /// Resalta el objeto apuntado tiñendo su color: ámbar si es nuevo, blanco si ya se revisó (skill diseno-ui).
    /// Usa MaterialPropertyBlock para no duplicar materiales. Más adelante se puede cambiar por un contorno.
    /// </summary>
    public class Highlightable : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly Color Amber = new(0.878f, 0.647f, 0.149f); // warning_amber #E0A526

        [SerializeField, Range(0f, 1f)] private float _strength = 0.55f;

        Renderer[] _renderers;
        Color[] _baseColors;
        MaterialPropertyBlock _block;
        bool _on;
        bool _isNew;

        void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                var mat = _renderers[i].sharedMaterial;
                _baseColors[i] = mat != null && mat.HasProperty(BaseColorId) ? mat.GetColor(BaseColorId) : Color.white;
            }
            _block = new MaterialPropertyBlock();
        }

        /// <summary>Enciende o apaga el resaltado.</summary>
        public void SetHighlight(bool on, bool isNew)
        {
            if (_renderers == null || (on == _on && isNew == _isNew)) return;
            _on = on;
            _isNew = isNew;
            var tint = isNew ? Amber : Color.white;
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (!on) { _renderers[i].SetPropertyBlock(null); continue; }
                _renderers[i].GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, Color.Lerp(_baseColors[i], tint, _strength));
                _renderers[i].SetPropertyBlock(_block);
            }
        }
    }
}
