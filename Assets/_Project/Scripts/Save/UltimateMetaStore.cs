using System;
using UnityEngine;

namespace VSL
{
    public enum UltimateSlot { A = 0, B = 1 }

    [Serializable]
    public class UltimateMetaData
    {
        public int points = 0;
        public int selectedSlot = 0; // 0=A, 1=B
        public int levelA = 0;       // 0~5
        public int levelB = 0;       // 0~5
    }

    public static class UltimateMetaStore
    {
        private static string Key(JobType job) => $"VSL_ULT_META_{job}";

        public static UltimateMetaData Get(JobType job)
        {
            string k = Key(job);
            if (!PlayerPrefs.HasKey(k))
            {
                var fresh = new UltimateMetaData();
                Save(job, fresh);
                return fresh;
            }

            string json = PlayerPrefs.GetString(k);
            try { return JsonUtility.FromJson<UltimateMetaData>(json); }
            catch
            {
                var fresh = new UltimateMetaData();
                Save(job, fresh);
                return fresh;
            }
        }

        public static void Save(JobType job, UltimateMetaData data)
        {
            string k = Key(job);
            PlayerPrefs.SetString(k, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        // 클리어 보상 포인트 지급용 (난이도 easy=1점 등 너가 호출)
        public static void AddPoints(JobType job, int amount)
        {
            var d = Get(job);
            d.points += Mathf.Max(0, amount);
            Save(job, d);
        }

        public static int GetSelectedLevel(JobType job)
        {
            var d = Get(job);
            return d.selectedSlot == 0 ? d.levelA : d.levelB;
        }

        public static UltimateSlot GetSelectedSlot(JobType job)
        {
            var d = Get(job);
            return d.selectedSlot == 0 ? UltimateSlot.A : UltimateSlot.B;
        }
    }
}
