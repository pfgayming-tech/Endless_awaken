using System;
using UnityEngine;

namespace VSL
{
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public JobProgress[] jobs;

        public static SaveData CreateDefault()
        {
            var values = (JobType[])Enum.GetValues(typeof(JobType));
            var data = new SaveData();
            data.jobs = new JobProgress[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                data.jobs[i] = new JobProgress
                {
                    job = values[i],
                    totalEarned = 0,
                    hasClearedOnce = false,
                    spentDamage = 0,
                    spentFireRate = 0,
                    spentMoveSpeed = 0,
                    spentSpecialA = 0,
                    spentSpecialB = 0
                };
            }
            return data;
        }
    }

    [Serializable]
    public class JobProgress
    {
        public JobType job;

        public int totalEarned;
        public bool hasClearedOnce;

        // 공용 초월
        public int spentDamage;
        public int spentFireRate;
        public int spentMoveSpeed;

        // 직업 전용 2개
        public int spentSpecialA;
        public int spentSpecialB;

        public int TotalSpent()
            => spentDamage + spentFireRate + spentMoveSpeed + spentSpecialA + spentSpecialB;
    }
}
