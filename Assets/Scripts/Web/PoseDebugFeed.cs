public static class PoseDebugFeed
{
    public const string CHANNEL_PATH = "/camera";

    public static readonly LiveJsonStore Pose = new LiveJsonStore();
    public static readonly LiveBytesStore Frames = new LiveBytesStore();

    public static int ViewerCount { get; set; }

    public static bool HasViewers => ViewerCount > 0;
}
