using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

public class KinectSensorSource : IDisposable
{
    private const int OPAQUE_ALPHA = unchecked((int)0xFF000000);
    private const uint COLOR_FRAME_LIMIT = 2;

    private readonly bool _seatedMode;
    private readonly bool _colorMirrored;
    private readonly float[] _frameWords = new float[KinectNative.FRAME_WORDS];
    private readonly int[] _frameInts = new int[KinectNative.FRAME_WORDS];
    private readonly Vector3[] _joints = new Vector3[KinectSkeletonMapper.JOINT_COUNT];
    private readonly int[] _jointStates = new int[KinectSkeletonMapper.JOINT_COUNT];

    private IntPtr _skeletonBuffer;
    private IntPtr _colorStream;
    private Task<string> _startup;
    private bool _running;
    private bool _hasColorFrame;
    private uint _trackedId;
    private long _frameCount;
    private string _status = "not started";

    private Texture2D _texture;
    private int[] _colorSource;
    private int[] _colorPixels;
    private KinectNative.LockRectFn _lockRect;
    private KinectNative.UnlockRectFn _unlockRect;
    private IntPtr _textureVtable;

    public KinectSensorSource(bool seatedMode, bool colorMirrored)
    {
        _seatedMode = seatedMode;
        _colorMirrored = colorMirrored;
    }

    public static bool IsSupportedPlatform => Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor;
    public static bool IsRuntimeInstalled => IsSupportedPlatform && KinectNative.IsRuntimeInstalled;

    public bool IsRunning => _running;
    public bool IsStarting => _startup != null;
    public Texture Texture => _hasColorFrame ? _texture : null;
    public long FrameCount => _frameCount;
    public int ImageWidth => KinectNative.COLOR_WIDTH;
    public int ImageHeight => KinectNative.COLOR_HEIGHT;
    public string Diagnostics => $"Kinect v1: {_status}, frames: {_frameCount}, runtime installed: {IsRuntimeInstalled}";

    public void Start()
    {
        Stop();

        if (!IsRuntimeInstalled)
        {
            _status = "Kinect for Windows Runtime 1.8 not installed (Kinect10.dll missing)";
            Debug.LogWarning($"{nameof(KinectSensorSource)}: {_status}");
            return;
        }

        _status = "initializing";
        uint trackingFlags = _seatedMode ? KinectNative.NUI_SKELETON_TRACKING_FLAG_ENABLE_SEATED_SUPPORT : 0u;
        _startup = Task.Run(() => Initialize(trackingFlags));
    }

    public void Stop()
    {
        if (_startup != null)
        {
            _startup.Wait();
            CompleteStartup();
        }

        if (!_running) return;

        _running = false;
        _hasColorFrame = false;
        _colorStream = IntPtr.Zero;
        _trackedId = 0;
        KinectNative.NuiShutdown();
        _status = "stopped";
    }

    // Returns true when the sensor delivered anything this frame; skeletonArrived is set when a skeleton frame
    // was read, and target then holds the tracked player (or no person).
    public bool Poll(float now, PoseFrame target, out bool skeletonArrived)
    {
        skeletonArrived = false;
        if (_startup != null && _startup.IsCompleted) CompleteStartup();
        if (!_running) return false;

        bool colorArrived = PollColor();
        skeletonArrived = PollSkeleton(now, target);
        if (colorArrived || skeletonArrived) _frameCount++;
        return colorArrived || skeletonArrived;
    }

    public void Dispose()
    {
        Stop();
        if (_skeletonBuffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_skeletonBuffer);
            _skeletonBuffer = IntPtr.Zero;
        }

