using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BloodDripSound : MonoBehaviour
{
    [SerializeField] private ParticleSystem ps;
    [SerializeField] private AudioClip dripClip;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;
    [SerializeField] private float fallTime = 0.45f;
    [SerializeField] private float jitter = 0.03f;

    private AudioSource source;

    void Awake() => source = GetComponent<AudioSource>();

    void OnEnable()
    {
        float interval = 1f / Mathf.Max(0.0001f, ps.emission.rateOverTime.constant);
        StartCoroutine(DripLoop(interval));
    }

    IEnumerator DripLoop(float interval)
    {
        var landWait = new WaitForSeconds(fallTime);
        while (true)
        {
            yield return landWait;
            source.pitch = Random.Range(minPitch, maxPitch);
            source.PlayOneShot(dripClip, volume);
            yield return new WaitForSeconds(interval - fallTime + Random.Range(-jitter, jitter));
        }
    }
}
