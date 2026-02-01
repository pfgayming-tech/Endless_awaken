using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VSL.UI;

namespace VSL
{
    public class UIManager : MonoBehaviour
    {
        // ✅ 씬에 UIManager가 여러 개 생기는 걸 방지
        public static UIManager I { get; private set; }

        [Header("HUD")]
        public Slider hpSlider;
        public Slider xpSlider;
        public TMP_Text timerText;
        public TMP_Text levelText;

        [Header("Panels")]
        public LevelUpPanel levelUpPanel;

        [Tooltip("ON이면 레벨업이 연속으로 여러 번 떠도, 패널이 이미 켜져 있을 땐 추가 호출을 무시합니다(한 번만 뜸).")]
        public bool ignoreLevelUpWhilePanelOpen = true;

        [Header("Result")]
        public GameObject resultRoot;
        public TMP_Text resultTitleText;
        public TMP_Text resultRewardText;
        public Button backToMenuButton;

        private Health _hp;
        private Experience _xp;
        private GameManager _gm;
        private PlayerStats _stats;

        private void Awake()
        {
            if (I != null && I != this)
            {
                // ✅ 중복 UIManager 제거
                Destroy(gameObject);
                return;
            }
            I = this;
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (I == this) I = null;
        }

        private void Unsubscribe()
        {
            if (_hp != null) _hp.OnChanged -= OnHpChanged;

            if (_xp != null)
            {
                _xp.OnXPChanged -= OnXpChanged;
                _xp.OnLevelUp -= OnLevelUp;
            }
        }

        public void Bind(Health hp, Experience xp, GameManager gm)
        {
            // ✅ 이전 구독 해제(중복 방지)
            Unsubscribe();

            _hp = hp;
            _xp = xp;
            _gm = gm;

            var player = GameObject.FindGameObjectWithTag("Player");
            _stats = player != null ? player.GetComponent<PlayerStats>() : null;

            if (_hp != null) _hp.OnChanged += OnHpChanged;

            if (_xp != null)
            {
                _xp.OnXPChanged += OnXpChanged;
                _xp.OnLevelUp += OnLevelUp;
            }

            if (levelUpPanel != null)
                levelUpPanel.Bind(_stats);

            if (resultRoot != null)
                resultRoot.SetActive(false);

            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.RemoveAllListeners();
                backToMenuButton.onClick.AddListener(() =>
                {
                    if (_gm != null) _gm.BackToMenu();
                });
            }

            if (_hp != null) OnHpChanged(_hp.CurrentHP, _hp.MaxHP);
            if (_xp != null) OnXpChanged(_xp.CurrentXP, _xp.XPToNext, _xp.Level);


        }

        private void OnHpChanged(int cur, int max)
        {
            if (hpSlider != null)
                hpSlider.value = (max <= 0) ? 0f : (cur / (float)max);
        }

        private void OnXpChanged(int cur, int toNext, int level)
        {
            if (xpSlider != null)
                xpSlider.value = (toNext <= 0) ? 0f : (cur / (float)toNext);

            if (levelText != null)
                levelText.text = $"Lv {level}";
        }

        private void OnLevelUp(int newLevel)
        {
            Debug.Log($"[UIManager] OnLevelUp (Lv {newLevel})");

       

            levelUpPanel.Show(newLevel);
        }



        public void SetTimer(float remainSeconds)
        {
            if (timerText == null) return;

            remainSeconds = Mathf.Max(0f, remainSeconds);
            int m = Mathf.FloorToInt(remainSeconds / 60f);
            int s = Mathf.FloorToInt(remainSeconds % 60f);
            timerText.text = $"{m:00}:{s:00}";
        }

        public void ShowResult(bool cleared, int reward)
        {
            if (resultRoot == null) return;

            resultRoot.SetActive(true);
            if (resultTitleText != null) resultTitleText.text = cleared ? "클리어!" : "실패";
            if (resultRewardText != null)
                resultRewardText.text = cleared ? $"초월포인트 +{reward}" : "";
        }

        private static void EnsureActiveUpwards(GameObject go)
        {
            if (go == null) return;

            Transform t = go.transform;
            while (t != null)
            {
                if (!t.gameObject.activeSelf)
                    t.gameObject.SetActive(true);

                t = t.parent;
            }
        }
    }
}
