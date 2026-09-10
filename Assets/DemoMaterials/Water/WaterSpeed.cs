using UnityEngine;

public class WaterSpeed : MonoBehaviour
{
    public static WaterSpeed instance;
    [SerializeField] private float maxSpeed = -.3f;
    [SerializeField] private float minSpeed = -2f;
    [SerializeField] private float speed = -0.3f;
    private Vector4 speedVector = new Vector4(0, 0, 0, 0);
    [SerializeField] private Material waterMaterial = null;
    private Material newWater = null;
    public Renderer rend = null;
    private float time;

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
        waterMaterial.Lerp(waterMaterial, newWater, Mathf.Clamp01((Time.time - time)*2)* 1f);
    }

    private void setMaterial()
    {
        Debug.Log(waterMaterial);
        waterMaterial = GetComponent<Renderer>().material;
        Debug.Log(waterMaterial);
    }

    private void readMaterial()
    {
        newWater = Instantiate(waterMaterial);
        if (waterMaterial != null)
        {
            speedVector = waterMaterial.GetVector("_SurfaceNoiseScroll");
            // Debug.Log("WaterSpeed: " + speedVector);
            speed = speedVector.y;
        }
    }

    private void UpdateMaterial()
    {
        newWater = Instantiate(waterMaterial);
        time = Time.time;
    }

    private void SetSpeed(float newSpeed)
    {
        if (newSpeed < minSpeed) {
            newSpeed = minSpeed;
        }
        if (newSpeed > maxSpeed) {
            newSpeed = maxSpeed;
        }
        speed = newSpeed;
        speedVector.y = speed;
        newWater.SetVector("_SurfaceNoiseScroll", speedVector);
    }

    public void IncreaseSpeed(float multiplier)
    {
        SetSpeed(speed * multiplier);
    }

    public void ResetSpeed()
    {
        SetSpeed(-0.3f);
    }

    public float GetSpeed()
    {
        return speed;
    }

}
