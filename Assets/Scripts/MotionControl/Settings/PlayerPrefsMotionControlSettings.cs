using System;
using UnityEngine;

public class PlayerPrefsMotionControlSettings : IMotionControlSettings
{
    private const string ENABLED_KEY = "MotionControl.Enabled";
    private const string CAMERA_NAME_KEY = "MotionControl.CameraName";
    private const string MIRROR_KEY = "MotionControl.MirrorHorizontal";

    public event Action Changed;

    public bool Enabled
    {
        get => PlayerPrefs.GetInt(ENABLED_KEY, 0) == 1;
        set => SetInt(ENABLED_KEY, value ? 1 : 0);
    }

    public string CameraName
    {
        get => PlayerPrefs.GetString(CAMERA_NAME_KEY, string.Empty);
        set
        {
            string current = CameraName;
            if (current == value) return;
            PlayerPrefs.SetString(CAMERA_NAME_KEY, value ?? string.Empty);
            Commit();
        }
    }

    public bool MirrorHorizontal
    {
        get => PlayerPrefs.GetInt(MIRROR_KEY, 1) == 1;
        set => SetInt(MIRROR_KEY, value ? 1 : 0);
    }

    private void SetInt(string key, int value)
    {
        if (PlayerPrefs.HasKey(key) && PlayerPrefs.GetInt(key) == value) return;
        PlayerPrefs.SetInt(key, value);
        Commit();
    }

    private void Commit()
    {
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
