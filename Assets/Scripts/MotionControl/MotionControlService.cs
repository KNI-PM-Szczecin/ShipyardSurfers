using UnityEngine;
using UnityEngine.SceneManagement;

public class MotionControlService : MonoBehaviour
{
    private const int HISTORY_CAPACITY = 64;
    private const float INFERENCE_TIMEOUT = 1.5f;
    private const int TIMEOUTS_BEFORE_WORKER_REBUILD = 2;
    private const float CAMERA_RESTART_INTERVAL = 8f;
    private const float DEBUG_HEARTBEAT_INTERVAL = 0.1f;

    public static MotionControlService Instance { get; private set; }

    public Observable<TrackingState> State { get; } = new Observable<TrackingState>(TrackingState.Disabled);
    public IMotionControlSettings Settings { get; private set; }
    public MotionControlConfig Config => _config;

    [SerializeField] private MotionControlConfig _config;

    private WebcamSource _camera;
    private SquareCropBlitter _cropper;
    private PoseModelRunner _runner;
    private YoloPoseDecoder _decoder;
    private PoseNormalizer _normalizer;
    private PoseSmoother _smoother;
    private PoseHistory _history;
    private GestureRecognizer _recognizer;
    private TrackingStateMachine _tracking;
    private PoseGestureInputEmitter _emitter;
    private CameraDebugPublisher _debugPublisher;

    private readonly PoseFrame _frame = new PoseFrame();
    private readonly NormalizedPose _pose = new NormalizedPose();
    private readonly PoseDebugSnapshot _snapshot = new PoseDebugSnapshot();

