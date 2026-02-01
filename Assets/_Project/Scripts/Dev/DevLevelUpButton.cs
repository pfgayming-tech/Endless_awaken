using UnityEngine;
using UnityEngine.UI;

namespace VSL.UI
{
    [RequireComponent(typeof(Button))]
    public class DevLevelUpButton : MonoBehaviour
    {
        public VSL.Experience experience;   // 비워도 자동 탐색
        public int levelUpsPerClick = 1;

        private void Awake()
        {
            if (experience == null)
                experience = FindObjectOfType<VSL.Experience>();

            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            if (experience == null) return;
            experience.DevLevelUp(levelUpsPerClick);
        }
    }
}
