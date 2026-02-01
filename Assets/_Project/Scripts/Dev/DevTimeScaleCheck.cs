using UnityEngine;

public class DevTimeScaleCheck : MonoBehaviour
{
    void Update()
    {
        if (Time.frameCount % 30 == 0)
            Debug.Log($"[Time] timeScale={Time.timeScale} dt={Time.deltaTime}");
    }
}
