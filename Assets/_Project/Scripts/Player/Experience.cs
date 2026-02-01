using System;
using UnityEngine;

namespace VSL
{
    public class Experience : MonoBehaviour
    {
        public int Level { get; private set; } = 1;
        public int CurrentXP { get; private set; }
        public int XPToNext { get; private set; } = 2;

        public event Action<int, int, int> OnXPChanged; // current, toNext, level
        public event Action<int> OnLevelUp; // new level

        [Header("Dev / Tuning")]
        [Tooltip("true면 AddXP로 경험치가 올라감(몹 처치/드랍 등). 개발 중엔 보통 false로 둠.")]
        public bool enableRuntimeXP = false;

        [Tooltip("개발용 단축키로 레벨업 (0이면 사용 안 함)")]
        public KeyCode devHotkey = KeyCode.L;

        [Tooltip("레벨업 당 XPToNext 증가량(현재 코드: +2)")]
        public int xpToNextAdd = 2;

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (devHotkey != KeyCode.None && Input.GetKeyDown(devHotkey))
            {
                DevLevelUp(1);
            }
#endif
        }

        /// <summary>
        /// ✅ 몬스터 처치/드랍 등에서 호출되는 경험치 추가(기본: 막음)
        /// </summary>
        public void AddXP(int amount)
        {
            if (!enableRuntimeXP) return; // ✅ 몹 경험치(런타임 XP) 전부 차단

            amount = Mathf.Max(0, amount);
            if (amount == 0) return;

            CurrentXP += amount;
            ProcessLevelUps();
            OnXPChanged?.Invoke(CurrentXP, XPToNext, Level);
        }

        /// <summary>
        /// ✅ 개발용 레벨업: 버튼/키로 호출
        /// </summary>
        public void DevLevelUp(int times = 1)
        {
            times = Mathf.Max(1, times);

            for (int i = 0; i < times; i++)
            {
                Level++;
                CurrentXP = 0; // 개발 편의상 0으로 초기화(원하면 유지해도 됨)
                XPToNext = Mathf.RoundToInt(XPToNext + xpToNextAdd);

                OnLevelUp?.Invoke(Level);
            }

            OnXPChanged?.Invoke(CurrentXP, XPToNext, Level);
        }

        private void ProcessLevelUps()
        {
            while (CurrentXP >= XPToNext)
            {
                CurrentXP -= XPToNext;
                Level++;
                XPToNext = Mathf.RoundToInt(XPToNext + xpToNextAdd);
                OnLevelUp?.Invoke(Level);
            }
        }
    }
}
