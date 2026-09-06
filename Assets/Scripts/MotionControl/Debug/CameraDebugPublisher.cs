using System;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class CameraDebugPublisher : IDisposable
{
    private readonly PoseDebugJsonWriter _jsonWriter = new PoseDebugJsonWriter();
    private readonly RenderTexture _preview;
    private readonly int _jpegQuality;
    private readonly float _frameInterval;
    private readonly bool _flipVertically;

    private float _nextFrameTime;
    private bool _readbackPending;

    public CameraDebugPublisher(int previewWidth, int previewHeight, int jpegQuality, float previewFps, bool flipVertically)
    {
        _preview = new RenderTexture(previewWidth, previewHeight, 0, RenderTextureFormat.ARGB32)
        {
            hideFlags = HideFlags.DontUnloadUnusedAsset
        };
        _preview.Create();
        _jpegQuality = jpegQuality;
        _frameInterval = previewFps > 0f ? 1f / previewFps : 0.1f;
        _flipVertically = flipVertically;
    }

    public void PublishPose(PoseDebugSnapshot snapshot)
    {
        if (!PoseDebugFeed.HasViewers) return;

        PoseDebugFeed.Pose.Set(_jsonWriter.Write(snapshot));
    }

    public void PublishFrame(Texture source, bool verticallyMirrored, float now)
    {
        if (!PoseDebugFeed.HasViewers || source == null || _readbackPending || now < _nextFrameTime) return;

        _nextFrameTime = now + _frameInterval;

        Vector2 scale = new Vector2(1f, verticallyMirrored ? -1f : 1f);
        Vector2 offset = new Vector2(0f, verticallyMirrored ? 1f : 0f);
        Graphics.Blit(source, _preview, scale, offset);

        _readbackPending = true;
        AsyncGPUReadback.Request(_preview, 0, TextureFormat.RGBA32, OnReadback);
    }

    public void Dispose()
    {
        if (_preview == null) return;
        _preview.Release();
        UnityEngine.Object.Destroy(_preview);
    }

    private void OnReadback(AsyncGPUReadbackRequest request)
    {
        _readbackPending = false;
        if (request.hasError || _preview == null) return;

        NativeArray<byte> data = request.GetData<byte>();
        byte[] pixels = data.ToArray();
        int width = _preview.width;
        int height = _preview.height;
        int quality = _jpegQuality;
        bool flip = _flipVertically;

        ThreadPool.QueueUserWorkItem(_ => EncodeAndPublish(pixels, width, height, quality, flip));
    }

    private static void EncodeAndPublish(byte[] pixels, int width, int height, int quality, bool flip)
    {
        try
        {
            if (flip) FlipRows(pixels, width * 4, height);
            byte[] jpeg = ImageConversion.EncodeArrayToJPG(pixels, GraphicsFormat.R8G8B8A8_UNorm, (uint)width, (uint)height, 0, quality);
            PoseDebugFeed.Frames.Set(jpeg);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"{nameof(CameraDebugPublisher)}: JPEG encode failed ({e.Message})");
        }
    }

    private static void FlipRows(byte[] pixels, int rowBytes, int height)
    {
        var row = new byte[rowBytes];
        for (int y = 0; y < height / 2; y++)
        {
            int top = y * rowBytes;
            int bottom = (height - 1 - y) * rowBytes;
            Buffer.BlockCopy(pixels, top, row, 0, rowBytes);
            Buffer.BlockCopy(pixels, bottom, pixels, top, rowBytes);
            Buffer.BlockCopy(row, 0, pixels, bottom, rowBytes);
        }
    }
}
