using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MotionControlService : MonoBehaviour
{
    private const int HISTORY_CAPACITY = 64;
    private const float INFERENCE_TIMEOUT = 1.5f;
    private const int TIMEOUTS_BEFORE_WORKER_REBUILD = 2;
    private const float CAMERA_RESTART_INTERVAL = 8f;
    private const float DEBUG_HEARTBEAT_INTERVAL = 0.1f;
    private const float CAMERA_REPORT_INTERVAL = 2f;
    private const int BACKEND_WARMUP_SAMPLES = 4;
    private const int BACKEND_TRIAL_SAMPLES = 12;
    private const float BACKEND_REQUIRED_GAIN = 0.2f;
    private const int MAX_RUNNER_ATTEMPTS = 3;

    public static MotionControlService Instance { get; private set; }

    public Observable<TrackingState> State { get; } = new Observable<TrackingState>(TrackingState.Disabled);
    public IMotionControlSettings Settings { get; private set; }
    public MotionControlConfig Config => _config;
    public bool IsApplyingSettings => _applyRoutine != null;
    public MotionSource ActiveSource => _activeSource;
    public Texture CameraTexture => UsingKinect ? _kinect.Texture : _camera != null ? _camera.Texture : null;
    public bool CameraVerticallyMirrored => !UsingKinect && _camera != null && _camera.VerticallyMirrored;
    public NormalizedPose Pose => _pose;
    public PoseDebugSnapshot Snapshot => _snapshot;

    [SerializeField] private MotionControlConfig _config;

    private WebcamSource _camera;
    private KinectSensorSource _kinect;
    private SquareCropBlitter _cropper;
    private PoseModelRunner _runner;
    private Model _model;
    private DispatchPlanner _planner;
    private BackendSelector<BackendType> _selector;
    private YoloPoseDecoder _decoder;
    private PoseNormalizer _normalizer;
    private PoseNormalizer _kinectNormalizer;
    private PoseSmoother _smoother;
    private PoseHistory _history;
    private GestureRecognizer _recognizer;
    private TrackingStateMachine _tracking;
    private PoseGestureInputEmitter _emitter;
    private CameraDebugPublisher _debugPublisher;

    private readonly PoseFrame _frame = new PoseFrame();
    private readonly NormalizedPose _pose = new NormalizedPose();
    private readonly PoseDebugSnapshot _snapshot = new PoseDebugSnapshot();

    private Coroutine _applyRoutine;
    private bool _pipelineActive;
    private MotionSource _activeSource;
    private int _resultsThisSecond;
    private float _fpsWindowStart;
    private float _poseFps;
    private int _inferenceTimeouts;
    private float _nextCameraRestart;
    private float _nextDebugHeartbeat;
    private float _nextCameraReport;
    private long _lastReportedFrameCount = -1;
    private long _lastInferredFrame = -1;
    private float _lastInferenceMs;
    private int _lastDispatchFrames;
    private bool _selectionLogged;
    private bool _externalDemand;
    private int _frameRateBeforePipeline;

    private bool UsingKinect => _activeSource == MotionSource.Kinect && _kinect != null;
    private PoseNormalizer ActiveNormalizer => UsingKinect ? _kinectNormalizer : _normalizer;

    public bool IsPipelineWanted => (Settings != null && Settings.Enabled) || _externalDemand;

    public void SetExternalDemand(bool demanded)
    {
        if (_externalDemand == demanded) return;

        _externalDemand = demanded;
        ScheduleApplySettings();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoStart()
    {
        if (Instance != null) return;

        var go = new GameObject(nameof(MotionControlService));
        go.AddComponent<MotionControlService>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _config = MotionControlConfig.LoadDefault();
        if (_config == null)
        {
            Debug.LogError($"{nameof(MotionControlService)}: missing config at Resources/{MotionControlConfig.RESOURCE_PATH}", this);
            enabled = false;
            return;
        }

        Settings = new PlayerPrefsMotionControlSettings();
        BuildPipeline();
        Settings.Changed += ScheduleApplySettings;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_pipelineActive || UsingKinect || !_camera.HasTexture || _camera.IsRunning) return;

        _camera.Resume();
        OnCameraStarted(Time.unscaledTime);
        Debug.Log($"{nameof(MotionControlService)}: camera resumed after loading scene '{scene.name}'", this);
    }

    private void Start()
    {
        if (Config != null) ScheduleApplySettings();
    }

    private void BuildPipeline()
    {
        _camera = new WebcamSource(Config.CameraWidth, Config.CameraHeight, Config.CameraFps);
        _kinect = new KinectSensorSource(Config.KinectSeatedMode, Config.KinectColorMirrored);
        _cropper = new SquareCropBlitter(Config.InputSize);
        _planner = new DispatchPlanner(Config.DispatchWindowSeconds);
        _decoder = new YoloPoseDecoder(Config.MinPersonConfidence);
        _normalizer = new PoseNormalizer(Config.Validity);
        _kinectNormalizer = new PoseNormalizer(Config.KinectValidity);
        _smoother = new PoseSmoother(Config.Smoothing, KeypointIndex.UpperBody);
        _history = new PoseHistory(HISTORY_CAPACITY);
        _recognizer = new GestureRecognizer(GestureRecognizer.DefaultRules(), Config.Gestures, _history);
        _tracking = new TrackingStateMachine(Config.Tracking);
        _emitter = new PoseGestureInputEmitter(Config.ButtonHoldDuration);
        _debugPublisher = new CameraDebugPublisher(Config.PreviewWidth, Config.PreviewHeight, Config.JpegQuality, Config.PreviewFps, Config.FlipPreviewVertically);
    }

    private void ScheduleApplySettings()
    {
        if (_applyRoutine != null) StopCoroutine(_applyRoutine);
        _applyRoutine = StartCoroutine(ApplySettingsRoutine());
    }

    private IEnumerator ApplySettingsRoutine()
    {
        if (!IsPipelineWanted)
        {
            StopPipeline();
            _applyRoutine = null;
            yield break;
        }

        yield return null;

        if (_pipelineActive && _activeSource != Settings.Source)
        {
            StopPipeline();
            yield return null;
        }

        if (!_pipelineActive && Settings.Source == MotionSource.Kinect)
        {
            StartPipeline();
        }
        else if (!_pipelineActive)
        {
            yield return EnsureRunner();
            if (_runner != null)
            {
                yield return null;
                if (!IsPipelineWanted)
                {
                    _applyRoutine = null;
                    yield break;
                }

                StartPipeline();
            }
        }
        else if (!UsingKinect && _camera.ActiveDeviceName != Settings.CameraName && !string.IsNullOrEmpty(Settings.CameraName))
        {
            _camera.Stop();
            OnCameraStarted(Time.unscaledTime);
            yield return null;
            _camera.Start(Settings.CameraName);
            ResetRecognition();
            Debug.Log($"{nameof(MotionControlService)}: camera switched to '{_camera.ActiveDeviceName ?? "none"}'", this);
        }

        _applyRoutine = null;
    }

    private IEnumerator EnsureRunner()
    {
        if (_runner != null) yield break;

        if (_model == null)
        {
            ModelAsset asset = Config.Model;
            if (asset == null)
            {
                Debug.LogError($"{nameof(MotionControlService)}: no pose model assigned in {nameof(MotionControlConfig)}", this);
                yield break;
            }

            Task<Model> load = PoseModelLoader.LoadAsync(asset);
            while (!load.IsCompleted) yield return null;

            if (load.Status == TaskStatus.RanToCompletion && load.Result != null)
            {
                _model = load.Result;
            }
            else
            {
                Debug.LogWarning($"{nameof(MotionControlService)}: background model load failed, loading on main thread ({load.Exception?.GetBaseException().Message})", this);
                _model = ModelLoader.Load(asset);
            }
        }

        if (_selector == null) _selector = CreateSelector();
        CreateRunner();
    }

    private BackendSelector<BackendType> CreateSelector()
    {
        BackendType preferred = SupportedBackend(Config.Backend);
        BackendType[] candidates = Config.AutoSelectBackend
            ? new[] { preferred, AlternateOf(preferred) }
            : new[] { preferred };
        _selectionLogged = candidates.Length == 1;
        return new BackendSelector<BackendType>(candidates, BACKEND_WARMUP_SAMPLES, BACKEND_TRIAL_SAMPLES, BACKEND_REQUIRED_GAIN);
    }

    private static BackendType SupportedBackend(BackendType backend)
    {
        if (backend == BackendType.GPUCompute && !SystemInfo.supportsComputeShaders) return BackendType.GPUPixel;
        return backend;
    }

    private static BackendType AlternateOf(BackendType backend)
    {
        if (backend != BackendType.CPU) return BackendType.CPU;
        return SupportedBackend(BackendType.GPUCompute);
    }

    private void CreateRunner()
    {
        for (int attempt = 0; attempt < MAX_RUNNER_ATTEMPTS; attempt++)
        {
            BackendType backend = _selector.Current;
            try
            {
                _runner = new PoseModelRunner(_model, Config.InputSize, backend, _planner);
                return;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"{nameof(MotionControlService)}: backend {backend} unavailable ({exception.Message})", this);
                if (!_selector.ReportFailure()) break;
            }
        }

        Debug.LogError($"{nameof(MotionControlService)}: no usable inference backend", this);
    }

    private void RebuildRunner()
    {
        _runner?.Dispose();
        _runner = null;
        if (_model != null) CreateRunner();
    }

    private void StartPipeline()
    {
        _activeSource = Settings.Source;
        if (UsingKinect) _kinect.Start();
        else _camera.Start(Settings.CameraName);
        _emitter.Attach();
        _tracking.Enable(Time.unscaledTime);
        _lastInferredFrame = -1;
        ResetRecognition();
        Application.runInBackground = true;
        _frameRateBeforePipeline = Application.targetFrameRate;
        int frameRate = TrackingFrameRate();
        Application.targetFrameRate = frameRate;
        _lastReportedFrameCount = -1;
        _pipelineActive = true;
        _inferenceTimeouts = 0;
        _nextCameraRestart = Time.unscaledTime + CAMERA_RESTART_INTERVAL;
        State.Value = _tracking.State;
        if (UsingKinect)
        {
            Debug.Log($"{nameof(MotionControlService)}: pipeline started with Kinect v1 skeleton tracking, frame rate capped at {frameRate}", this);
            return;
        }

        Debug.Log($"{nameof(MotionControlService)}: pipeline started, camera '{_camera.ActiveDeviceName ?? "none"}', " +
                  $"frame rate capped at {frameRate}, backend {_runner.Backend} ({_runner.LayerCount} layers, " +
                  $"dispatch window {Config.DispatchWindowSeconds * 1000f:0} ms, auto-select {Config.AutoSelectBackend})", this);
    }

    private int TrackingFrameRate()
    {
        int displayRefreshRate = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
        return Mathf.Max(Config.FrameRateWhileTracking, displayRefreshRate);
    }

    private void StopPipeline()
    {
        if (!_pipelineActive) return;

        _camera.Stop();
        _kinect.Stop();
        _emitter.Detach();
        _tracking.Disable();
        _runner?.Dispose();
        _runner = null;
        ResetRecognition();
        Application.targetFrameRate = _frameRateBeforePipeline;
        _pipelineActive = false;
        State.Value = TrackingState.Disabled;
        Debug.Log($"{nameof(MotionControlService)}: pipeline stopped", this);
    }

    private void ResetRecognition()
    {
        _recognizer.Reset();
        _smoother.Reset();
        _normalizer.Reset();
        _kinectNormalizer.Reset();
    }

    private void OnCameraStarted(float now)
    {
        _lastInferredFrame = -1;
        _tracking.NotifyCameraStarted(now);
    }

    private void Update()
    {
        if (!_pipelineActive) return;

        float now = Time.unscaledTime;
        bool frameArrived;
        bool resultArrived;

        if (UsingKinect)
        {
            frameArrived = _kinect.Poll(now, _frame, out resultArrived);
        }
        else
        {
            if (_runner == null) return;
            frameArrived = StepInference(now, out resultArrived);
        }

        bool poseValid = false;
        if (resultArrived)
        {
            TrackFps(now);
            poseValid = ActiveNormalizer.TryNormalize(_frame, _pose);
            if (poseValid) _smoother.Smooth(_pose);
        }

        TrackingState previous = _tracking.State;
        _tracking.Update(now, frameArrived, resultArrived, poseValid);
        if (_tracking.State != previous)
        {
            if (previous == TrackingState.Tracking) ResetRecognition();
            State.Value = _tracking.State;
            Debug.Log(UsingKinect
                ? $"{nameof(MotionControlService)}: t={now:0.00} {previous} -> {_tracking.State} ({_kinect.Diagnostics})"
                : $"{nameof(MotionControlService)}: t={now:0.00} {previous} -> {_tracking.State} (camera running: {_camera.IsRunning}, inference idle: {_runner == null || _runner.IsIdle})", this);
        }

        RecoverStalledCamera(now);

        if (resultArrived && poseValid && _tracking.State == TrackingState.Tracking && _recognizer.TryRecognize(_pose, out GestureType gesture))
        {
            _emitter.Press(gesture, now);
            EventBus.GestureDetected(gesture);
            _snapshot.LastGesture = gesture.ToString();
            _snapshot.LastGestureTime = now;
        }

        _emitter.Update(now);
        ReportCamera(now);
        if (resultArrived) UpdateSnapshot(poseValid, now);
        PublishDebug(resultArrived, now);
    }

    private bool StepInference(float now, out bool resultArrived)
    {
        bool frameArrived = _camera.TryConsumeFrame();
        _planner.ReportFrameTime(Time.unscaledDeltaTime);

        RecoverStalledInference(now);
        _runner.Step();

        resultArrived = _runner.TryTakeResult(now, out float[] output, out int anchorCount);
        if (resultArrived)
        {
            _inferenceTimeouts = 0;
            _lastInferenceMs = _runner.LastInferenceMs;
            _lastDispatchFrames = _runner.LastDispatchFrames;
            _decoder.TryDecode(output, anchorCount, _runner.InputSize, _runner.InputSize, now, _frame);
            ApplyBackendTrial(_lastInferenceMs);
        }

        StartInferenceIfReady(now);
        return frameArrived;
    }

    private void StartInferenceIfReady(float now)
    {
        if (_runner == null || !_runner.IsIdle || !_camera.HasFrameNewerThan(_lastInferredFrame)) return;

        _lastInferredFrame = _camera.FrameCount;
        _cropper.Blit(_camera.Texture, _camera.VerticallyMirrored);
        _runner.Begin(_cropper.Target, now);
    }

    private void ApplyBackendTrial(float latencyMs)
    {
        if (_selector.IsDecided && _selectionLogged) return;

        bool changed = _selector.ReportLatency(latencyMs);
        if (changed) RebuildRunner();

        if (_selector.IsDecided)
        {
            _selectionLogged = true;
            Debug.Log($"{nameof(MotionControlService)}: inference backend chosen by measured pose latency: {_selector.Summary()}", this);
        }
        else if (changed)
        {
            Debug.Log($"{nameof(MotionControlService)}: measuring inference backend {_selector.Current}", this);
        }
    }

    private void ReportCamera(float now)
    {
        if (_tracking.State == TrackingState.Tracking || now < _nextCameraReport) return;

        _nextCameraReport = now + CAMERA_REPORT_INTERVAL;
        long frames = UsingKinect ? _kinect.FrameCount : _camera.FrameCount;
        bool stalled = frames == _lastReportedFrameCount;
        _lastReportedFrameCount = frames;
        if (!stalled) return;

        Debug.LogWarning($"{nameof(MotionControlService)}: t={now:0.00} camera delivered no new frame for {CAMERA_REPORT_INTERVAL:0} s: " +
                         $"{(UsingKinect ? _kinect.Diagnostics : _camera.Diagnostics)}, state {_tracking.State}", this);
    }

    private void RecoverStalledInference(float now)
    {
        if (_runner.IsIdle || now - _runner.StartedAt < INFERENCE_TIMEOUT) return;

        _inferenceTimeouts++;
        _runner.Abort();

        if (_inferenceTimeouts < TIMEOUTS_BEFORE_WORKER_REBUILD)
        {
            Debug.LogWarning($"{nameof(MotionControlService)}: inference stalled, request aborted", this);
            return;
        }

        BackendType stalled = _runner.Backend;
        bool backendDropped = !_selector.IsDecided && _selector.ReportFailure();
        RebuildRunner();
        _inferenceTimeouts = 0;
        Debug.LogWarning(backendDropped
            ? $"{nameof(MotionControlService)}: inference stalled repeatedly on {stalled}, backend dropped from the trial, now {_selector.Current}"
            : $"{nameof(MotionControlService)}: inference stalled repeatedly, worker rebuilt", this);
    }

    private void RecoverStalledCamera(float now)
    {
        if (now < _nextCameraRestart || IsApplyingSettings) return;

        if (UsingKinect)
        {
            if (_tracking.State != TrackingState.NoCamera || _kinect.IsStarting) return;

            _nextCameraRestart = now + CAMERA_RESTART_INTERVAL;
            _kinect.Start();
            OnCameraStarted(now);
            Debug.LogWarning($"{nameof(MotionControlService)}: no Kinect frames, sensor restarted", this);
            return;
        }

        if (_camera.HasTexture && !_camera.IsRunning)
        {
            _nextCameraRestart = now + CAMERA_RESTART_INTERVAL;
            _camera.Resume();
            OnCameraStarted(now);
            Debug.LogWarning($"{nameof(MotionControlService)}: camera stopped playing, resumed", this);
            return;
        }

        if (_tracking.State != TrackingState.NoCamera) return;

        _nextCameraRestart = now + CAMERA_RESTART_INTERVAL;
        _camera.Start(Settings.CameraName);
        OnCameraStarted(now);
        Debug.LogWarning($"{nameof(MotionControlService)}: no camera frames, camera restarted ('{_camera.ActiveDeviceName ?? "none"}')", this);
    }

    private void TrackFps(float now)
    {
        _resultsThisSecond++;
        if (now - _fpsWindowStart < 1f) return;

        _poseFps = _resultsThisSecond / (now - _fpsWindowStart);
        _resultsThisSecond = 0;
        _fpsWindowStart = now;
    }

    private void PublishDebug(bool resultArrived, float now)
    {
        if (!PoseDebugFeed.HasViewers) return;

        _debugPublisher.PublishFrame(CameraTexture, CameraVerticallyMirrored, now);
        if (!resultArrived && now < _nextDebugHeartbeat) return;
        _nextDebugHeartbeat = now + DEBUG_HEARTBEAT_INTERVAL;

        _debugPublisher.PublishPose(_snapshot);
    }

    private void UpdateSnapshot(bool poseValid, float now)
    {
        _snapshot.State = _tracking.State;
        _snapshot.HasPerson = _frame.HasPerson;
        _snapshot.PersonConfidence = _frame.Confidence;
        _snapshot.PoseValid = poseValid;
        _snapshot.ArmedVertical = _recognizer.IsArmed(GestureChannel.Vertical);
        _snapshot.ArmedHorizontal = _recognizer.IsArmed(GestureChannel.Horizontal);
        _snapshot.Mirror = Settings.MirrorHorizontal;
        _snapshot.PoseFps = _poseFps;
        _snapshot.InferenceMs = _lastInferenceMs;
        _snapshot.DispatchFrames = _lastDispatchFrames;
        _snapshot.Backend = UsingKinect ? "Kinect" : _runner != null ? _runner.Backend.ToString() : "none";
        _snapshot.Now = now;
        _snapshot.BoundingBox = _frame.HasPerson ? FrameToCamera(_frame.BoundingBox) : default;

        for (int i = 0; i < KeypointIndex.COUNT; i++)
        {
            Keypoint keypoint = _frame.Keypoints[i];
            _snapshot.Keypoints[i] = _frame.HasPerson ? FrameToCamera(keypoint.Position) : default;
            _snapshot.Confidences[i] = _frame.HasPerson ? keypoint.Confidence : 0f;
        }
    }

    private Vector2 FrameToCamera(Vector2 framePixel)
    {
        if (!UsingKinect) return _cropper.ModelToCamera(framePixel);
        return new Vector2(framePixel.x / _frame.ImageWidth, framePixel.y / _frame.ImageHeight);
    }

    private Rect FrameToCamera(Rect frameRect)
    {
        if (!UsingKinect) return _cropper.ModelToCamera(frameRect);
        return Rect.MinMaxRect(frameRect.xMin / _frame.ImageWidth, frameRect.yMin / _frame.ImageHeight,
            frameRect.xMax / _frame.ImageWidth, frameRect.yMax / _frame.ImageHeight);
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        if (Settings != null) Settings.Changed -= ScheduleApplySettings;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        StopPipeline();
        _camera?.Dispose();
        _kinect?.Dispose();
        _cropper?.Dispose();
        _emitter?.Dispose();
        _debugPublisher?.Dispose();
        Instance = null;
    }
}
