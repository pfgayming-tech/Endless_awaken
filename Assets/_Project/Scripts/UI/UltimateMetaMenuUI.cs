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
            // dropdown 채우기
            if (jobDropdown != null)
            {
                jobDropdown.ClearOptions();
                jobDropdown.AddOptions(new System.Collections.Generic.List<string>
                {
                    "Knight","Archer","Mage","Fighter","SpiritMaster"
                });

                jobDropdown.onValueChanged.RemoveAllListeners();
                jobDropdown.onValueChanged.AddListener(OnJobChanged);
            }

            HookButtons();
            Refresh();
        }

        private void HookButtons()
        {
            if (toggleA != null)
                toggleA.onValueChanged.AddListener(v => { if (v) SetSelected(0); });

            if (toggleB != null)
                toggleB.onValueChanged.AddListener(v => { if (v) SetSelected(1); });

            if (aPlus != null) aPlus.onClick.AddListener(() => AddLevel(isA:true, +1));
            if (aMinus != null) aMinus.onClick.AddListener(() => AddLevel(isA:true, -1));

            if (bPlus != null) bPlus.onClick.AddListener(() => AddLevel(isA:false, +1));
            if (bMinus != null) bMinus.onClick.AddListener(() => AddLevel(isA:false, -1));
        }

        private void OnJobChanged(int idx)
        {
            _job = idx switch
            {
                0 => JobType.Knight,
                1 => JobType.Archer,
                2 => JobType.Mage,
                3 => JobType.Fighter,
                _ => JobType.SpiritMaster
            };
            Refresh();
        }

        private void SetSelected(int slot)
        {
            var d = UltimateMetaStore.Get(_job);
            d.selectedSlot = slot;
            UltimateMetaStore.Save(_job, d);
            Refresh();
        }

        private void AddLevel(bool isA, int delta)
        {
            var d = UltimateMetaStore.Get(_job);

            // 포인트가 있어야 + 가능
            if (delta > 0)
            {
                if (d.points <= 0) return;
                if (isA && d.levelA >= 5) return;
                if (!isA && d.levelB >= 5) return;

                d.points -= 1;
                if (isA) d.levelA += 1;
                else d.levelB += 1;
            }
            else
            {
                // - 하면 포인트 환급
                if (isA && d.levelA <= 0) return;
                if (!isA && d.levelB <= 0) return;

                d.points += 1;
                if (isA) d.levelA -= 1;
                else d.levelB -= 1;
            }

            d.levelA = Mathf.Clamp(d.levelA, 0, 5);
            d.levelB = Mathf.Clamp(d.levelB, 0, 5);
            UltimateMetaStore.Save(_job, d);
            Refresh();
        }

        private void Refresh()
        {
            var d = UltimateMetaStore.Get(_job);

            if (pointsText != null) pointsText.text = $"Points: {d.points}";
            if (aLevelText != null) aLevelText.text = $"A Lv {d.levelA}";
            if (bLevelText != null) bLevelText.text = $"B Lv {d.levelB}";

            if (toggleA != null) toggleA.isOn = (d.selectedSlot == 0);
            if (toggleB != null) toggleB.isOn = (d.selectedSlot == 1);
        }
    }
}
