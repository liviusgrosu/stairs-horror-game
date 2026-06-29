using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ParticleZoneTrigger : MonoBehaviour
{
    private ParticleSystem _zoneParticles;

    private void Awake()
    {
        _zoneParticles = GetComponentInChildren<ParticleSystem>();
        _zoneParticles.Stop();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        foreach (var ps in other.GetComponentsInChildren<ParticleSystem>())
            ps.Stop();

        if (_zoneParticles) _zoneParticles.Play();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        foreach (var ps in other.GetComponentsInChildren<ParticleSystem>())
            ps.Play();

        if (_zoneParticles) _zoneParticles.Stop();
    }
}
