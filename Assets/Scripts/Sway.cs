using UnityEngine;

public class Sway : MonoBehaviour
{
    public float amplitudeX = 3f;
    public float amplitudeZ = 1.5f;
    public float frequencyX = 0.5f;
    public float frequencyZ = 0.37f;
    public float phaseOffset = 1.3f;

    private Quaternion _startRot;
    private float _seed;

    private void Start()
    {
        _startRot = transform.localRotation;
        _seed = Random.value * 100f;
    }

    private void Update()
    {
        var t = Time.time + _seed;
        var x = Mathf.Sin(t * frequencyX * Mathf.PI * 2f) * amplitudeX;
        var z = Mathf.Sin(t * frequencyZ * Mathf.PI * 2f + phaseOffset) * amplitudeZ;
        transform.localRotation = _startRot * Quaternion.Euler(x, 0f, z);
    }
}
