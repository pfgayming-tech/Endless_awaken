using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VSL
{
    public class MainMenuUI : MonoBehaviour
    {
        public TMP_Dropdown jobDropdown;
        public TMP_Dropdown diffDropdown;
        public Button startButton;

        [Header("Transcend")]
        public TranscendPanel transcendPanel;
        public Button openTranscendButton;

        private void Start()
        {
            SetupDropdowns();

            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(() =>
                {
                    // 안전: 드롭다운이 안 붙어있으면 기본값 사용
                    var job = jobDropdown != null ? (JobType)jobDropdown.value : JobType.Knight;
                    var diff = diffDropdown != null ? (DifficultyType)diffDropdown.value : DifficultyType.Normal;

                    // GameManager가 씬에 없으면 생성
                    if (GameManager.Instance == null)
                    {
                        var go = new GameObject("GameManager");
                        go.AddComponent<GameManager>();
                    }

                    GameManager.Instance.StartGame(job, diff);
                });
            }

            if (openTranscendButton != null)
            {
                openTranscendButton.onClick.RemoveAllListeners();
                openTranscendButton.onClick.AddListener(() =>
                {
                    if (transcendPanel == null) return;
                    var job = jobDropdown != null ? (JobType)jobDropdown.value : JobType.Knight;
                    transcendPanel.Open(job);
                });
            }
        }

        private void SetupDropdowns()
        {
            if (jobDropdown != null)
            {
                jobDropdown.ClearOptions();
                jobDropdown.AddOptions(new List<string>
                {
                    "기사", "궁수", "마법사", "격투가", "정령사"
                });
                jobDropdown.value = 0;
                jobDropdown.RefreshShownValue();
            }

            if (diffDropdown != null)
            {
                diffDropdown.ClearOptions();
                diffDropdown.AddOptions(new List<string>
                {
                    "노말",
                    "하드(적 체력 1.25배)"
                });
                diffDropdown.value = 0;
                diffDropdown.RefreshShownValue();
            }
        }
    }
}
