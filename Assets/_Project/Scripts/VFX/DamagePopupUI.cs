using System;
using TMPro;
using UnityEngine;

namespace VSL.VFX
{
    public class DamagePopupUI : MonoBehaviour
    {
        [Header("Refs")]
        public TMP_Text text;                // TextMeshProUGUI

        [Header("Anim (unscaled)")]
        public float life = 0.7f;            // 지속시간
        public float floatPixels = 60f;      // 위로 뜨는 픽셀
        public Vector2 randomJitter = new Vector2(10f, 0f); // 살짝 흔들림(선택)

        public event Action<DamagePopupUI> Finished;

        RectTransform _rt;
        float _t;
        Color _baseColor;
        Vector2 _start;

        void Awake()
        {
            _rt = GetComponent<RectTransform>();
            if (text == null) text = GetComponentInChildren<TMP_Text>(true);
            if (text != null) _baseColor = text.color;
        }

        public void Play(int amount, Vector2 anchoredPos)
        {
            _t = 0f;

            // 시작 위치 + 약간 랜덤(가시성/겹침 개선)
            if (randomJitter.x != 0f || randomJitter.y != 0f)
                anchoredPos += new Vector2(UnityEngine.Random.Range(-randomJitter.x, randomJitter.x),
                                           UnityEngine.Random.Range(-randomJitter.y, randomJitter.y));

            _start = anchoredPos;
            if (_rt != null) _rt.anchoredPosition = anchoredPos;

            if (text != null)
            {
                text.text = amount.ToString();
                text.color = _baseColor; // 알파 복구
            }

            gameObject.SetActive(true);
        }

        void Update()
        {
            // Time.timeScale=0이어도 UI는 떠야하므로 unscaled 사용
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
