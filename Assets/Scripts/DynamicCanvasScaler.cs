using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
public class DynamicCanvasScaler : MonoBehaviour
{
    private CanvasScaler _scaler;
    private float _lastAspect;

    private void Awake()
    {
        _scaler = GetComponent<CanvasScaler>();
        UpdateResolution();
    }

    private void Update()
    {
        float currentAspect = (float)Screen.width / Screen.height;

        if (Mathf.Abs(currentAspect - _lastAspect) > 0.01f)
        {
            UpdateResolution();
        }
    }

    private void UpdateResolution()
    {
        _lastAspect = (float)Screen.width / Screen.height;

        if (Screen.width > Screen.height)
        {
            _scaler.referenceResolution = new Vector2(1920, 1080);
        }
        else
        {
            _scaler.referenceResolution = new Vector2(1080, 1920);
        }
    }
}