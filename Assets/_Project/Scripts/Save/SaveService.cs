using System;
using UnityEngine;

namespace VSL
{
    public static class SaveService
    {
        private const string KEY = "VSL_SAVE_V1";

        private static SaveData _cache;
        private static bool _initialized;

        // ✅ Data getter에서는 "초기화 1회"만 보장
        public static SaveData Data
        {
            get
            {
                EnsureInitialized();
                return _cache;
            }
        }

        private static void EnsureInitialized()
        {
            if (_initialized) return;

            _cache = LoadOrCreate();

            bool dirty = false;
            dirty |= EnsureAllJobsExist(_cache);
            dirty |= MigrateJobEarnedToGlobal(_cache);

            // clamp
            if (_cache.jobs != null)
            {
                for (int i = 0; i < _cache.jobs.Length; i++)
                    _cache.jobs[i].Clamp();
            }

            if (dirty)
                SaveInternal();

            _initialized = true;
        }

        private static SaveData LoadOrCreate()
        {
            SaveData loaded = null;

            if (PlayerPrefs.HasKey(KEY))
            {
                try
                {
                    var json = PlayerPrefs.GetString(KEY);
                    loaded = JsonUtility.FromJson<SaveData>(json);
                }
                catch
                {
                    loaded = null;
                }
            }

            if (loaded == null)
            {
                loaded = SaveData.CreateDefault();
                PlayerPrefs.SetString(KEY, JsonUtility.ToJson(loaded));
                PlayerPrefs.Save();
            }

            // 버전 보정
            if (loaded.version < 3)
                loaded.version = 3;

            return loaded;
        }

        // ✅ Save()는 Data를 다시 만지지 않고 _cache만 저장
        public static void Save()
        {
            EnsureInitialized();
            SaveInternal();
        }

        private static void SaveInternal()
        {
            PlayerPrefs.SetString(KEY, JsonUtility.ToJson(_cache));
            PlayerPrefs.Save();
        }

        private static bool MigrateJobEarnedToGlobal(SaveData data)
        {
            // global이 비어있고, 레거시 totalEarned가 남아있을 때 1회 합산
            if (data.totalEarnedGlobal > 0) return false;
            if (data.jobs == null) return false;

            int sum = 0;
            for (int i = 0; i < data.jobs.Length; i++)
                sum += Mathf.Max(0, data.jobs[i].totalEarned);

            if (sum <= 0) return false;

            data.totalEarnedGlobal = sum;

            // 레거시 비우기
            for (int i = 0; i < data.jobs.Length; i++)
                data.jobs[i].totalEarned = 0;

            return true;
        }

        private static bool EnsureAllJobsExist(SaveData data)
        {
            var values = (JobType[])Enum.GetValues(typeof(JobType));
            int expected = values.Length;

            if (data.jobs == null || data.jobs.Length == 0)
            {
                data.jobs = new JobProgress[expected];
                for (int i = 0; i < expected; i++)
                    data.jobs[i] = JobProgress.CreateDefault(values[i]);
                return true;
            }

            if (data.jobs.Length == expected)
                return false;

            // 길이가 다르면 새로 만들고 가능한 것만 복사
            var fresh = new JobProgress[expected];
            for (int i = 0; i < expected; i++)
                fresh[i] = JobProgress.CreateDefault(values[i]);

            // 기존 데이터에서 job 맞는 것 찾아 복사
            for (int i = 0; i < data.jobs.Length; i++)
            {
                var old = data.jobs[i];
                int idx = IndexOf(values, old.job);
                if (idx < 0) continue;

                var dst = fresh[idx];
                dst.totalEarned = old.totalEarned;
                dst.hasClearedOnce = old.hasClearedOnce;

                dst.spentDamage = old.spentDamage;
                dst.spentFireRate = old.spentFireRate;
                dst.spentMoveSpeed = old.spentMoveSpeed;
                dst.spentSpecialA = old.spentSpecialA;
                dst.spentSpecialB = old.spentSpecialB;

                dst.ultSelectedSlot = old.ultSelectedSlot;
                dst.ultLevelA = old.ultLevelA;
                dst.ultLevelB = old.ultLevelB;
            }

            data.jobs = fresh;
            return true;
        }

        private static int IndexOf(JobType[] arr, JobType value)
        {
            for (int i = 0; i < arr.Length; i++)
                if (arr[i].Equals(value)) return i;
            return -1;
        }

        public static JobProgress GetJob(JobType job)
        {
            EnsureInitialized();

            for (int i = 0; i < _cache.jobs.Length; i++)
                if (_cache.jobs[i].job.Equals(job)) return _cache.jobs[i];

            // 안전장치: 없으면 강제로 Ensure 재생성
            EnsureAllJobsExist(_cache);
            for (int i = 0; i < _cache.jobs.Length; i++)
                if (_cache.jobs[i].job.Equals(job)) return _cache.jobs[i];

            return _cache.jobs[0];
        }

