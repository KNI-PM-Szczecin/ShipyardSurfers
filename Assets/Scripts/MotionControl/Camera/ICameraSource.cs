using System;
using UnityEngine;

public interface ICameraSource : IDisposable
{
    bool IsRunning { get; }
    Texture Texture { get; }
    bool VerticallyMirrored { get; }
    string ActiveDeviceName { get; }
    bool HasTexture { get; }
    long FrameCount { get; }
    void Start(string deviceName);
    void Resume();
    void Stop();
    bool TryConsumeFrame();
    bool HasFrameNewerThan(long frameCount);
}
