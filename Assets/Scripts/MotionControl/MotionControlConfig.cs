using System;
using Unity.InferenceEngine;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/MotionControlConfig", order = 3)]
public class MotionControlConfig : ScriptableObject
{
    public const string RESOURCE_PATH = "MotionControl/MotionControlConfig";

    [Header("Model")]
    [SerializeField] private ModelAsset _model;
    [SerializeField] private string _modelResourcePath = "MotionControl/yolo26n-pose-320";
    [SerializeField] private int _inputSize = 320;
    [SerializeField, Tooltip("Wall-clock window the layers of one inference are spread over. Fewer frames fit on a slow machine, so it dispatches the whole model in one frame instead of falling further behind the camera.")]
    private float _dispatchWindowMs = 30f;
    [SerializeField] private BackendType _backend = BackendType.GPUCompute;
    [SerializeField, Tooltip("Measures the configured backend against the other device (GPU vs CPU) on the first inferences and keeps the one with the lower pose latency.")]
    private bool _autoSelectBackend = true;
    [SerializeField, Range(0f, 1f)] private float _minPersonConfidence = 0.5f;

    [Header("Camera")]
    [SerializeField] private int _cameraWidth = 640;
    [SerializeField] private int _cameraHeight = 480;
    [SerializeField] private int _cameraFps = 30;
    [SerializeField, Tooltip("Lowest Application.targetFrameRate cap while the camera pipeline runs (raised to the display refresh rate when that is higher); an uncapped player starves Media Foundation and the webcam stops delivering frames.")]
    private int _frameRateWhileTracking = 60;

    [Header("Kinect v1")]
    [SerializeField, Tooltip("Tracks only the upper body (10 joints), for players standing close to the sensor or partly out of frame.")]
    private bool _kinectSeatedMode;
    [SerializeField, Tooltip("The Kinect v1 color feed is mirrored; the preview un-mirrors it so it lines up with the skeleton overlay. Turn off if the preview comes out mirrored.")]
    private bool _kinectColorMirrored = true;
    [SerializeField, Tooltip("Kinect skeletons are tracked 1-4 m away, so the player's shoulders cover far less of the image than in front of a webcam.")]
    private PoseValidityThresholds _kinectValidity = new PoseValidityThresholds { MinShoulderWidthFraction = 0.04f };

    [Header("Input")]
    [SerializeField] private float _buttonHoldDuration = 0.1f;

    [Header("Pose")]
    [SerializeField] private PoseValidityThresholds _validity = new PoseValidityThresholds();
    [SerializeField] private SmoothingSettings _smoothing = new SmoothingSettings();
    [SerializeField] private GestureThresholds _gestures = new GestureThresholds();
    [SerializeField] private TrackingTimings _tracking = new TrackingTimings();

    [Header("Debug feed")]
    [SerializeField] private int _previewWidth = 320;
    [SerializeField] private int _previewHeight = 240;
    [SerializeField, Range(1, 100)] private int _jpegQuality = 50;
    [SerializeField] private float _previewFps = 10f;
    [SerializeField] private bool _flipPreviewVertically;

    public ModelAsset Model => _model != null ? _model : Resources.Load<ModelAsset>(_modelResourcePath);
    public int InputSize => _inputSize;
    public float DispatchWindowSeconds => Mathf.Max(0f, _dispatchWindowMs) * 0.001f;
    public BackendType Backend => Enum.IsDefined(typeof(BackendType), _backend) ? _backend : BackendType.GPUCompute;
    public bool AutoSelectBackend => _autoSelectBackend;
    public float MinPersonConfidence => _minPersonConfidence;

    public int CameraWidth => _cameraWidth;
    public int CameraHeight => _cameraHeight;
    public int CameraFps => _cameraFps;
    public int FrameRateWhileTracking => _frameRateWhileTracking;

    public bool KinectSeatedMode => _kinectSeatedMode;
    public bool KinectColorMirrored => _kinectColorMirrored;
    public PoseValidityThresholds KinectValidity => _kinectValidity;

    public float ButtonHoldDuration => _buttonHoldDuration;

    public PoseValidityThresholds Validity => _validity;
    public SmoothingSettings Smoothing => _smoothing;
    public GestureThresholds Gestures => _gestures;
    public TrackingTimings Tracking => _tracking;

    public int PreviewWidth => _previewWidth;
    public int PreviewHeight => _previewHeight;
    public int JpegQuality => _jpegQuality;
    public float PreviewFps => _previewFps;
    public bool FlipPreviewVertically => _flipPreviewVertically;

    public static MotionControlConfig LoadDefault()
    {
        return Resources.Load<MotionControlConfig>(RESOURCE_PATH);
    }
}
