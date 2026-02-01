using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VSL
{
    public class TranscendPanel : MonoBehaviour
    {
        [Header("Root")]
        public GameObject root;

        [Header("Top")]
        public TMP_Text titleText;
        public TMP_Text pointsText;

        [Header("Common")]
        public Button dmgPlus;
        public Button dmgMinus;

        public Button frPlus;
        public Button frMinus;

        public Button msPlus;
        public Button msMinus;

        [Header("Special A/B")]
        public TMP_Text specialAText;
        public TMP_Text specialBText;

        public Button aPlus;
        public Button aMinus;

        public Button bPlus;
        public Button bMinus;

        [Header("Bottom")]
        public Button resetButton;
        public Button closeButton;

        private JobType _job;

        private void Awake()
        {
            if (root != null) root.SetActive(false);

            if (dmgPlus != null)  dmgPlus.onClick.AddListener(() => Spend(p => p.spentDamage++));
            if (dmgMinus != null) dmgMinus.onClick.AddListener(() => Refund(p => p.spentDamage--, () => SaveService.GetJob(_job).spentDamage));

            if (frPlus != null)   frPlus.onClick.AddListener(() => Spend(p => p.spentFireRate++));
            if (frMinus != null)  frMinus.onClick.AddListener(() => Refund(p => p.spentFireRate--, () => SaveService.GetJob(_job).spentFireRate));

            if (msPlus != null)   msPlus.onClick.AddListener(() => Spend(p => p.spentMoveSpeed++));
            if (msMinus != null)  msMinus.onClick.AddListener(() => Refund(p => p.spentMoveSpeed--, () => SaveService.GetJob(_job).spentMoveSpeed));

            if (aPlus != null)    aPlus.onClick.AddListener(() => Spend(p => p.spentSpecialA++));
            if (aMinus != null)   aMinus.onClick.AddListener(() => Refund(p => p.spentSpecialA--, () => SaveService.GetJob(_job).spentSpecialA));

            if (bPlus != null)    bPlus.onClick.AddListener(() => Spend(p => p.spentSpecialB++));
            if (bMinus != null)   bMinus.onClick.AddListener(() => Refund(p => p.spentSpecialB--, () => SaveService.GetJob(_job).spentSpecialB));

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(() =>
                {
                    SaveService.ResetSpent(_job);
                    Refresh();
                });
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() =>
                {
                    if (root != null) root.SetActive(false);
                });
            }
        }

        public void Open(JobType job)
        {
            _job = job;
            if (root != null) root.SetActive(true);
            Refresh();
        }

        private void Refresh()
        {
            var jp = SaveService.GetJob(_job);
            int available = SaveService.GetAvailablePoints(_job);

            if (titleText != null)
                titleText.text = $"{ToKoreanJob(_job)} 초월";

            if (pointsText != null)
                pointsText.text = $"보유: {jp.totalEarned} / 사용: {jp.TotalSpent()} / 남음: {available}";

            if (specialAText != null)
                specialAText.text = $"{SpecialAName(_job)} ({jp.spentSpecialA})";

            if (specialBText != null)
                specialBText.text = $"{SpecialBName(_job)} ({jp.spentSpecialB})";
        }

        private void Spend(Action<JobProgress> mutate)
        {
            if (SaveService.TrySpend(_job, mutate))
                Refresh();
        }

        private void Refund(Action<JobProgress> mutate, Func<int> getCur)
        {
            int cur = getCur();
            if (cur <= 0) return;

            mutate?.Invoke(SaveService.GetJob(_job));
            SaveService.Save();
            Refresh();
        }

        private string ToKoreanJob(JobType j)
        {
            switch (j)
            {
                case JobType.Knight:       return "기사";
                case JobType.Archer:       return "궁수";
                case JobType.Mage:         return "마법사";
                case JobType.Fighter:      return "격투가";
                case JobType.SpiritMaster: return "정령사";
                default:                   return j.ToString();
            }
        }

        private string SpecialAName(JobType j)
        {
            switch (j)
            {
                case JobType.Knight:       return "특화A: 최대체력 +2/pt";
                case JobType.Archer:       return "특화A: 탄속 +0.6/pt";
                case JobType.Mage:         return "특화A: 추가탄(2pt당 +1)";
                case JobType.Fighter:      return "특화A: 근접범위 +0.08/pt";
                case JobType.SpiritMaster: return "특화A: 정령(2pt당 +1)";
                default:                   return "특화A";
            }
        }

        private string SpecialBName(JobType j)
        {
            switch (j)
            {
                case JobType.Knight:       return "특화B: 근접범위 +0.12/pt";
                case JobType.Archer:       return "특화B: 관통 +1/pt";
                case JobType.Mage:         return "특화B: 폭발반경 +0.2/pt";
                case JobType.Fighter:      return "특화B: 공속 +2%/pt";
                case JobType.SpiritMaster: return "특화B: 정령딜 +10%/pt";
                default:                   return "특화B";
            }
        }
    }
}
