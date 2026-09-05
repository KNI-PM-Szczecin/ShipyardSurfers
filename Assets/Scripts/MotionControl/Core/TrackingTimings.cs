using System;

[Serializable]
public class TrackingTimings
{
    public float NoCameraTimeout = 3f;
    public float CameraWarmup = 6f;
    public int ValidFramesToTrack = 3;
    public float LostTimeout = 0.7f;
    public float LostToSearchingTimeout = 3f;
}
