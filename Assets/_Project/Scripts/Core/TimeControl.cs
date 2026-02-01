using UnityEngine;

namespace VSL
{
    public static class TimeControl
    {
        private const float BaseFixedDelta = 0.02f;

        public static void SetTimeScale(float scale)
        {
            scale = Mathf.Clamp(scale, 0f, 10f);
            Time.timeScale = scale;
            Time.fixedDeltaTime = BaseFixedDelta * Mathf.Max(0.0001f, scale);
        }

        public static void Reset()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = BaseFixedDelta;
        }
    }
}
