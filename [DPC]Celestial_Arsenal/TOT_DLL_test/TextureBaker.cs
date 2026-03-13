using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public static class TextureBaker
    {
        public static Texture2D Bake(Texture2D source, Material mat)
        {
            if (source == null || mat == null)
            {
                return source;
            }

            RenderTexture rt = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Default
            );

            RenderTexture oldActive = RenderTexture.active;
            Graphics.Blit(source, rt, mat);
            Texture2D result = new Texture2D(source.width, source.height, TextureFormat.ARGB32, false);
            RenderTexture.active = rt;
            result.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            result.Apply();
            RenderTexture.active = oldActive;
            RenderTexture.ReleaseTemporary(rt);

            return result;
        }
    }
}