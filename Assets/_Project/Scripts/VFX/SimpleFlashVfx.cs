using UnityEngine;

namespace VSL.VFX
{
    public class SimpleFlashVfx : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private float _t;
        private float _dur;
        private Vector3 _fromScale;
        private Vector3 _toScale;
        private Color _fromColor;

        public static void Spawn(Vector3 pos, float duration, float fromScale, float toScale, int sortingOrder = 50)
        {
            var go = new GameObject("VFX_Flash");
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = VfxSprites.Pixel;
            sr.material = VfxSprites.SpriteMat;
            sr.sortingOrder = sortingOrder;
            sr.color = Color.white;

            var fx = go.AddComponent<SimpleFlashVfx>();
            fx._sr = sr;
            fx._dur = Mathf.Max(0.01f, duration);
            fx._fromScale = Vector3.one * fromScale;
            fx._toScale = Vector3.one * toScale;
            fx._fromColor = sr.color;

            go.transform.localScale = fx._fromScale;
        }

        private void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / _dur);

            transform.localScale = Vector3.Lerp(_fromScale, _toScale, k);

            var c = _fromColor;
            c.a = Mathf.Lerp(1f, 0f, k);
            _sr.color = c;

            if (_t >= _dur) Destroy(gameObject);
        }
    }
}
