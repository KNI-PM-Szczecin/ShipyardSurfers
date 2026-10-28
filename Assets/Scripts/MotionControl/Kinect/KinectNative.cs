using System;
using System.IO;
using System.Runtime.InteropServices;

// Flat C API of the Kinect for Windows Runtime 1.8 (Kinect10.dll, installed into System32 by the SDK/runtime).
// Works with the Kinect for Xbox 360 (model 1414) and Kinect for Windows v1.
public static class KinectNative
{
    private const string DLL = "Kinect10";

    public const uint NUI_INITIALIZE_FLAG_USES_COLOR = 0x00000002;
    public const uint NUI_INITIALIZE_FLAG_USES_SKELETON = 0x00000008;

    public const uint NUI_SKELETON_TRACKING_FLAG_ENABLE_SEATED_SUPPORT = 0x00000004;
    public const uint NUI_SKELETON_TRACKING_FLAG_ENABLE_IN_NEAR_RANGE = 0x00000008;

    public const int NUI_IMAGE_TYPE_COLOR = 1;
    public const int NUI_IMAGE_RESOLUTION_640x480 = 2;
    public const int COLOR_WIDTH = 640;
    public const int COLOR_HEIGHT = 480;
    public const float COLOR_FOCAL_LENGTH_640x480 = 531.15f;

    public const int NUI_SKELETON_COUNT = 6;
    public const int NUI_SKELETON_TRACKED = 2;

    // NUI_SKELETON_FRAME laid out in 4-byte words: LARGE_INTEGER timestamp, frame number, flags,
    // floor clip plane, normal to gravity, then NUI_SKELETON_COUNT NUI_SKELETON_DATA records.
    public const int FRAME_WORDS = FRAME_SKELETONS_OFFSET + NUI_SKELETON_COUNT * SKELETON_WORDS;
    public const int FRAME_SKELETONS_OFFSET = 12;
    public const int SKELETON_WORDS = 109;
    public const int SKELETON_TRACKING_STATE = 0;
    public const int SKELETON_TRACKING_ID = 1;
    public const int SKELETON_POSITION = 4;
    public const int SKELETON_JOINTS = 8;
    public const int SKELETON_JOINT_STATES = SKELETON_JOINTS + KinectSkeletonMapper.JOINT_COUNT * 4;

    public const int S_OK = 0;

    [StructLayout(LayoutKind.Sequential)]
    public struct NuiLockedRect
    {
        public int Pitch;
        public int Size;
        public IntPtr Bits;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate int LockRectFn(IntPtr self, uint level, ref NuiLockedRect lockedRect, IntPtr rect, uint flags);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate int UnlockRectFn(IntPtr self, uint level);

    // INuiFrameTexture vtable: IUnknown (3 slots), BufferLen, Pitch, LockRect, GetLevelDesc, UnlockRect.
    public const int VTABLE_LOCK_RECT = 5;
    public const int VTABLE_UNLOCK_RECT = 7;

    // NUI_IMAGE_FRAME: LARGE_INTEGER, three DWORDs, then the INuiFrameTexture pointer aligned to pointer size.
    public static int FrameTextureOffset => (20 + IntPtr.Size - 1) / IntPtr.Size * IntPtr.Size;

    public static bool IsRuntimeInstalled => File.Exists(Path.Combine(Environment.SystemDirectory, DLL + ".dll"));

    [DllImport(DLL)] public static extern int NuiInitialize(uint flags);
    [DllImport(DLL)] public static extern void NuiShutdown();
    [DllImport(DLL)] public static extern int NuiSkeletonTrackingEnable(IntPtr nextFrameEvent, uint flags);
    [DllImport(DLL)] public static extern int NuiSkeletonGetNextFrame(uint millisecondsToWait, IntPtr skeletonFrame);
    [DllImport(DLL)] public static extern int NuiImageStreamOpen(int imageType, int resolution, uint frameFlags, uint frameLimit, IntPtr nextFrameEvent, out IntPtr streamHandle);
    [DllImport(DLL)] public static extern int NuiImageStreamGetNextFrame(IntPtr stream, uint millisecondsToWait, out IntPtr imageFrame);
    [DllImport(DLL)] public static extern int NuiImageStreamReleaseFrame(IntPtr stream, IntPtr imageFrame);

    public static string Hex(int hresult) => "0x" + hresult.ToString("X8");
}