    private bool _pipelineActive;
    private int _resultsThisSecond;
    private float _fpsWindowStart;
    private float _poseFps;
    private int _inferenceTimeouts;
    private float _nextCameraRestart;
    private float _nextDebugHeartbeat;

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
        Settings.Changed += ApplySettings;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_pipelineActive || !_camera.HasTexture || _camera.IsRunning) return;

        _camera.Resume();
        _tracking.NotifyCameraStarted(Time.unscaledTime);
        Debug.Log($"{nameof(MotionControlService)}: camera resumed after loading scene '{scene.name}'", this);
    }

    private void Start()
    {
        if (Config != null) ApplySettings();
    }

    private void BuildPipeline()
    {
        _camera = new WebcamSource(Config.CameraWidth, Config.CameraHeight, Config.CameraFps);
        _cropper = new SquareCropBlitter(Config.InputSize);
        _decoder = new YoloPoseDecoder(Config.MinPersonConfidence);
        _normalizer = new PoseNormalizer(Config.Validity) { Mirror = Settings.MirrorHorizontal };
        _smoother = new PoseSmoother(Config.Smoothing, KeypointIndex.UpperBody);
        _history = new PoseHistory(HISTORY_CAPACITY);
        _recognizer = new GestureRecognizer(GestureRecognizer.DefaultRules(), Config.Gestures, _history);
        _tracking = new TrackingStateMachine(Config.Tracking);
        _emitter = new PoseGestureInputEmitter(Config.ButtonHoldDuration);
        _debugPublisher = new CameraDebugPublisher(Config.PreviewWidth, Config.PreviewHeight, Config.JpegQuality, Config.PreviewFps, Config.FlipPreviewVertically);
    }

    private void ApplySettings()
    {
        _normalizer.Mirror = Settings.MirrorHorizontal;

        if (!Settings.Enabled)
        {
            StopPipeline();
            return;
        }

        if (!_pipelineActive)
        {
            StartPipeline();
        }
        else if (_camera.ActiveDeviceName != Settings.CameraName && !string.IsNullOrEmpty(Settings.CameraName))
        {
            _camera.Start(Settings.CameraName);
            ResetRecognition();
        }
    }

    private void StartPipeline()
    {
        ModelAssetGuard();
        if (_runner == null) return;

        _camera.Start(Settings.CameraName);
        _emitter.Attach();
        _tracking.Enable(Time.unscaledTime);
        ResetRecognition();
        Application.runInBackground = true;
        _pipelineActive = true;
        _inferenceTimeouts = 0;
        _nextCameraRestart = Time.unscaledTime + CAMERA_RESTART_INTERVAL;
        State.Value = _tracking.State;
        Debug.Log($"{nameof(MotionControlService)}: pipeline started, camera '{_camera.ActiveDeviceName ?? "none"}'", this);
    }

    private void ModelAssetGuard()
    {
        if (_runner != null) return;

        var model = Config.Model;
        if (model == null)
        {
            Debug.LogError($"{nameof(MotionControlService)}: no pose model assigned in {nameof(MotionControlConfig)}", this);
            return;
        }

        _runner = new PoseModelRunner(model, Config.InputSize, Config.LayersPerFrame, Config.Backend);
    }

    private void StopPipeline()
    {
        if (!_pipelineActive) return;

        _camera.Stop();
        _emitter.Detach();
        _tracking.Disable();
        _runner?.Dispose();
        _runner = null;
        ResetRecognition();
        _pipelineActive = false;
        State.Value = TrackingState.Disabled;
        Debug.Log($"{nameof(MotionControlService)}: pipeline stopped", this);
    }

    private void ResetRecognition()
    {
        _recognizer.Reset();
        _smoother.Reset();
        _normalizer.Reset();
    }

    private void Update()
    {
        if (!_pipelineActive) return;

        float now = Time.unscaledTime;
        bool frameArrived = _camera.TryConsumeFrame();

        RecoverStalledInference(now);

        if (frameArrived && _runner.IsIdle)
        {
            _cropper.Blit(_camera.Texture, _camera.VerticallyMirrored);
            _runner.Begin(_cropper.Target, now);
        }

        _runner.Step();

        bool resultArrived = _runner.TryTakeResult(now, out float[] output, out int anchorCount);
        bool poseValid = false;

        if (resultArrived)
        {
            _inferenceTimeouts = 0;
            TrackFps(now);
            _decoder.TryDecode(output, anchorCount, _runner.InputSize, _runner.InputSize, now, _frame);
            poseValid = _normalizer.TryNormalize(_frame, _pose);
            if (poseValid) _smoother.Smooth(_pose);
        }

        TrackingState previous = _tracking.State;
        _tracking.Update(now, frameArrived, resultArrived, poseValid);
        if (_tracking.State != previous)
        {
            if (previous == TrackingState.Tracking) ResetRecognition();
            State.Value = _tracking.State;
            Debug.Log($"{nameof(MotionControlService)}: {previous} -> {_tracking.State} (camera running: {_camera.IsRunning}, inference idle: {_runner.IsIdle})", this);
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
        PublishDebug(resultArrived, poseValid, now);
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

        _runner.Dispose();
        _runner = null;
        ModelAssetGuard();
        _inferenceTimeouts = 0;
        Debug.LogWarning($"{nameof(MotionControlService)}: inference stalled repeatedly, worker rebuilt", this);
    }

    private void RecoverStalledCamera(float now)
    {
        if (now < _nextCameraRestart) return;

        if (_camera.HasTexture && !_camera.IsRunning)
        {
            _nextCameraRestart = now + CAMERA_RESTART_INTERVAL;
            _camera.Resume();
            _tracking.NotifyCameraStarted(now);
            Debug.LogWarning($"{nameof(MotionControlService)}: camera stopped playing, resumed", this);
            return;
        }

        if (_tracking.State != TrackingState.NoCamera) return;

        _nextCameraRestart = now + CAMERA_RESTART_INTERVAL;
        _camera.Start(Settings.CameraName);
        _tracking.NotifyCameraStarted(now);
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

    private void PublishDebug(bool resultArrived, bool poseValid, float now)
    {
        if (!PoseDebugFeed.HasViewers) return;

        _debugPublisher.PublishFrame(_camera.Texture, _camera.VerticallyMirrored, now);
        if (!resultArrived && now < _nextDebugHeartbeat) return;
        _nextDebugHeartbeat = now + DEBUG_HEARTBEAT_INTERVAL;

        _snapshot.State = _tracking.State;
        _snapshot.HasPerson = _frame.HasPerson;
        _snapshot.PersonConfidence = _frame.Confidence;
        _snapshot.PoseValid = poseValid;
        _snapshot.ArmedVertical = _recognizer.IsArmed(GestureChannel.Vertical);
        _snapshot.ArmedHorizontal = _recognizer.IsArmed(GestureChannel.Horizontal);
        _snapshot.Mirror = Settings.MirrorHorizontal;
        _snapshot.PoseFps = _poseFps;
        _snapshot.InferenceMs = _runner.LastInferenceMs;
        _snapshot.Now = now;
        _snapshot.BoundingBox = _frame.HasPerson ? _cropper.ModelToCamera(_frame.BoundingBox) : default;

        for (int i = 0; i < KeypointIndex.COUNT; i++)
        {
            Keypoint keypoint = _frame.Keypoints[i];
            _snapshot.Keypoints[i] = _frame.HasPerson ? _cropper.ModelToCamera(keypoint.Position) : default;
            _snapshot.Confidences[i] = _frame.HasPerson ? keypoint.Confidence : 0f;
        }

        _debugPublisher.PublishPose(_snapshot);
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        if (Settings != null) Settings.Changed -= ApplySettings;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        StopPipeline();
        _camera?.Dispose();
        _cropper?.Dispose();
        _emitter?.Dispose();
        _debugPublisher?.Dispose();
        Instance = null;
    }
}
