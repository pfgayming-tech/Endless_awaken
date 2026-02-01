using System.Collections.Generic;
using UnityEngine;

namespace VSL.VFX
{
    // 가능한 빨리 초기화되도록(피격이 매우 빠를 때도 안전)
    [DefaultExecutionOrder(-1000)]
    public class DamagePopupUIPool : MonoBehaviour
    {
        public static DamagePopupUIPool I { get; private set; }

        [Header("Assign in Inspector")]
        [Tooltip("권장: Canvas 오브젝트의 RectTransform(=Canvas 자체)을 드래그")]
        public RectTransform canvasRect;

        [Tooltip("PF_DamagePopupUI 프리팹(안에 DamagePopupUI가 있어야 함)")]
        public GameObject popupPrefab;

        [Tooltip("월드→스크린 변환용. 비우면 Camera.main")]
        public Camera worldCamera;

        [Header("Pool")]
        public int prewarm = 30;

        [Header("Debug")]
        public bool debugLog = false;

        Canvas _canvas;
        RectTransform _container;

        readonly Queue<DamagePopupUI> _pool = new Queue<DamagePopupUI>(128);

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;

            if (canvasRect == null)
            {
                Debug.LogError("[DamagePopupUIPool] canvasRect(Canvas의 RectTransform)을 인스펙터에 연결해줘!");
                return;
            }

            _canvas = canvasRect.GetComponent<Canvas>();
            if (_canvas == null) _canvas = canvasRect.GetComponentInParent<Canvas>();
            if (_canvas == null)
            {
                Debug.LogError("[DamagePopupUIPool] canvasRect에서 Canvas를 찾지 못했어. (Canvas 오브젝트를 드래그했는지 확인)");
                return;
            }

            if (popupPrefab == null)
            {
                Debug.LogError("[DamagePopupUIPool] popupPrefab(PF_DamagePopupUI)를 연결해줘!");
                return;
            }

            if (worldCamera == null) worldCamera = Camera.main;

            // 팝업 전용 컨테이너(항상 UI 최상단)
            var go = new GameObject("DamagePopupContainer", typeof(RectTransform));
            _container = go.GetComponent<RectTransform>();
            _container.SetParent(canvasRect, false);
            _container.anchorMin = Vector2.zero;
            _container.anchorMax = Vector2.one;
            _container.offsetMin = Vector2.zero;
            _container.offsetMax = Vector2.zero;
            _container.pivot = new Vector2(0.5f, 0.5f);
            _container.SetAsLastSibling();

            for (int i = 0; i < prewarm; i++)
            {
                var p = CreateOne();
                if (p == null) break;
                ReturnToPool(p);
            }

            if (debugLog)
                Debug.Log($"[DamagePopupUIPool] Awake OK. canvas={_canvas.name}, prewarm={prewarm}");
        }

        void OnDestroy()
        {
            if (I == this) I = null;
        }

        DamagePopupUI CreateOne()
        {
            var go = Instantiate(popupPrefab, _container);

            // 프리팹 구조가 루트/자식 어디에 붙어도 최대한 찾아줌
            var p = go.GetComponent<DamagePopupUI>();
            if (p == null) p = go.GetComponentInChildren<DamagePopupUI>(true);

            if (p == null)
            {
                Debug.LogError("[DamagePopupUIPool] PF_DamagePopupUI 안에 DamagePopupUI가 없어요!");
                Destroy(go);
                return null;
            }

            // anchoredPosition이 잘 먹게 중앙 기준으로 고정
            var rt = p.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            p.Finished -= ReturnToPool;
            p.Finished += ReturnToPool;

            return p;
        }

        void ReturnToPool(DamagePopupUI p)
        {
            if (p == null) return;
            p.gameObject.SetActive(false);
            _pool.Enqueue(p);
        }

        public void Show(int amount, Vector3 worldPos)
        {
            if (_canvas == null || _container == null || popupPrefab == null) return;
            if (worldCamera == null) worldCamera = Camera.main;

            // 월드 → 스크린
            Vector3 sp = (worldCamera != null) ? worldCamera.WorldToScreenPoint(worldPos) : worldPos;
            if (sp.z < 0f) return; // 카메라 뒤

            // Screen Space - Camera/World Space면 UI 카메라 필요
            Camera uiCam = null;
            if (_canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCam = (_canvas.worldCamera != null) ? _canvas.worldCamera : worldCamera;

            // ✅ "Canvas(RectTransform) 기준" 로컬 좌표로 변환
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                sp,
                uiCam,
                out Vector2 localPos
            );

            var p = (_pool.Count > 0) ? _pool.Dequeue() : CreateOne();
            if (p == null) return;

            p.transform.SetParent(_container, false);
            p.Play(amount, localPos);

            if (debugLog)
                Debug.Log($"[DamagePopupUIPool] Show amount={amount} localPos={localPos} renderMode={_canvas.renderMode}");
        }
    }
}
