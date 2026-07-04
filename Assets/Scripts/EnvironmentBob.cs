using UnityEngine;

public class EnvironmentBob : MonoBehaviour
{
    public float amplitude = 0.25f;
    public float frequency = 1f;

    private Vector3 _startPos;
    private float _phaseOffset;

    private void Start()
    {
        _startPos = transform.localPosition;
        _phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        var offset = Mathf.Sin(Time.time * frequency + _phaseOffset) * amplitude;
        transform.localPosition = _startPos + Vector3.up * offset;
    }
}
