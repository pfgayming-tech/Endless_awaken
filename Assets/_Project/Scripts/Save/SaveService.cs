using System;
using UnityEngine;

namespace VSL
{
    public static class SaveService
    {
        private const string KEY = "VSL_SAVE_V1";
        private static SaveData _cache;

        public static SaveData Data
        {
            get
            {
                if (_cache == null) _cache = LoadInternal();
                EnsureAllJobsExist(_cache);
                return _cache;
            }
        }

        public static void Save()
        {
            PlayerPrefs.SetString(KEY, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        private static SaveData LoadInternal()
        {
            if (PlayerPrefs.HasKey(KEY))
            {
                var json = PlayerPrefs.GetString(KEY);
                var loaded = JsonUtility.FromJson<SaveData>(json);
                if (loaded != null) return loaded;
            }

            var fresh = SaveData.CreateDefault();
            PlayerPrefs.SetString(KEY, JsonUtility.ToJson(fresh));
            return fresh;
        }

        private static void EnsureAllJobsExist(SaveData data)
        {
            if (data.jobs == null || data.jobs.Length == 0)
            {
                var fresh = SaveData.CreateDefault();
                data.jobs = fresh.jobs;
                return;
            }

            int expected = Enum.GetValues(typeof(JobType)).Length;
            if (data.jobs.Length != expected)
            {
                var fresh = SaveData.CreateDefault();

                // 가능한 것만 복사
                foreach (var jp in data.jobs)
                {
                    var dst = GetJobSafe(fresh, jp.job);
                    dst.totalEarned = jp.totalEarned;
                    dst.hasClearedOnce = jp.hasClearedOnce;
                    dst.spentDamage = jp.spentDamage;
                    dst.spentFireRate = jp.spentFireRate;
                    dst.spentMoveSpeed = jp.spentMoveSpeed;
                    dst.spentSpecialA = jp.spentSpecialA;
                    dst.spentSpecialB = jp.spentSpecialB;
                }

                data.jobs = fresh.jobs;
            }
        }

        public static JobProgress GetJob(JobType job) => GetJobSafe(Data, job);

        private static JobProgress GetJobSafe(SaveData data, JobType job)
        {
            for (int i = 0; i < data.jobs.Length; i++)
            {
                if (data.jobs[i].job == job) return data.jobs[i];
            }

            // 이론상 여기 오면 안되지만, 안전장치
            var fresh = SaveData.CreateDefault();
            data.jobs = fresh.jobs;
            return data.jobs[0];
        }

        public static int GetAvailablePoints(JobType job)
        {
            var jp = GetJob(job);
            return jp.totalEarned - jp.TotalSpent();
        }

        // ✅ 여기 수정: System.SystemAction -> System.Action
        public static bool TrySpend(JobType job, Action<JobProgress> mutate)
        {
            var jp = GetJob(job);
            if (GetAvailablePoints(job) <= 0) return false;

            mutate?.Invoke(jp);
            Save();
            return true;
        }

        public static void ResetSpent(JobType job)
        {
            var jp = GetJob(job);
            jp.spentDamage = 0;
            jp.spentFireRate = 0;
            jp.spentMoveSpeed = 0;
            jp.spentSpecialA = 0;
            jp.spentSpecialB = 0;
            Save();
        }
    }
}
