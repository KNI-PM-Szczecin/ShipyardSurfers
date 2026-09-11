using UnityEngine;

public class WebcamSource : ICameraSource
{
    private const int PLACEHOLDER_SIZE = 16;

    private static string[] _cachedDeviceNames;

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
    public long FrameCount => _texture != null ? _texture.updateCount : 0;

    public string Diagnostics => _texture == null
        ? $"no texture (devices: {DeviceNames().Length})"
        : $"'{_texture.deviceName}' playing: {_texture.isPlaying}, {_texture.width}x{_texture.height}, " +
          $"updates: {_texture.updateCount}, didUpdate: {_texture.didUpdateThisFrame}, " +
          $"requested: {_requestedWidth}x{_requestedHeight}@{_requestedFps}, " +
          $"authorized: {Application.HasUserAuthorization(UserAuthorization.WebCam)}, focused: {Application.isFocused}, " +
          $"devices: [{string.Join(" | ", DeviceNames())}]";

    public bool VerticallyMirrored => _texture != null && _texture.videoVerticallyMirrored;
    public string ActiveDeviceName => _texture != null ? _texture.deviceName : null;
    public bool HasTexture => _texture != null;

    public void Resume()
    {
        if (_texture == null || _texture.isPlaying) return;

        _lastUpdateCount = -1;
        _texture.Play();
    }

    public static string[] DeviceNames(bool refresh = false)
    {
        if (_cachedDeviceNames == null || refresh)
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            _cachedDeviceNames = new string[devices.Length];
            for (int i = 0; i < devices.Length; i++) _cachedDeviceNames[i] = devices[i].name;
        }

        return (string[])_cachedDeviceNames.Clone();
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
        string[] devices = DeviceNames();
        if (!string.IsNullOrEmpty(preferred) && System.Array.IndexOf(devices, preferred) < 0)
        {
            devices = DeviceNames(true);
        }

        if (devices.Length == 0) return null;
        return System.Array.IndexOf(devices, preferred) >= 0 ? preferred : devices[0];
    }
}
