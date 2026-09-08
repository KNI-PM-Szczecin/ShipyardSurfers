using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class PanelAppearAnimation : MonoBehaviour
{
    private const float DEFAULT_DURATION = 0.25f;
    private const float DEFAULT_START_SCALE = 0.97f;
    private const float DEFAULT_SLIDE_OFFSET = 12f;

    [SerializeField] private float _duration = DEFAULT_DURATION;
    [SerializeField] private float _startScale = DEFAULT_START_SCALE;
    [SerializeField] private float _slideOffset = DEFAULT_SLIDE_OFFSET;
    [SerializeField] private float _delay;

    private CanvasGroup _group;
    private RectTransform _rect;
    private Vector2 _restingPosition;
    private float _elapsed;
    private bool _playing;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        _rect = transform as RectTransform;
        _restingPosition = _rect.anchoredPosition;
    }

    private void OnEnable()
    {
        _elapsed = 0f;
        _playing = true;
        Apply(0f);
    }

    private void OnDisable()
    {
        _playing = false;
        Apply(1f);
    }

    private void Update()
    {
        if (!_playing) return;

        _elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01((_elapsed - _delay) / Mathf.Max(_duration, Mathf.Epsilon));
        Apply(EaseOutCubic(t));

        if (t >= 1f) _playing = false;
    }

    private void Apply(float eased)
    {
        _group.alpha = eased;
        _rect.localScale = Vector3.one * Mathf.Lerp(_startScale, 1f, eased);
        _rect.anchoredPosition = _restingPosition + Vector2.down * _slideOffset * (1f - eased);
    }

    private static float EaseOutCubic(float t)
    {
        float inverse = 1f - t;
        return 1f - inverse * inverse * inverse;
    }
}
