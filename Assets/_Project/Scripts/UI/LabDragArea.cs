using UnityEngine;
using UnityEngine.EventSystems;

namespace Caso404.UI
{
    /// <summary>Zona de la UI que pasa el arrastre del mouse a la mesa del laboratorio (gira la evidencia).</summary>
    public class LabDragArea : MonoBehaviour, IDragHandler
    {
        [SerializeField] private LabStage _stage;

        /// <summary>Mesa que recibe el arrastre.</summary>
        public LabStage Stage { get => _stage; set => _stage = value; }

        public void OnDrag(PointerEventData eventData)
        {
            if (_stage != null) _stage.Drag(eventData.delta);
        }
    }
}
