using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VSL.UI
{
    public class UltimateMetaMenuUI : MonoBehaviour
    {
        [Header("Pick Job")]
        public TMP_Dropdown jobDropdown;

        [Header("Texts")]
        public TMP_Text pointsText;
        public TMP_Text aLevelText;
        public TMP_Text bLevelText;

        [Header("Select")]
        public Toggle toggleA;
        public Toggle toggleB;

        [Header("Buttons")]
        public Button aPlus, aMinus;
        public Button bPlus, bMinus;

        private JobType _job = JobType.Knight;

        private void Start()
        {
            BuildJobDropdown();
            Hook();
            Refresh();
        }

        private void BuildJobDropdown()
        {
            if (jobDropdown == null) return;

            jobDropdown.onValueChanged.RemoveAllListeners();
            jobDropdown.ClearOptions();

            jobDropdown.AddOptions(new List<string>
            {
                "Knight","Archer","Mage","Fighter","SpiritMaster"
            });

            jobDropdown.SetValueWithoutNotify(JobToIndex(_job));
            jobDropdown.RefreshShownValue();

            jobDropdown.onValueChanged.AddListener(i =>
            {
                _job = IndexToJob(i);
                Refresh();
            });
        }

        private void Hook()
        {
            if (toggleA != null)
            {
                toggleA.onValueChanged.RemoveAllListeners();
                toggleA.onValueChanged.AddListener(v =>
                {
                    if (!v) return;
                    SaveService.SetUltSelectedSlot(_job, UltimateSlot.A);
                    Refresh();
                });
            }

            if (toggleB != null)
            {
                toggleB.onValueChanged.RemoveAllListeners();
                toggleB.onValueChanged.AddListener(v =>
                {
                    if (!v) return;
                    SaveService.SetUltSelectedSlot(_job, UltimateSlot.B);
                    Refresh();
                });
            }

            if (aPlus != null)
            {
                aPlus.onClick.RemoveAllListeners();
                aPlus.onClick.AddListener(() =>
                {
                    SaveService.TryChangeUltLevel(_job, UltimateSlot.A, +1);
                    Refresh();
                });
            }

            if (aMinus != null)
            {
                aMinus.onClick.RemoveAllListeners();
                aMinus.onClick.AddListener(() =>
                {
                    SaveService.TryChangeUltLevel(_job, UltimateSlot.A, -1);
                    Refresh();
                });
            }

            if (bPlus != null)
            {
                bPlus.onClick.RemoveAllListeners();
                bPlus.onClick.AddListener(() =>
                {
                    SaveService.TryChangeUltLevel(_job, UltimateSlot.B, +1);
                    Refresh();
                });
            }

            if (bMinus != null)
            {
                bMinus.onClick.RemoveAllListeners();
                bMinus.onClick.AddListener(() =>
                {
                    SaveService.TryChangeUltLevel(_job, UltimateSlot.B, -1);
                    Refresh();
                });
            }
        }

        private void Refresh()
        {
            int pts = SaveService.GetAvailablePoints(); // ✅ 직업 바꿔도 같은 포인트(공용 풀)
            int aLv = SaveService.GetUltLevelA(_job);
            int bLv = SaveService.GetUltLevelB(_job);
            var sel = SaveService.GetUltSelectedSlot(_job);

            if (pointsText != null) pointsText.text = $"Points: {pts}";
            if (aLevelText != null) aLevelText.text = $"A Lv {aLv}";
            if (bLevelText != null) bLevelText.text = $"B Lv {bLv}";

            // 이벤트 재발동 방지
            if (toggleA != null) toggleA.SetIsOnWithoutNotify(sel == UltimateSlot.A);
            if (toggleB != null) toggleB.SetIsOnWithoutNotify(sel == UltimateSlot.B);

            if (aPlus != null) aPlus.interactable = (pts > 0 && aLv < 5);
            if (aMinus != null) aMinus.interactable = (aLv > 0);

            if (bPlus != null) bPlus.interactable = (pts > 0 && bLv < 5);
            if (bMinus != null) bMinus.interactable = (bLv > 0);

            if (jobDropdown != null)
            {
                int idx = JobToIndex(_job);
                if (jobDropdown.value != idx)
                {
                    jobDropdown.SetValueWithoutNotify(idx);
                    jobDropdown.RefreshShownValue();
                }
            }
        }

        private static int JobToIndex(JobType job)
        {
            return job switch
            {
                JobType.Knight => 0,
                JobType.Archer => 1,
                JobType.Mage => 2,
                JobType.Fighter => 3,
                _ => 4
            };
        }

        private static JobType IndexToJob(int idx)
        {
            return idx switch
            {
                0 => JobType.Knight,
                1 => JobType.Archer,
                2 => JobType.Mage,
                3 => JobType.Fighter,
                _ => JobType.SpiritMaster
            };
        }
    }
}
