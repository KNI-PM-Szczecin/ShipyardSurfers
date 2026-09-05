using System;
using UnityEngine;

public class SquareCropBlitter : IDisposable
{
    private Rect _cropUv = new Rect(0f, 0f, 1f, 1f);

    public RenderTexture Target { get; }

    public SquareCropBlitter(int size)
    {
        Target = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontUnloadUnusedAsset
        };
        Target.Create();
    }

    public void Blit(Texture source, bool verticallyMirrored)
    {
        _cropUv = ComputeCrop(source.width, source.height);

        Vector2 scale = _cropUv.size;
        Vector2 offset = _cropUv.position;
        if (verticallyMirrored)
        {
            offset.y += scale.y;
            scale.y = -scale.y;
        }

        Graphics.Blit(source, Target, scale, offset);
    }

    public Vector2 ModelToCamera(Vector2 modelPixel)
    {
        float u = _cropUv.x + modelPixel.x / Target.width * _cropUv.width;
        float vFromTop = (1f - _cropUv.yMax) + modelPixel.y / Target.height * _cropUv.height;
        return new Vector2(u, vFromTop);
    }

    public Rect ModelToCamera(Rect modelRect)
    {
        Vector2 min = ModelToCamera(modelRect.min);
        Vector2 max = ModelToCamera(modelRect.max);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    public void Dispose()
    {
        if (Target == null) return;
        Target.Release();
        UnityEngine.Object.Destroy(Target);
    }

    private static Rect ComputeCrop(float width, float height)
    {
        if (width >= height)
        {
            float visible = height / width;
            return new Rect((1f - visible) * 0.5f, 0f, visible, 1f);
        }

        float visibleY = width / height;
        return new Rect(0f, (1f - visibleY) * 0.5f, 1f, visibleY);
    }
}
