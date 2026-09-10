using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PoseCameraView : MonoBehaviour
{
    private static readonly int[] Bones =
    {
        KeypointIndex.NOSE, KeypointIndex.LEFT_SHOULDER,
        KeypointIndex.NOSE, KeypointIndex.RIGHT_SHOULDER,
        KeypointIndex.LEFT_SHOULDER, KeypointIndex.RIGHT_SHOULDER,
        KeypointIndex.LEFT_SHOULDER, KeypointIndex.LEFT_ELBOW,
        KeypointIndex.LEFT_ELBOW, KeypointIndex.LEFT_WRIST,
        KeypointIndex.RIGHT_SHOULDER, KeypointIndex.RIGHT_ELBOW,
        KeypointIndex.RIGHT_ELBOW, KeypointIndex.RIGHT_WRIST,
        KeypointIndex.LEFT_SHOULDER, KeypointIndex.LEFT_HIP,
        KeypointIndex.RIGHT_SHOULDER, KeypointIndex.RIGHT_HIP,
        KeypointIndex.LEFT_HIP, KeypointIndex.RIGHT_HIP,
        KeypointIndex.LEFT_HIP, KeypointIndex.LEFT_KNEE,
        KeypointIndex.LEFT_KNEE, KeypointIndex.LEFT_ANKLE,
        KeypointIndex.RIGHT_HIP, KeypointIndex.RIGHT_KNEE,
        KeypointIndex.RIGHT_KNEE, KeypointIndex.RIGHT_ANKLE
    };

    private const int MIN_TEXTURE_SIZE = 16;

    [SerializeField] private RawImage _image;
    [SerializeField] private AspectRatioFitter _fitter;
    [SerializeField] private RectTransform _overlay;
    [SerializeField] private GameObject _offlineHint;
    [SerializeField] private TMP_Text _statusLabel;

    [Space]
    [SerializeField] private Sprite _jointSprite;
    [SerializeField] private Sprite _boneSprite;
    [SerializeField] private Color _skeletonColor = new Color(0f, 0.898f, 1f, 0.9f);
    [SerializeField] private float _jointSize = 14f;
    [SerializeField] private float _boneWidth = 6f;
    [SerializeField] private float _minConfidence = 0.25f;

    private Image[] _joints;
    private Image[] _bones;

    private void Awake()
    {
        if (_overlay == null) return;

        _joints = new Image[KeypointIndex.COUNT];
        for (int i = 0; i < _joints.Length; i++)
        {
            _joints[i] = CreatePart($"Joint{i}", _jointSprite, new Vector2(_jointSize, _jointSize));
        }

        _bones = new Image[Bones.Length / 2];
        for (int i = 0; i < _bones.Length; i++)
        {
            _bones[i] = CreatePart($"Bone{i}", _boneSprite, new Vector2(_boneWidth, _boneWidth));
        }
    }

    private void Update()
    {
        MotionControlService service = MotionControlService.Instance;
        UpdateStatus(service);

        Texture texture = service != null ? service.CameraTexture : null;
        bool hasTexture = texture != null && texture.width > MIN_TEXTURE_SIZE;

        if (_offlineHint != null) _offlineHint.SetActive(!hasTexture);
        if (_image != null) _image.enabled = hasTexture;

        if (!hasTexture)
        {
            HideSkeleton();
            return;
        }

        bool mirrored = service.Settings != null && service.Settings.MirrorHorizontal;
        _image.texture = texture;
        _image.uvRect = UvRect(mirrored, service.CameraVerticallyMirrored);
        if (_fitter != null) _fitter.aspectRatio = (float)texture.width / texture.height;

        DrawSkeleton(service.Snapshot, mirrored);
    }

    private void UpdateStatus(MotionControlService service)
    {
        if (_statusLabel == null) return;

        TrackingState state = service != null ? service.State.Value : TrackingState.Disabled;
        _statusLabel.text = TrackingStatePresentation.Text(state);
        _statusLabel.color = TrackingStatePresentation.Color(state);
    }

    private Image CreatePart(string name, Sprite sprite, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = _overlay.gameObject.layer;
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(_overlay, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = _skeletonColor;
        image.raycastTarget = false;
        image.enabled = false;
        return image;
    }

    private void DrawSkeleton(PoseDebugSnapshot snapshot, bool mirrored)
    {
        if (_joints == null || snapshot == null) return;

        Vector2 size = _overlay.rect.size;
        bool visible = snapshot.HasPerson && snapshot.PoseValid;

        for (int i = 0; i < _joints.Length; i++)
        {
            bool confident = visible && snapshot.Confidences[i] >= _minConfidence;
            _joints[i].enabled = confident;
            if (confident) _joints[i].rectTransform.anchoredPosition = ToLocal(snapshot.Keypoints[i], size, mirrored);
        }

        for (int i = 0; i < _bones.Length; i++)
        {
            int from = Bones[i * 2];
            int to = Bones[i * 2 + 1];
            bool confident = visible && snapshot.Confidences[from] >= _minConfidence && snapshot.Confidences[to] >= _minConfidence;

            _bones[i].enabled = confident;
            if (!confident) continue;

            Vector2 start = ToLocal(snapshot.Keypoints[from], size, mirrored);
            Vector2 end = ToLocal(snapshot.Keypoints[to], size, mirrored);
            Vector2 delta = end - start;

            RectTransform rect = _bones[i].rectTransform;
            rect.anchoredPosition = start + delta * 0.5f;
            rect.sizeDelta = new Vector2(delta.magnitude, _boneWidth);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }
    }

    private void HideSkeleton()
    {
        if (_joints == null) return;

        foreach (Image joint in _joints) joint.enabled = false;
        foreach (Image bone in _bones) bone.enabled = false;
    }

    private static Vector2 ToLocal(Vector2 cameraUv, Vector2 size, bool mirrored)
    {
        float u = mirrored ? 1f - cameraUv.x : cameraUv.x;
        return new Vector2(u * size.x, -cameraUv.y * size.y);
    }

    private static Rect UvRect(bool mirrored, bool verticallyMirrored)
    {
        return new Rect(mirrored ? 1f : 0f, verticallyMirrored ? 1f : 0f, mirrored ? -1f : 1f, verticallyMirrored ? -1f : 1f);
    }
}
