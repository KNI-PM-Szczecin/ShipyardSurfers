using UnityEngine;

public class WebcamSource : ICameraSource
{
    private const int PLACEHOLDER_SIZE = 16;

    private readonly int _requestedWidth;
    private readonly int _requestedHeight;
    private readonly int _requestedFps;

    private WebCamTexture _texture;
    private int _lastUpdateCount = -1;

    public WebcamSource(int requestedWidth, int requestedHeight, int requestedFps)
    {
        _requestedWidth = requestedWidth;
        _requestedHeight = requestedHeight;
        _requestedFps = requestedFps;
    }

    public bool IsRunning => _texture != null && _texture.isPlaying;
    public Texture Texture => _texture;
    public bool VerticallyMirrored => _texture != null && _texture.videoVerticallyMirrored;
    public string ActiveDeviceName => _texture != null ? _texture.deviceName : null;
    public bool HasTexture => _texture != null;

    public void Resume()
    {
        if (_texture == null || _texture.isPlaying) return;

        _lastUpdateCount = -1;
        _texture.Play();
    }

    public static string[] DeviceNames()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        var names = new string[devices.Length];
        for (int i = 0; i < devices.Length; i++) names[i] = devices[i].name;
        return names;
    }

    public void Start(string deviceName)
    {
        Stop();

        string resolved = ResolveDeviceName(deviceName);
        if (resolved == null) return;

        _texture = new WebCamTexture(resolved, _requestedWidth, _requestedHeight, _requestedFps);
        _texture.wrapMode = TextureWrapMode.Clamp;
        _texture.hideFlags = HideFlags.DontUnloadUnusedAsset;
        _lastUpdateCount = -1;
        _texture.Play();
    }

    public void Stop()
    {
        if (_texture == null) return;

        _texture.Stop();
        Object.Destroy(_texture);
        _texture = null;
    }

    public bool TryConsumeFrame()
    {
        if (!IsRunning || _texture.width <= PLACEHOLDER_SIZE) return false;

        int updateCount = (int)_texture.updateCount;
        if (updateCount == _lastUpdateCount) return false;

        _lastUpdateCount = updateCount;
        return true;
    }

    public void Dispose() => Stop();

    private static string ResolveDeviceName(string preferred)
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0) return null;

        if (!string.IsNullOrEmpty(preferred))
        {
            foreach (WebCamDevice device in devices)
            {
                if (device.name == preferred) return device.name;
            }
        }

        return devices[0].name;
    }
}
