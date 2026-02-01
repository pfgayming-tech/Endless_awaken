using System;
using TMPro;
using UnityEngine;

namespace VSL.VFX
{
    public class DamagePopupUI : MonoBehaviour
    {
        [Header("Refs")]
        public TMP_Text text; // TextMeshProUGUI

        [Header("Anim (unscaled)")]
        public float life = 0.7f;                        // 지속 시간
        public float floatPixels = 60f;                  // 위로 뜨는 픽셀
        public Vector2 randomJitter = new Vector2(10f, 0f); // 살짝 흔들림(선택)

        public event System.Action<DamagePopupUI> Finished;

        RectTransform _rt;
        float _t;
        Vector2 _start;

        Color _baseColor;
        float _baseFontSize;
        Vector3 _baseScale;

        void Awake()
        {
            _rt = GetComponent<RectTransform>();
            if (text == null) text = GetComponentInChildren<TMP_Text>(true);

            if (text != null)
            {
                _baseColor = text.color;
                _baseFontSize = text.fontSize;
            }

            _baseScale = (_rt != null) ? _rt.localScale : Vector3.one;
        }

        // 기존 API 유지
        public void Play(int amount, Vector2 anchoredPos)
        {
            PlayStyled(amount, anchoredPos, null, 1f, 1f);
        }

        // ✅ 스타일 지원(색/스케일/폰트 크기)
        public void PlayStyled(int amount, Vector2 anchoredPos, Color? colorOverride, float scaleMult, float fontSizeMult)
        {
            _t = 0f;

            // 시작 위치 + 약간 랜덤(가독성 개선)
            if (randomJitter.x != 0f || randomJitter.y != 0f)
                anchoredPos += new Vector2(UnityEngine.Random.Range(-randomJitter.x, randomJitter.x),
                                           UnityEngine.Random.Range(-randomJitter.y, randomJitter.y));

            _start = anchoredPos;

            if (_rt != null)
            {
                _rt.anchoredPosition = anchoredPos;
                _rt.localScale = _baseScale * Mathf.Max(0.01f, scaleMult);
            }

            if (text != null)
            {
                text.text = amount.ToString();

                // 스타일 적용
                text.fontSize = _baseFontSize * Mathf.Max(0.01f, fontSizeMult);

                var c = colorOverride.HasValue ? colorOverride.Value : _baseColor;
                c.a = 1f; // 알파는 여기서 고정하고 Update에서 페이드
                text.color = c;
            }

            gameObject.SetActive(true);
        }

        void Update()
        {
            // Time.timeScale=0이어도 UI는 떠야 하므로 unscaled 사용
            _t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(_t / Mathf.Max(0.01f, life));

            if (_rt != null)
                _rt.anchoredPosition = _start + Vector2.up * (floatPixels * k);

            if (text != null)
            {
                var c = text.color;
                c.a = Mathf.Lerp(1f, 0f, k);
                text.color = c;
            }

            if (_t >= life)
            {
                gameObject.SetActive(false);
                Finished?.Invoke(this);
            }
        }
    }
}