        if (_texture != null)
        {
            UnityEngine.Object.Destroy(_texture);
            _texture = null;
        }
    }

    private string Initialize(uint trackingFlags)
    {
        try
        {
            int hr = KinectNative.NuiInitialize(KinectNative.NUI_INITIALIZE_FLAG_USES_COLOR | KinectNative.NUI_INITIALIZE_FLAG_USES_SKELETON);
            if (hr != KinectNative.S_OK) return $"NuiInitialize failed ({KinectNative.Hex(hr)}): sensor missing, unpowered or used by another app";

            hr = KinectNative.NuiSkeletonTrackingEnable(IntPtr.Zero, trackingFlags);
            if (hr != KinectNative.S_OK)
            {
                KinectNative.NuiShutdown();
                return $"NuiSkeletonTrackingEnable failed ({KinectNative.Hex(hr)})";
            }

            hr = KinectNative.NuiImageStreamOpen(KinectNative.NUI_IMAGE_TYPE_COLOR, KinectNative.NUI_IMAGE_RESOLUTION_640x480, 0, COLOR_FRAME_LIMIT, IntPtr.Zero, out _colorStream);
            if (hr != KinectNative.S_OK) _colorStream = IntPtr.Zero;
            return null;
        }
        catch (Exception exception) when (exception is DllNotFoundException || exception is EntryPointNotFoundException || exception is BadImageFormatException)
        {
            return $"Kinect10.dll unusable ({exception.Message})";
        }
    }

    private void CompleteStartup()
    {
        string error = _startup.Status == TaskStatus.RanToCompletion ? _startup.Result : _startup.Exception?.GetBaseException().Message;
        _startup = null;

        if (error != null)
        {
            _status = error;
            Debug.LogWarning($"{nameof(KinectSensorSource)}: {error}");
            return;
        }

        if (_skeletonBuffer == IntPtr.Zero) _skeletonBuffer = Marshal.AllocHGlobal(KinectNative.FRAME_WORDS * 4);
        EnsureTexture();
        _running = true;
        _status = _colorStream != IntPtr.Zero ? "running" : "running (no color stream)";
        Debug.Log($"{nameof(KinectSensorSource)}: sensor started, seated mode {_seatedMode}, color stream {_colorStream != IntPtr.Zero}");
    }

    private bool PollSkeleton(float now, PoseFrame target)
    {
        if (KinectNative.NuiSkeletonGetNextFrame(0, _skeletonBuffer) != KinectNative.S_OK) return false;

        Marshal.Copy(_skeletonBuffer, _frameWords, 0, _frameWords.Length);
        Marshal.Copy(_skeletonBuffer, _frameInts, 0, _frameInts.Length);

        int skeleton = ChooseSkeleton();
        if (skeleton < 0)
        {
            _trackedId = 0;
            target.Clear(now, ImageWidth, ImageHeight);
            return true;
        }

        int baseWord = KinectNative.FRAME_SKELETONS_OFFSET + skeleton * KinectNative.SKELETON_WORDS;
        _trackedId = (uint)_frameInts[baseWord + KinectNative.SKELETON_TRACKING_ID];

        for (int j = 0; j < KinectSkeletonMapper.JOINT_COUNT; j++)
        {
            int joint = baseWord + KinectNative.SKELETON_JOINTS + j * 4;
            _joints[j] = new Vector3(_frameWords[joint], _frameWords[joint + 1], _frameWords[joint + 2]);
            _jointStates[j] = _frameInts[baseWord + KinectNative.SKELETON_JOINT_STATES + j];
        }

        KinectSkeletonMapper.Map(_joints, _jointStates, now, ImageWidth, ImageHeight, KinectNative.COLOR_FOCAL_LENGTH_640x480, target);
        return true;
    }

    // Sticks to the player already being followed; otherwise picks the tracked skeleton closest to the sensor.
    private int ChooseSkeleton()
    {
        int closest = -1;
        float closestDepth = float.MaxValue;

        for (int s = 0; s < KinectNative.NUI_SKELETON_COUNT; s++)
        {
            int baseWord = KinectNative.FRAME_SKELETONS_OFFSET + s * KinectNative.SKELETON_WORDS;
            if (_frameInts[baseWord + KinectNative.SKELETON_TRACKING_STATE] != KinectNative.NUI_SKELETON_TRACKED) continue;

            if (_trackedId != 0 && (uint)_frameInts[baseWord + KinectNative.SKELETON_TRACKING_ID] == _trackedId) return s;

            float depth = _frameWords[baseWord + KinectNative.SKELETON_POSITION + 2];
            if (depth >= closestDepth) continue;

            closestDepth = depth;
            closest = s;
        }

        return closest;
    }

    private bool PollColor()
    {
        if (_colorStream == IntPtr.Zero) return false;
        if (KinectNative.NuiImageStreamGetNextFrame(_colorStream, 0, out IntPtr frame) != KinectNative.S_OK) return false;

        try
        {
            IntPtr frameTexture = Marshal.ReadIntPtr(frame, KinectNative.FrameTextureOffset);
            return frameTexture != IntPtr.Zero && CopyColor(frameTexture);
        }
        finally
        {
            KinectNative.NuiImageStreamReleaseFrame(_colorStream, frame);
        }
    }

    private bool CopyColor(IntPtr frameTexture)
    {
        BindTextureMethods(frameTexture);

        var locked = new KinectNative.NuiLockedRect();
        if (_lockRect(frameTexture, 0, ref locked, IntPtr.Zero, 0) != KinectNative.S_OK) return false;

        try
        {
            if (locked.Bits == IntPtr.Zero || locked.Pitch != ImageWidth * 4) return false;
            Marshal.Copy(locked.Bits, _colorSource, 0, _colorSource.Length);
        }
        finally
        {
            _unlockRect(frameTexture, 0);
        }

        ConvertColor();
        _texture.SetPixelData(_colorPixels, 0);
        _texture.Apply(false);
        _hasColorFrame = true;
        return true;
    }

    // The sensor delivers top-down BGRX rows with an undefined X byte; Unity wants bottom-up rows and opaque alpha.
    // A mirrored feed reversed as a whole is both flipped vertically and unmirrored, so the preview matches a webcam.
    private void ConvertColor()
    {
        int count = _colorSource.Length;
        if (_colorMirrored)
        {
            for (int i = 0, j = count - 1; i < count; i++, j--) _colorPixels[j] = _colorSource[i] | OPAQUE_ALPHA;
            return;
        }

        int width = ImageWidth;
        for (int row = 0; row < ImageHeight; row++)
        {
            int source = row * width;
            int target = (ImageHeight - 1 - row) * width;
            for (int x = 0; x < width; x++) _colorPixels[target + x] = _colorSource[source + x] | OPAQUE_ALPHA;
        }
    }

    private void BindTextureMethods(IntPtr frameTexture)
    {
        IntPtr vtable = Marshal.ReadIntPtr(frameTexture);
        if (vtable == _textureVtable && _lockRect != null) return;

        _textureVtable = vtable;
        _lockRect = Marshal.GetDelegateForFunctionPointer<KinectNative.LockRectFn>(Marshal.ReadIntPtr(vtable, KinectNative.VTABLE_LOCK_RECT * IntPtr.Size));
        _unlockRect = Marshal.GetDelegateForFunctionPointer<KinectNative.UnlockRectFn>(Marshal.ReadIntPtr(vtable, KinectNative.VTABLE_UNLOCK_RECT * IntPtr.Size));
    }

    private void EnsureTexture()
    {
        if (_texture != null) return;

        _texture = new Texture2D(ImageWidth, ImageHeight, TextureFormat.BGRA32, false)
        {
            name = "KinectColor",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontUnloadUnusedAsset
        };
        _colorSource = new int[ImageWidth * ImageHeight];
        _colorPixels = new int[ImageWidth * ImageHeight];
    }
}
