using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

public static class SceneCapture
{
    public static void RenderToPng(Camera camera, Vector2Int size, string path)
    {
        RenderTexture previousTarget = camera.targetTexture;
        var renderTexture = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32);
        try
        {
            camera.targetTexture = renderTexture;
            Render(camera, renderTexture);

            RenderTexture.active = renderTexture;
            var texture = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            Debug.Log($"{nameof(SceneCapture)}: wrote {path}");
        }
        finally
        {
            RenderTexture.active = null;
            camera.targetTexture = previousTarget;
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);
        }
    }

    private static void Render(Camera camera, RenderTexture target)
    {
        var request = new RenderPipeline.StandardRequest { destination = target };
        if (RenderPipeline.SupportsRenderRequest(camera, request))
        {
            RenderPipeline.SubmitRenderRequest(camera, request);
            return;
        }

        camera.Render();
    }
}
