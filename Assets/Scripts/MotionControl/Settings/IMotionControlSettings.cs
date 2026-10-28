using System;

public interface IMotionControlSettings
{
    bool Enabled { get; set; }
    MotionSource Source { get; set; }
    string CameraName { get; set; }
    bool MirrorHorizontal { get; set; }
    event Action Changed;
}