        // ✅ 전 직업 소모 합
        public static int GetTotalSpentAllJobs()
        {
            EnsureInitialized();

            int sum = 0;
            var jobs = _cache.jobs;
            if (jobs == null) return 0;

            for (int i = 0; i < jobs.Length; i++)
                sum += Mathf.Max(0, jobs[i].TotalSpent());

            return sum;
        }

        // ✅ 전역 포인트 1개 풀: 남은 포인트
        public static int GetAvailablePoints()
        {
            EnsureInitialized();
            return Mathf.Max(0, _cache.totalEarnedGlobal - GetTotalSpentAllJobs());
        }

        // ✅ 기존 코드 호환(직업 인자 무시)
        public static int GetAvailablePoints(JobType _ignoredJob)
        {
            return GetAvailablePoints();
        }

        // ✅ 클리어 보상 지급: 전역 포인트만 증가
        public static void AddEarnedPoints(int amount)
        {
            EnsureInitialized();

            amount = Mathf.Max(0, amount);
            if (amount == 0) return;

            _cache.totalEarnedGlobal += amount;
            SaveInternal();
        }

        // ✅ 기존 코드 호환
        public static void AddEarnedPoints(JobType _ignoredJob, int amount)
        {
            AddEarnedPoints(amount);
        }

        // ✅ 초월 구매: 남은 포인트가 있을 때만 mutate 수행
        public static bool TrySpend(JobType job, Action<JobProgress> mutate)
        {
            EnsureInitialized();
            if (GetAvailablePoints() <= 0) return false;

            var jp = GetJob(job);
            mutate?.Invoke(jp);
            jp.Clamp();
            SaveInternal();
            return true;
        }

        // ✅ 리셋(포인트 환급 효과는 자동으로 남은 포인트가 늘어나는 방식)
        public static void ResetSpent(JobType job, bool includeUltimate = true)
        {
            EnsureInitialized();

            var jp = GetJob(job);

            jp.spentDamage = 0;
            jp.spentFireRate = 0;
            jp.spentMoveSpeed = 0;

            jp.spentSpecialA = 0;
            jp.spentSpecialB = 0;

            if (includeUltimate)
            {
                jp.ultSelectedSlot = 0;
                jp.ultLevelA = 0;
                jp.ultLevelB = 0;
            }

            jp.Clamp();
            SaveInternal();
        }

        // ----------------------------
        // 궁극기 메타
        // ----------------------------
        public static UltimateSlot GetUltSelectedSlot(JobType job)
        {
            var jp = GetJob(job);
            jp.Clamp();
            return (jp.ultSelectedSlot == 0) ? UltimateSlot.A : UltimateSlot.B;
        }

        public static void SetUltSelectedSlot(JobType job, UltimateSlot slot)
        {
            var jp = GetJob(job);
            jp.ultSelectedSlot = (slot == UltimateSlot.A) ? 0 : 1;
            jp.Clamp();
            SaveInternal();
        }

        public static int GetUltLevelA(JobType job)
        {
            var jp = GetJob(job);
            jp.Clamp();
            return jp.ultLevelA;
        }

        public static int GetUltLevelB(JobType job)
        {
            var jp = GetJob(job);
            jp.Clamp();
            return jp.ultLevelB;
        }

        public static int GetUltSelectedLevel(JobType job)
        {
            var jp = GetJob(job);
            jp.Clamp();
            return (jp.ultSelectedSlot == 0) ? jp.ultLevelA : jp.ultLevelB;
        }

        public static bool TryChangeUltLevel(JobType job, UltimateSlot slot, int delta)
        {
            EnsureInitialized();

            if (delta == 0) return false;

            // +는 남은 포인트 있어야 가능
            if (delta > 0 && GetAvailablePoints() <= 0) return false;

            var jp = GetJob(job);
            jp.Clamp();

            if (delta > 0)
            {
                if (slot == UltimateSlot.A)
                {
                    if (jp.ultLevelA >= 5) return false;
                    jp.ultLevelA += 1;
                }
                else
                {
                    if (jp.ultLevelB >= 5) return false;
                    jp.ultLevelB += 1;
                }
            }
            else
            {
                if (slot == UltimateSlot.A)
                {
                    if (jp.ultLevelA <= 0) return false;
                    jp.ultLevelA -= 1;
                }
                else
                {
                    if (jp.ultLevelB <= 0) return false;
                    jp.ultLevelB -= 1;
                }
            }

            jp.Clamp();
            SaveInternal();
            return true;
        }

        // ✅ 개발용: 저장 초기화(원하면 UI 버튼에 연결)
        public static void DevWipeSave()
        {
            PlayerPrefs.DeleteKey(KEY);
            PlayerPrefs.Save();
            _cache = null;
            _initialized = false;
        }
    }
}
