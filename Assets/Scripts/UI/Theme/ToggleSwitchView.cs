using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ToggleSwitchView : MonoBehaviour
{
    private const float DEFAULT_SPEED = 16f;

    [SerializeField] private RectTransform _knob;
    [SerializeField] private Image _track;
    [SerializeField] private Color _onTrackColor = Color.cyan;
    [SerializeField] private Color _offTrackColor = Color.gray;
    [SerializeField] private float _knobTravel = 40f;
    [SerializeField] private float _speed = DEFAULT_SPEED;

    private Toggle _toggle;
    private float _progress;

    private void Awake()
    {
        _toggle = GetComponent<Toggle>();
    }

    private void OnEnable()
    {
        _toggle.onValueChanged.AddListener(OnValueChanged);
        _progress = _toggle.isOn ? 1f : 0f;
        Apply();
    }

    private void OnDisable()
    {
        _toggle.onValueChanged.RemoveListener(OnValueChanged);
    }

    private void Update()
    {
        float goal = _toggle.isOn ? 1f : 0f;
        if (Mathf.Approximately(goal, _progress)) return;

        _progress = Mathf.Lerp(_progress, goal, 1f - Mathf.Exp(-_speed * Time.unscaledDeltaTime));
        if (Mathf.Abs(goal - _progress) < 0.002f) _progress = goal;
        Apply();
    }

    private void OnValueChanged(bool isOn)
    {
        if (!isActiveAndEnabled) _progress = isOn ? 1f : 0f;
    }

    private void Apply()
    {
        if (_knob != null)
        {
            _knob.anchoredPosition = new Vector2(Mathf.Lerp(-_knobTravel, _knobTravel, _progress) * 0.5f, 0f);
        }

        if (_track != null)
        {
            _track.color = Color.Lerp(_offTrackColor, _onTrackColor, _progress);
        }
    }
}
