using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WindNoiseSoundArea : MonoBehaviour
{
    [SerializeField] private AudioSource _windNoise;

    private void Awake()
    {
        if (_windNoise)
        {
            _windNoise.mute = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_windNoise || !other.CompareTag("Player")) return;

        _windNoise.mute = false;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!_windNoise || !other.CompareTag("Player")) return;

        _windNoise.mute = true;
    }
}
