using UnityEngine;

namespace VSL.VFX
{
    public static class VfxSprites
    {
        private static Sprite _pixel;
        private static Material _spriteMat;

        public static Sprite Pixel
        {
            get
            {
                if (_pixel != null) return _pixel;

                var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();

                tex.filterMode = FilterMode.Point; // 도트 느낌 유지
                tex.wrapMode = TextureWrapMode.Clamp;

                _pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                return _pixel;
            }
        }

        public static Material SpriteMat
        {
            get
            {
                if (_spriteMat != null) return _spriteMat;

                // URP 2D면 Lit 셰이더가 더 예쁘게 나올 수 있음
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
                if (shader == null) shader = Shader.Find("Sprites/Default");

                _spriteMat = new Material(shader);
                return _spriteMat;
            }
        }
    }
}
