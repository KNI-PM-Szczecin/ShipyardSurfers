using UnityEngine;

public class ContainerDrop : MonoBehaviour
{
    [SerializeField, Tooltip("The hanging container; released from the rope and handed to physics when the rope snaps")]
    private Rigidbody _container;

    [SerializeField, Tooltip("Random spin given to the falling container, in degrees per second")]
    private float _tumbleDegreesPerSecond = 35f;

    [SerializeField, Tooltip("How long the menu keeps showing the fall before the game scene loads")]
    private float _dropSeconds = 1.4f;

    public float DropSeconds => _dropSeconds;
    public bool HasDropped { get; private set; }

    public void Drop()
    {
        if (HasDropped || _container == null) return;

        HasDropped = true;
        _container.transform.SetParent(null, true);
        _container.isKinematic = false;
        _container.useGravity = true;
        _container.angularVelocity = Random.onUnitSphere * (_tumbleDegreesPerSecond * Mathf.Deg2Rad);
    }
}
