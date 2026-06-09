using UnityEngine;

public class Bob : MonoBehaviour
{
    public float amplitude = 0.25f;
    public float frequency = 1f;

    private Vector3 _startPos;

    private void Start()
    {
        _startPos = transform.localPosition;
    }

    private void Update()
    {
        var offset = Mathf.Sin(Time.time * frequency) * amplitude;
        transform.localPosition = _startPos + Vector3.up * offset;
    }
}
