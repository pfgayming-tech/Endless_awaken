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

        [Header("Ultimate Style")]
        [Tooltip("궁극기 활성 중일 때 데미지 팝업을 강조할지")]
        public bool ultimateStyleEnabled = true;

        [Tooltip("궁극기 활성 중 데미지 색")]
        public Color ultimateColor = new Color(1f, 0.85f, 0.25f, 1f); // 골드

        [Tooltip("궁극기 활성 중 전체 스케일 배수")]
        public float ultimateScaleMult = 1.35f;

        [Tooltip("궁극기 활성 중 폰트 크기 배수")]
        public float ultimateFontSizeMult = 1.15f;

        [Header("Debug")]
        public bool debugLog = false;

        Canvas _canvas;
        RectTransform _container;
        bool _ultimateActive;

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

        /// <summary>UltimateSystem에서 ON/OFF 해주면 됨</summary>
        public void SetUltimateActive(bool active)
        {
            _ultimateActive = active;
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

        // ✅ 기존 호출 유지(Health.cs에서 이걸 씀)
        public void Show(int amount, Vector3 worldPos)
        {
            if (_ultimateActive && ultimateStyleEnabled)
                ShowInternal(amount, worldPos, ultimateColor, ultimateScaleMult, ultimateFontSizeMult);
            else
                ShowInternal(amount, worldPos, null, 1f, 1f);
        }

        // 원하면 다른 곳에서 직접 호출 가능
        public void ShowStyled(int amount, Vector3 worldPos, Color color, float scaleMult = 1f, float fontSizeMult = 1f)
        {
            ShowInternal(amount, worldPos, color, scaleMult, fontSizeMult);
        }

        void ShowInternal(int amount, Vector3 worldPos, Color? colorOverride, float scaleMult, float fontSizeMult)
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

            // Canvas(RectTransform) 기준 로컬 좌표로 변환
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                sp,
                uiCam,
                out Vector2 localPos
            );

            var p = (_pool.Count > 0) ? _pool.Dequeue() : CreateOne();
            if (p == null) return;

            p.transform.SetParent(_container, false);

            if (colorOverride.HasValue || scaleMult != 1f || fontSizeMult != 1f)
                p.PlayStyled(amount, localPos, colorOverride, scaleMult, fontSizeMult);
            else
                p.Play(amount, localPos);

            if (debugLog)
                Debug.Log($"[DamagePopupUIPool] Show amount={amount} localPos={localPos} ultimateActive={_ultimateActive}");
        }
    }
}
