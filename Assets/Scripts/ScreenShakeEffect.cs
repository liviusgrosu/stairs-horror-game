using System.Collections;
using UnityEngine;

public class ScreenShakeEffect : MonoBehaviour
{
    public static ScreenShakeEffect Instance;
    public AnimationCurve Curve;
    public float Duration = 1f;
    public bool IsCameraShaking;
    
    public float ShakeIntensity = 0.1f;

    private Vector3 _baseLocalPosition;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _baseLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        transform.localPosition = _baseLocalPosition + Random.insideUnitSphere * ShakeIntensity;
    }
    
    public void BeginShaking()
    {
        IsCameraShaking = true;
        StartCoroutine(Shaking());
    }

    public void ShakeOnce(float intensity)
    {
        StartCoroutine(ShakeForDuration(intensity));
    }

    private IEnumerator Shaking()
    {
        var elapsedTime = 0f;

        while (elapsedTime < Duration)
        {
            elapsedTime += Time.deltaTime;
            var strength = Curve.Evaluate(elapsedTime / Duration);
            transform.localPosition = _baseLocalPosition + Random.insideUnitSphere * strength;
            yield return null;
        }

        transform.localPosition = _baseLocalPosition;
        IsCameraShaking = false;
    }

    private IEnumerator ShakeForDuration(float intensity)
    {
        IsCameraShaking = true;
        var elapsedTime = 0f;

        while (elapsedTime < Duration)
        {
            elapsedTime += Time.deltaTime;
            var strength = Curve.Evaluate(elapsedTime / Duration);
            transform.localPosition = _baseLocalPosition + Random.insideUnitSphere * strength;
            yield return null;
        }

        transform.localPosition = _baseLocalPosition;
        IsCameraShaking = false;
    }
}
