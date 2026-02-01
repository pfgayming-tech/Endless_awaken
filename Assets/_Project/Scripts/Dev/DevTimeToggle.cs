using UnityEngine;

namespace VSL.Dev
{
    public class DevTimeToggle : MonoBehaviour
    {
        [Header("Toggle Settings")]
        public float fastScale = 4f;   // 토글 ON일 때 배속
        public KeyCode toggleKey = KeyCode.T;

        private const float BaseFixedDelta = 0.02f;
        private bool _fast;

        private void Update()
        {
            if (!Input.GetKeyDown(toggleKey)) return;

            _fast = !_fast;
            Apply(_fast ? fastScale : 1f);
        }

        private void Apply(float scale)
        {
            scale = Mathf.Clamp(scale, 0f, 10f);
            Time.timeScale = scale;
            Time.fixedDeltaTime = BaseFixedDelta * Mathf.Max(0.0001f, scale);
            Debug.Log($"[TimeToggle] timeScale={scale}");
        }
    }
}
