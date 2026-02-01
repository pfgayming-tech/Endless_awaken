using UnityEngine;
using VSL.VFX;

public class DamagePopupUITest : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("[UITest] pool exists? " + (DamagePopupUIPool.I != null));

        var cam = Camera.main;
        var pos = Vector3.zero;
        if (cam != null)
        {
            // 카메라 화면 중앙을 월드로 변환(대충 플레이어 근처로 뜸)
            pos = cam.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 10f));
        }

        if (DamagePopupUIPool.I != null)
            DamagePopupUIPool.I.Show(777, pos);
    }
}
