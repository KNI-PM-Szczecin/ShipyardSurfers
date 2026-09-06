using System;

public interface IMotionControlSettings
{
    bool Enabled { get; set; }
    string CameraName { get; set; }
    bool MirrorHorizontal { get; set; }
    event Action Changed;
}
