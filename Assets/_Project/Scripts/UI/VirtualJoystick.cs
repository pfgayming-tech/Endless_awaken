using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VSL
{
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform background;
        public RectTransform handle;
        public float handleRange = 60f;

        public Vector2 Direction { get; private set; }

        private Canvas _canvas;
        private Camera _uiCam;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                _uiCam = _canvas.worldCamera;
        }

        public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

        public void OnDrag(PointerEventData eventData)
        {
            if (background == null || handle == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background,
                eventData.position,
                _uiCam,
                out var localPoint
            );

            Vector2 v = localPoint;
            v = Vector2.ClampMagnitude(v, handleRange);

            handle.anchoredPosition = v;
            Direction = v / handleRange;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (handle != null) handle.anchoredPosition = Vector2.zero;
            Direction = Vector2.zero;
        }
    }
}
