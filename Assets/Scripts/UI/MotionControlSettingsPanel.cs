using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MotionControlSettingsPanel : MonoBehaviour
{
    private const string NO_CAMERAS_OPTION = "brak kamer";
    private const string NO_SERVICE_STATUS = "sterowanie kamera niedostepne";

    [SerializeField] private Toggle _enabledToggle;
    [SerializeField] private TMP_Dropdown _cameraDropdown;
    [SerializeField] private Toggle _mirrorToggle;
    [SerializeField] private TMP_Text _statusLabel;

    private IMotionControlSettings _settings;
    private Observable<TrackingState> _state;
    private string[] _devices = new string[0];

    private void OnEnable()
    {
        MotionControlService service = MotionControlService.Instance;
        if (service == null || service.Settings == null)
        {
            SetStatus(NO_SERVICE_STATUS);
            SetInteractable(false);
            return;
        }

        _settings = service.Settings;
        _state = service.State;

        SetInteractable(true);
        Populate();
        Bind();
        _state.Subscribe(OnStateChanged);
    }

    private void OnDisable()
    {
        Unbind();
        _state?.Unsubscribe(OnStateChanged);
        _settings = null;
        _state = null;
    }

    private void Populate()
    {
        _devices = WebcamSource.DeviceNames();

        var options = new List<string>(_devices.Length > 0 ? _devices : new[] { NO_CAMERAS_OPTION });
        _cameraDropdown.ClearOptions();
        _cameraDropdown.AddOptions(options);
        _cameraDropdown.SetValueWithoutNotify(Mathf.Max(0, System.Array.IndexOf(_devices, _settings.CameraName)));
        _cameraDropdown.interactable = _devices.Length > 0;

        _enabledToggle.SetIsOnWithoutNotify(_settings.Enabled);
        _mirrorToggle.SetIsOnWithoutNotify(_settings.MirrorHorizontal);
    }

    private void Bind()
    {
        _enabledToggle.onValueChanged.AddListener(OnEnabledChanged);
        _cameraDropdown.onValueChanged.AddListener(OnCameraChanged);
        _mirrorToggle.onValueChanged.AddListener(OnMirrorChanged);
    }

    private void Unbind()
    {
        _enabledToggle.onValueChanged.RemoveListener(OnEnabledChanged);
        _cameraDropdown.onValueChanged.RemoveListener(OnCameraChanged);
        _mirrorToggle.onValueChanged.RemoveListener(OnMirrorChanged);
    }

    private void OnEnabledChanged(bool enabled)
    {
        _settings.Enabled = enabled;
    }

    private void OnCameraChanged(int index)
    {
        if (index < 0 || index >= _devices.Length) return;
        _settings.CameraName = _devices[index];
    }

    private void OnMirrorChanged(bool mirror)
    {
        _settings.MirrorHorizontal = mirror;
    }

    private void OnStateChanged(TrackingState state)
    {
        SetStatus(StatusText(state));
    }

    private void SetStatus(string text)
    {
        if (_statusLabel != null) _statusLabel.text = text;
    }

    private void SetInteractable(bool interactable)
    {
        _enabledToggle.interactable = interactable;
        _cameraDropdown.interactable = interactable;
        _mirrorToggle.interactable = interactable;
    }

    private static string StatusText(TrackingState state)
    {
        switch (state)
        {
            case TrackingState.Disabled: return "sterowanie kamera wylaczone";
            case TrackingState.NoCamera: return "brak obrazu z kamery";
            case TrackingState.Searching: return "szukam gracza...";
            case TrackingState.Tracking: return "sledzenie aktywne";
            case TrackingState.Lost: return "zgubiono gracza";
            default: return string.Empty;
        }
    }
}
