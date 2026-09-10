using System;
using System.Globalization;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CalibrationController : MonoBehaviour
{
    private const int ROUNDS = 3;
    private const string FOLDER_NAME = "Calibration";
    private const string FILE_PREFIX = "calibration_";
    private const string FILE_TIMESTAMP = "yyyyMMdd_HHmmss";

    [SerializeField] private Button _backButton;
    [SerializeField] private Button _infoButton;
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _closeInfoButton;
    [SerializeField] private Button _openFolderButton;
    [SerializeField] private Button _doneBackButton;

    [Space]
    [SerializeField] private GameObject _infoPanel;
    [SerializeField] private GameObject _donePanel;
    [SerializeField] private GameObject _promptCard;
    [SerializeField] private GameObject _idleHint;

    [Space]
    [SerializeField] private TMP_Text _phaseLabel;
    [SerializeField] private TMP_Text _gestureLabel;
    [SerializeField] private TMP_Text _timerLabel;
    [SerializeField] private TMP_Text _progressLabel;
    [SerializeField] private TMP_Text _doneLabel;
    [SerializeField] private TMP_Text _pathLabel;
    [SerializeField] private Image _gestureImage;
    [SerializeField] private Toggle _mirrorToggle;

    [Space]
    [SerializeField] private Sprite _laneLeftSprite;
    [SerializeField] private Sprite _laneRightSprite;
    [SerializeField] private Sprite _jumpSprite;
    [SerializeField] private Sprite _rollSprite;

    [Space]
    [SerializeField] private Color _gestureActiveColor = Color.white;
    [SerializeField] private Color _gestureIdleColor = new Color(1f, 1f, 1f, 0.35f);

    [Space]
    [SerializeField] private float _countdownSeconds = 10f;
    [SerializeField] private float _promptSeconds = 3f;
    [SerializeField] private float _restSeconds = 2.5f;

#if UNITY_EDITOR
    [Space]
    public SceneAsset MenuScene;
#endif

    [HideInInspector]
    [SerializeField]
    private string MenuSceneName;

    private CalibrationSession _session;
    private CalibrationCsvLog _log;
    private GestureType? _firedThisFrame;
    private string _savedDirectory;
    private float _lastSampleTime = float.NegativeInfinity;
    private int _sampleIndex;
    private int _recordedStep = -1;
    private bool _demandApplied;
    private bool _mirrorBound;

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (MenuScene != null)
        {
            MenuSceneName = MenuScene.name;
        }
#endif
    }

    private void Awake()
    {
        if (_backButton != null) _backButton.onClick.AddListener(BackToMenu);
        if (_infoButton != null) _infoButton.onClick.AddListener(ShowInfo);
        if (_closeInfoButton != null) _closeInfoButton.onClick.AddListener(HideInfo);
        if (_startButton != null) _startButton.onClick.AddListener(StartSession);
        if (_openFolderButton != null) _openFolderButton.onClick.AddListener(OpenFolder);
        if (_doneBackButton != null) _doneBackButton.onClick.AddListener(BackToMenu);

        EventBus.GestureDetectedEvent += OnGestureDetected;

        if (_infoPanel != null) _infoPanel.SetActive(false);
        if (_donePanel != null) _donePanel.SetActive(false);
        if (_promptCard != null) _promptCard.SetActive(false);
        if (_idleHint != null) _idleHint.SetActive(true);
    }

    private void OnDestroy()
    {
        if (_backButton != null) _backButton.onClick.RemoveListener(BackToMenu);
        if (_infoButton != null) _infoButton.onClick.RemoveListener(ShowInfo);
        if (_closeInfoButton != null) _closeInfoButton.onClick.RemoveListener(HideInfo);
        if (_startButton != null) _startButton.onClick.RemoveListener(StartSession);
        if (_openFolderButton != null) _openFolderButton.onClick.RemoveListener(OpenFolder);
        if (_doneBackButton != null) _doneBackButton.onClick.RemoveListener(BackToMenu);

        EventBus.GestureDetectedEvent -= OnGestureDetected;
        if (_mirrorBound && _mirrorToggle != null) _mirrorToggle.onValueChanged.RemoveListener(OnMirrorChanged);
        ReleaseCamera();
    }

    private void Update()
    {
        RequestCamera();

        if (_session == null) return;

        _session.Tick(Time.unscaledDeltaTime);
        UpdatePromptUi();

        if (_session.IsFinished) FinishSession();
    }

    private void LateUpdate()
    {
        if (_session == null || !_session.IsSampling)
        {
            _firedThisFrame = null;
            return;
        }

        if (_recordedStep != _session.StepIndex)
        {
            _recordedStep = _session.StepIndex;
            _sampleIndex = 0;
        }

        MotionControlService service = MotionControlService.Instance;
        NormalizedPose pose = service != null ? service.Pose : null;

        if (pose != null && pose.IsValid && pose.Timestamp > _lastSampleTime)
        {
            _lastSampleTime = pose.Timestamp;
            _log.AppendSample(_session.Round, _session.StepInRound, _session.CurrentGesture, _session.Phase, _sampleIndex,
                _session.PhaseElapsed, service.State.Value, _firedThisFrame, pose);
            _sampleIndex++;
        }

        _firedThisFrame = null;
    }

    private void RequestCamera()
    {
        if (_demandApplied || MotionControlService.Instance == null) return;

        MotionControlService.Instance.SetExternalDemand(true);
        _demandApplied = true;
        BindMirrorToggle();
    }

    private void BindMirrorToggle()
    {
        if (_mirrorBound || _mirrorToggle == null) return;

        IMotionControlSettings settings = MotionControlService.Instance.Settings;
        if (settings == null) return;

        _mirrorToggle.SetIsOnWithoutNotify(settings.MirrorHorizontal);
        _mirrorToggle.onValueChanged.AddListener(OnMirrorChanged);
        _mirrorBound = true;
    }

    private void OnMirrorChanged(bool mirrored)
    {
        MotionControlService service = MotionControlService.Instance;
        if (service == null || service.Settings == null) return;

        service.Settings.MirrorHorizontal = mirrored;
    }

    private void ReleaseCamera()
    {
        if (!_demandApplied || MotionControlService.Instance == null) return;

        MotionControlService.Instance.SetExternalDemand(false);
        _demandApplied = false;
    }

    private void OnGestureDetected(GestureType gesture) => _firedThisFrame = gesture;

    private void StartSession()
    {
        _session = new CalibrationSession(new CalibrationPlan(ROUNDS, new System.Random()),
            _countdownSeconds, _promptSeconds, _restSeconds);
        _log = new CalibrationCsvLog();
        _lastSampleTime = float.NegativeInfinity;
        _recordedStep = -1;
        _sampleIndex = 0;

        if (_idleHint != null) _idleHint.SetActive(false);
        if (_promptCard != null) _promptCard.SetActive(true);
        if (_startButton != null) _startButton.interactable = false;

        UpdatePromptUi();
    }

    private void UpdatePromptUi()
    {
        if (_timerLabel != null) _timerLabel.text = Mathf.CeilToInt(_session.SecondsLeft).ToString(CultureInfo.InvariantCulture);
        if (_progressLabel != null) _progressLabel.text = $"{_session.StepNumber} / {_session.StepCount}";

        bool showGesture = _session.Phase == CalibrationPhase.Prompt;
        if (_gestureImage != null)
        {
            _gestureImage.sprite = SpriteFor(_session.CurrentGesture);
            _gestureImage.color = showGesture ? _gestureActiveColor : _gestureIdleColor;
        }

        if (_gestureLabel != null) _gestureLabel.text = GestureName(_session.CurrentGesture);
        if (_phaseLabel != null) _phaseLabel.text = PhaseName(_session.Phase);
    }

    private void FinishSession()
    {
        if (_promptCard != null) _promptCard.SetActive(false);
        if (_donePanel != null) _donePanel.SetActive(true);

        int samples = _log.SampleCount;
        _session = null;

        try
        {
            string directory = Path.Combine(Application.persistentDataPath, FOLDER_NAME);
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, $"{FILE_PREFIX}{DateTime.Now.ToString(FILE_TIMESTAMP, CultureInfo.InvariantCulture)}.csv");
            File.WriteAllText(path, _log.ToCsv());

            _savedDirectory = directory;
            if (_doneLabel != null) _doneLabel.text = $"Zapisano plik kalibracji ({samples} próbek).";
            if (_pathLabel != null) _pathLabel.text = path;
        }
        catch (Exception exception)
        {
            _savedDirectory = null;
            if (_doneLabel != null) _doneLabel.text = "Nie udało się zapisać pliku kalibracji.";
            if (_pathLabel != null) _pathLabel.text = exception.Message;
            Debug.LogException(exception, this);
        }

        if (_openFolderButton != null) _openFolderButton.interactable = _savedDirectory != null;
        if (_startButton != null) _startButton.interactable = true;
    }

    private void OpenFolder()
    {
        if (string.IsNullOrEmpty(_savedDirectory)) return;

        Application.OpenURL($"file:///{_savedDirectory.Replace('\\', '/')}");
    }

    private void ShowInfo()
    {
        if (_infoPanel != null) _infoPanel.SetActive(true);
    }

    private void HideInfo()
    {
        if (_infoPanel != null) _infoPanel.SetActive(false);
    }

    private Sprite SpriteFor(GestureType gesture)
    {
        switch (gesture)
        {
            case GestureType.LaneLeft: return _laneLeftSprite;
            case GestureType.LaneRight: return _laneRightSprite;
            case GestureType.Jump: return _jumpSprite;
            case GestureType.Roll: return _rollSprite;
            default: return null;
        }
    }

    public static string GestureName(GestureType gesture)
    {
        switch (gesture)
        {
            case GestureType.LaneLeft: return "W lewo";
            case GestureType.LaneRight: return "W prawo";
            case GestureType.Jump: return "Skok";
            case GestureType.Roll: return "Rolka";
            default: return string.Empty;
        }
    }

    private static string PhaseName(CalibrationPhase phase)
    {
        switch (phase)
        {
            case CalibrationPhase.Countdown: return "Przygotuj się";
            case CalibrationPhase.Prompt: return "Zrób ruch";
            case CalibrationPhase.Rest: return "Wróć do pozy gotowej";
            default: return "Koniec";
        }
    }

    private void BackToMenu()
    {
        if (string.IsNullOrEmpty(MenuSceneName))
        {
            print($"WARNINIG: {nameof(MenuSceneName)} in CalibrationController is null!");
            return;
        }

        SceneManager.LoadScene(MenuSceneName);
    }
}
