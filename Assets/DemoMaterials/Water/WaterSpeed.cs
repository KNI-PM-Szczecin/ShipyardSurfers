using UnityEngine;

public class WaterSpeed : MonoBehaviour
{
    public static WaterSpeed instance;
    [SerializeField] private float maxSpeed = -1f;
    [SerializeField] private float minSpeed = -200f;
    [SerializeField] private float speed = -0.3f;
    private Vector4 speedVector = new Vector4(0, 0, 0, 0);
    [SerializeField] private Material waterMaterial = null;
    public Renderer rend = null;
    private float time;
    private float lerpSpeed = 1f;
    private float s = 0f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            rend = GetComponent<Renderer>();
            readMaterial();
        }
        else
        {
            Destroy(this);
        }
    }

    private void Update()
    {
        s = Mathf.Lerp(speedVector.y, speed, Mathf.Pow(Mathf.Clamp01((Time.time - time) * lerpSpeed), .5f));
        speedVector.y = s;
        Debug.Log("Speed: " + speed + " Vector: " + speedVector + " S: " + s + " Lerp: " + Mathf.Clamp01((Time.time - time) * lerpSpeed));
        waterMaterial.SetVector("_SurfaceNoiseScroll", speedVector);
    }

    private void setMaterial()
    {
        waterMaterial = rend.material;
    }

    private void readMaterial()
    {
        if (waterMaterial != null)
        {
            speedVector = waterMaterial.GetVector("_SurfaceNoiseScroll");
            speed = speedVector.y;
        }
    }

    private void UpdateSpeed(float newSpeed)
    {
        if (newSpeed < minSpeed) {
            newSpeed = minSpeed;
        }
        if (newSpeed > maxSpeed) {
            newSpeed = maxSpeed;
        }
        speed = newSpeed;
        // speedVector.y = speed;
        time = Time.time;
    }

    public void IncreaseSpeed(float multiplier)
    {
        UpdateSpeed(speed * multiplier);
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
        speedVector.y = speed;
        waterMaterial.SetVector("_SurfaceNoiseScroll", speedVector);
        time = 0;
    }
    public void ResetSpeed()
    {
        SetSpeed(maxSpeed);
    }

    public void SetInterval(float interval)
    {
        lerpSpeed = 1f / interval;
    }

    public float GetSpeed()
    {
        return speed;
    }

}
