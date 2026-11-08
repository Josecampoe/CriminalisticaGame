using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Caso404.UI
{
    /// <summary>
    /// Respuesta visual inmediata de un botón: crece un 2 % al pasar el mouse o seleccionarlo con teclado
    /// y se hunde un poco al pulsarlo (skill diseno-ui: confirmar la acción en menos de 100 ms).
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class ButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        const float HoverScale = 1.02f;
        const float PressScale = 0.97f;
        const float Speed = 18f;

        Selectable _selectable;
        bool _hover, _selected, _pressed;

        void Awake() => _selectable = GetComponent<Selectable>();

        void OnDisable()
        {
            _hover = _selected = _pressed = false;
            transform.localScale = Vector3.one;
        }

        public void OnPointerEnter(PointerEventData e) => _hover = true;
        public void OnPointerExit(PointerEventData e) { _hover = false; _pressed = false; }
        public void OnPointerDown(PointerEventData e)
        {
            _pressed = true;
            if (_selectable.IsInteractable()) UiSound.Click();
        }
        public void OnPointerUp(PointerEventData e) => _pressed = false;
        public void OnSelect(BaseEventData e) => _selected = true;
        public void OnDeselect(BaseEventData e) => _selected = false;

        void Update()
        {
            bool active = _selectable.IsInteractable();
            float target = !active ? 1f : _pressed ? PressScale : (_hover || _selected) ? HoverScale : 1f;
            float s = transform.localScale.x;
            if (Mathf.Abs(s - target) < 0.0005f) return;
            s = Mathf.Lerp(s, target, 1f - Mathf.Exp(-Speed * Time.unscaledDeltaTime));
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
