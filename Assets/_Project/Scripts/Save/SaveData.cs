using System;
using UnityEngine;

namespace VSL
{
    public enum UltimateSlot { A = 0, B = 1 }

    [Serializable]
    public class SaveData
    {
        public int version = 3;

        // ✅ 전역 포인트 1개만 사용
        public int totalEarnedGlobal = 0;

        public JobProgress[] jobs;

        public static SaveData CreateDefault()
        {
            var values = (JobType[])Enum.GetValues(typeof(JobType));

            var data = new SaveData();
            data.version = 3;
            data.totalEarnedGlobal = 0;

            data.jobs = new JobProgress[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                data.jobs[i] = JobProgress.CreateDefault(values[i]);
            }

            return data;
        }
    }

    [Serializable]
    public class JobProgress
    {
        public JobType job;

        // ✅ 레거시(직업별 포인트). 마이그레이션용으로만 남김
        public int totalEarned;

        public bool hasClearedOnce;

        // 공용 초월
        public int spentDamage;
        public int spentFireRate;
        public int spentMoveSpeed;

        // 직업 전용 2개
        public int spentSpecialA;
        public int spentSpecialB;

        // 궁극기 메타(레벨=소모포인트)
        public int ultSelectedSlot; // 0=A, 1=B
        public int ultLevelA;       // 0~5
        public int ultLevelB;       // 0~5

        public static JobProgress CreateDefault(JobType jt)
        {
            return new JobProgress
            {
                job = jt,
                totalEarned = 0,
                hasClearedOnce = false,

                spentDamage = 0,
                spentFireRate = 0,
                spentMoveSpeed = 0,
                spentSpecialA = 0,
                spentSpecialB = 0,

                ultSelectedSlot = 0,
                ultLevelA = 0,
                ultLevelB = 0
            };
        }

        public int TotalSpent()
        {
            return spentDamage
                 + spentFireRate
                 + spentMoveSpeed
                 + spentSpecialA
                 + spentSpecialB
                 + ultLevelA
                 + ultLevelB;
        }

        public void Clamp()
        {
            ultSelectedSlot = Mathf.Clamp(ultSelectedSlot, 0, 1);
            ultLevelA = Mathf.Clamp(ultLevelA, 0, 5);
            ultLevelB = Mathf.Clamp(ultLevelB, 0, 5);

            // 음수 방지
            spentDamage = Mathf.Max(0, spentDamage);
            spentFireRate = Mathf.Max(0, spentFireRate);
            spentMoveSpeed = Mathf.Max(0, spentMoveSpeed);
            spentSpecialA = Mathf.Max(0, spentSpecialA);
            spentSpecialB = Mathf.Max(0, spentSpecialB);
        }
    }
}
