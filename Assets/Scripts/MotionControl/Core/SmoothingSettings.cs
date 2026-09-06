using System;

[Serializable]
public class SmoothingSettings
{
    public float MinCutoff = 1.5f;
    public float Beta = 0.05f;
    public float DerivativeCutoff = 1f;
}
