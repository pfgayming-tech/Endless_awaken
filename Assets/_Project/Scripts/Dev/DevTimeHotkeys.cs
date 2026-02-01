using UnityEngine;

namespace VSL.Dev
{
    public class DevTimeHotkeys : MonoBehaviour
    {
        public float[] presets = { 1f, 2f, 3f, 5f, 8f };

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) VSL.TimeControl.SetTimeScale(presets[0]);
            if (Input.GetKeyDown(KeyCode.Alpha2)) VSL.TimeControl.SetTimeScale(presets[1]);
            if (Input.GetKeyDown(KeyCode.Alpha3)) VSL.TimeControl.SetTimeScale(presets[2]);
            if (Input.GetKeyDown(KeyCode.Alpha4)) VSL.TimeControl.SetTimeScale(presets[3]);
            if (Input.GetKeyDown(KeyCode.Alpha5)) VSL.TimeControl.SetTimeScale(presets[4]);

            if (Input.GetKeyDown(KeyCode.R)) VSL.TimeControl.Reset();
        }
    }
}
