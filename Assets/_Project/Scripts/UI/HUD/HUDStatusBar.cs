using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VSL.UI
{
    public class HUDStatusBar : MonoBehaviour
    {
        [Header("UI")]
        public Slider hpSlider;      // 초록 바
        public Slider xpSlider;      // 노랑 바
        public TMP_Text levelText;   // "Lv 1" 같은 표시(선택)
        public TMP_Text xpText;      // "12 / 20" 같은 표시(선택)

        [Header("Target (optional)")]
        public Transform playerRoot; // 비워두면 Tag=Player로 자동 찾기

        private VSL.Health _hp;
        private VSL.Experience _exp;

        private void Start()
        {
            BindPlayer();
            RefreshAll(); // 시작 시 1회 갱신(초기값 표시)
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void BindPlayer()
        {
            Unbind();

            if (playerRoot == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) playerRoot = p.transform;
            }

            if (playerRoot == null)
            {
                Debug.LogWarning("[HUDStatusBar] Player not found. (Tag=Player 확인)");
                return;
            }

            _hp = playerRoot.GetComponentInChildren<VSL.Health>();
            _exp = playerRoot.GetComponentInChildren<VSL.Experience>();

            if (_hp != null) _hp.OnChanged += OnHpChanged;
            if (_exp != null) _exp.OnXPChanged += OnXpChanged;
        }

        private void Unbind()
        {
            if (_hp != null) _hp.OnChanged -= OnHpChanged;
            if (_exp != null) _exp.OnXPChanged -= OnXpChanged;

            _hp = null;
            _exp = null;
        }

        private void RefreshAll()
        {
            if (_hp != null) OnHpChanged(_hp.CurrentHP, _hp.MaxHP);
            if (_exp != null) OnXpChanged(_exp.CurrentXP, _exp.XPToNext, _exp.Level);
        }

        private void OnHpChanged(int current, int max)
        {
            if (hpSlider == null) return;

            max = Mathf.Max(1, max);
            current = Mathf.Clamp(current, 0, max);

            // 슬라이더 값/최대값 방식(직관적)
            hpSlider.minValue = 0;
            hpSlider.maxValue = max;
            hpSlider.value = current;
        }

        private void OnXpChanged(int current, int toNext, int level)
        {
            if (xpSlider != null)
            {
                toNext = Mathf.Max(1, toNext);
                current = Mathf.Clamp(current, 0, toNext);

                xpSlider.minValue = 0;
                xpSlider.maxValue = toNext;
                xpSlider.value = current;
            }

            if (levelText != null)
                levelText.text = $"Lv {level}";

            if (xpText != null)
                xpText.text = $"{current} / {toNext}";
        }
    }
}
