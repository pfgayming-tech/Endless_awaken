using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class FitTiledBackgroundToCamera : MonoBehaviour
{
    public Camera cam;
    public float margin = 2f;

    SpriteRenderer sr;

    void OnEnable()
    {
        sr = GetComponent<SpriteRenderer>();
        if (cam == null) cam = Camera.main;
        sr.drawMode = SpriteDrawMode.Tiled;
        UpdateBG();
    }

    void LateUpdate() => UpdateBG();

    void UpdateBG()
    {
        if (cam == null || !cam.orthographic) return;

        float h = cam.orthographicSize * 2f;
        float w = h * cam.aspect;

        sr.size = new Vector2(w + margin, h + margin);

        var p = cam.transform.position;
        transform.position = new Vector3(p.x, p.y, 0f);
    }
}
