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

        foreach (var ps in GetMistParticles(other))
            ps.Stop();

        if (_zoneParticles) _zoneParticles.Play();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        foreach (var ps in GetMistParticles(other))
            ps.Play();

        if (_zoneParticles) _zoneParticles.Stop();
    }

    private ParticleSystem[] GetMistParticles(Collider player)
    {
        var mist = player.transform.Find("Normal - Mist");
        if (!mist) return System.Array.Empty<ParticleSystem>();
        return mist.GetComponentsInChildren<ParticleSystem>();
    }
}
