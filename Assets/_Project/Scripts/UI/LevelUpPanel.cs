using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VSL.UI
{
    public class LevelUpPanel : MonoBehaviour
    {
        [Header("Root (패널 내용 루트)")]
        public GameObject root;                 // 보통 PanelRoot 같은 자식 오브젝트

        [Header("UI (3 options)")]
        public Button[] optionButtons;          // 3개
        public TMP_Text[] optionTexts;          // 3개

        [Header("Tuning (DEV - 확연히 차이나게)")]
        public float damageMult = 1.50f;        // 데미지 x1.5
        public float moveSpeedMult = 1.35f;     // 이동속도 x1.35
        public float shotIntervalMult = 0.70f;  // 발사간격 x0.7 (= 더 빠름)
        public int pierceUp = 1;                // 관통 +1
        public int splitUp = 1;                 // 분열 +1
        public float sizeMult = 2.0f;          // ✅ 크기 x1.5 (표기/설명용)

        [Header("Behaviour")]
        public bool pauseGameOnShow = true;

        private PlayerStats _stats;

        private bool _isShowing;
        private int _showingLevel = -1;
        private int _lastEnqueuedLevel = -1;
        private readonly Queue<int> _pendingLevels = new Queue<int>();

        private float _prevTimeScale = 1f;
        private Coroutine _pauseCo;

        private enum UpgradeType
        {
            Pierce,
            DamageUp,
            ShotIntervalDown,
            Split,
            MoveSpeedUp,
            SizeUp
        }

        // ✅ 6개 중 3개 랜덤
        private readonly List<UpgradeType> _all = new List<UpgradeType>
        {
            UpgradeType.Pierce,
            UpgradeType.DamageUp,
            UpgradeType.ShotIntervalDown,
            UpgradeType.Split,
            UpgradeType.MoveSpeedUp,
            UpgradeType.SizeUp
        };

        private void Awake()
        {
            if (root == null) root = gameObject;

            root.SetActive(false);
            _isShowing = false;
        }

        public void Bind(PlayerStats stats)
        {
            _stats = stats;
        }

        // UIManager에서 레벨업 때 호출: Show(newLevel)
        public void Show(int level)
        {
            if (_stats == null)
            {
                Debug.LogError("[LevelUpPanel] Bind(stats)가 안 됐어. UIManager에서 levelUpPanel.Bind(_stats) 확인!");
                return;
            }

            // 이미 떠있으면 중복 호출 방지 + 레벨이 여러 번 오른 경우만 큐잉
            if (_isShowing)
            {
                if (level <= _showingLevel) return;

                if (level != _lastEnqueuedLevel)
                {
                    _pendingLevels.Enqueue(level);
                    _lastEnqueuedLevel = level;
                }
                return;
            }

            _showingLevel = level;
            ShowInternal();
        }

        private void ShowInternal()
        {
            if (root == null) root = gameObject;

            if (optionButtons == null || optionButtons.Length < 3 ||
                optionTexts == null || optionTexts.Length < 3)
            {
                Debug.LogError("[LevelUpPanel] optionButtons/optionTexts에 3개씩 연결해줘. (각 버튼 + 버튼안 TMP 텍스트)");
                return;
            }

            _isShowing = true;

            // 최상단
            transform.SetAsLastSibling();

            // 켜기
            root.SetActive(true);

            // CanvasGroup 강제 표시(있다면)
            var cg = root.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

            // Animator가 있으면 timeScale=0에서도 돌게
            var anims = root.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < anims.Length; i++)
                anims[i].updateMode = AnimatorUpdateMode.UnscaledTime;

            // timeScale=0은 한 프레임 뒤에(패널 렌더링 먼저)
            if (pauseGameOnShow)
            {
                _prevTimeScale = Time.timeScale;

                if (_pauseCo != null) StopCoroutine(_pauseCo);
                _pauseCo = StartCoroutine(PauseNextFrame());
            }

            // 6개 중 3개 랜덤(중복 X)
            var picks = Pick3Unique();

            for (int i = 0; i < 3; i++)
            {
                var type = picks[i];
                optionTexts[i].text = BuildLabel(type);

                optionButtons[i].onClick.RemoveAllListeners();
                int captured = i;
                optionButtons[i].onClick.AddListener(() =>
                {
                    Apply(picks[captured]);
                    HideAndContinue();
                });
            }
        }

        private IEnumerator PauseNextFrame()
        {
            yield return null;
            Time.timeScale = 0f;
        }

        private List<UpgradeType> Pick3Unique()
        {
            var tmp = new List<UpgradeType>(_all);
            for (int i = tmp.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (tmp[i], tmp[j]) = (tmp[j], tmp[i]);
            }
            return new List<UpgradeType> { tmp[0], tmp[1], tmp[2] };
        }

        private string BuildLabel(UpgradeType type)
        {
            float interval = (_stats.fireRate <= 0.0001f) ? 999f : (1f / _stats.fireRate);

            switch (type)
            {
                case UpgradeType.Pierce:
                    return $"관통 +{pierceUp}  ( {_stats.pierce} → {_stats.pierce + pierceUp} )";

                case UpgradeType.DamageUp:
                    return $"데미지 x{damageMult:0.##}  ( {_stats.damage:0.#} → {_stats.damage * damageMult:0.#} )";

                case UpgradeType.ShotIntervalDown:
                    return $"발사간격 감소 (x{shotIntervalMult:0.##})  ( {interval:0.00}s → {interval * shotIntervalMult:0.00}s )";

                case UpgradeType.Split:
                    return $"발사체 분열 +{splitUp}  ( {_stats.extraProjectiles} → {_stats.extraProjectiles + splitUp} )";

                case UpgradeType.MoveSpeedUp:
                    return $"이동속도 x{moveSpeedMult:0.##}  ( {_stats.moveSpeed:0.00} → {_stats.moveSpeed * moveSpeedMult:0.00} )";

                case UpgradeType.SizeUp:
                    return $"발사체 크기 x{sizeMult:0.##}  ( x{_stats.projectileSizeMult:0.##} → x{_stats.projectileSizeMult * sizeMult:0.##} )";
            }

            return type.ToString();
        }

        private void Apply(UpgradeType type)
        {
            if (_stats == null) return;

            // ✅ IMPORTANT:
            // PlayerStats.ApplyLevelUpUpgrade 시그니처가 (type, pierceUp, splitUp, dmgMult, intervalMult, moveSpeedMult) 6개 인자라고 가정하고 맞춤
            switch (type)
            {
                case UpgradeType.Pierce:
                    _stats.AddPierceBonus(pierceUp);
                    break;

                case UpgradeType.Split:
                    _stats.AddSplitBonus(splitUp);
                    break;


                case UpgradeType.DamageUp:
                    _stats.ApplyLevelUpUpgrade(LevelUpUpgradeType.DamageUp, 0, 0, damageMult, 1f, 1f);
                    break;

                case UpgradeType.ShotIntervalDown:
                    _stats.ApplyLevelUpUpgrade(LevelUpUpgradeType.ShotIntervalDown, 0, 0, 1f, shotIntervalMult, 1f);
                    break;

                case UpgradeType.MoveSpeedUp:
                    _stats.ApplyLevelUpUpgrade(LevelUpUpgradeType.MoveSpeedUp, 0, 0, 1f, 1f, moveSpeedMult);
                    break;

                case UpgradeType.SizeUp:
                    // SizeUp은 PlayerStats 쪽에서 내부적으로 projectileSizeMult를 키우도록 구현돼 있어야 함
                    _stats.ApplyLevelUpUpgrade(LevelUpUpgradeType.SizeUp, 0, 0, 1f, 1f, 1f);
                    break;
            }

            Debug.Log($"[LevelUp Applied] {type} => pierce={_stats.pierce}, split={_stats.extraProjectiles}, dmg={_stats.damage}, rate={_stats.fireRate}, ms={_stats.moveSpeed}, size={_stats.projectileSizeMult}");
        }

        private void HideAndContinue()
        {
            if (_pauseCo != null)
            {
                StopCoroutine(_pauseCo);
                _pauseCo = null;
            }

            root.SetActive(false);
            _isShowing = false;

            if (pauseGameOnShow)
                Time.timeScale = _prevTimeScale;

            if (_pendingLevels.Count > 0)
            {
                _showingLevel = _pendingLevels.Dequeue();
                StartCoroutine(ShowNextFrame());
            }
            else
            {
                _lastEnqueuedLevel = -1;
            }
        }

        private IEnumerator ShowNextFrame()
        {
            yield return null;
            ShowInternal();
        }
    }
}
