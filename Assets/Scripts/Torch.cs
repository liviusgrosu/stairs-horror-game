using System.Collections;
using UnityEngine;

public class Torch : MonoBehaviour
{
    [SerializeField] private ParticleSystem _flameParticles;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private float _audioFadeDuration = 2f;

    private bool _used;
    public bool Used => _used;

    private void Awake()
    {
        if (!_flameParticles)
        {
            _flameParticles = GetComponentInChildren<ParticleSystem>();
        }

        if (!_audioSource)
        {
            _audioSource = GetComponentInChildren<AudioSource>();
        }
    }

    public void Interact()
    {
        if (_used)
        {
            if (GameManager.Instance)
            {
                GameManager.Instance.ShowUsedTorchText();
            }
            return;
        }

        _used = true;

        if (_flameParticles)
        {
            _flameParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (_audioSource)
        {
            StartCoroutine(FadeOutAudio());
        }

        if (TorchManager.Instance)
        {
            TorchManager.Instance.NotifyTorchUsed();
        }
    }

    private IEnumerator FadeOutAudio()
    {
        float startVolume = _audioSource.volume;
        float elapsed = 0f;

        while (elapsed < _audioFadeDuration)
        {
            elapsed += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / _audioFadeDuration);
            yield return null;
        }

        _audioSource.Stop();
        _audioSource.volume = startVolume;
    }
}
