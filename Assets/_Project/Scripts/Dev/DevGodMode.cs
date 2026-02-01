using UnityEngine;

namespace VSL
{
    public class DevGodMode : MonoBehaviour
    {
        [Header("Target")]
        public Health health; // 비워도 자동으로 자기 Health 사용

        [Header("Toggle")]
        public bool godMode = true;              // 시작부터 무적이면 true
        public KeyCode toggleKey = KeyCode.F1;

        [Header("Options")]
        public bool healToFullOnEnable = true;

        private void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            Apply();
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
            {
                godMode = !godMode;
                Apply();
                Debug.Log($"[DevGodMode] GodMode={(godMode ? "ON" : "OFF")}", this);
            }
#endif
        }

        private void Apply()
        {
            if (health == null) return;

            health.SetInvincible(godMode);

            if (godMode && healToFullOnEnable)
                health.HealToFull();
        }

        [ContextMenu("Toggle God Mode")]
        private void ToggleFromMenu()
        {
            godMode = !godMode;
            Apply();
        }
    }
}
