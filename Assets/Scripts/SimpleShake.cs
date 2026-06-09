using UnityEngine;

public class SimpleShake : MonoBehaviour
{
    public float ShakeIntensity = 0.05f;
    private Vector3 _shakeOffset;

    private void LateUpdate()
    {
        _shakeOffset = Random.insideUnitSphere * ShakeIntensity;
        transform.localPosition += _shakeOffset;
    }

    private void Update()
    {
        transform.localPosition -= _shakeOffset;
        _shakeOffset = Vector3.zero;
    }
}
