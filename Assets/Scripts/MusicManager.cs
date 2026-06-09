using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;
    
    private AudioSource _audioSource;
    [SerializeField]
    private AudioClip _ambientMusic;
    [SerializeField]
    private AudioClip _chaseMusic;
    [SerializeField]
    private float _fadeDuration = 2f;

    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (_audioSource.clip == _ambientMusic)
        {
            return;
        }
        
        _audioSource.Stop();
        _audioSource.clip = _ambientMusic;
        _audioSource.volume = 1f;
        _audioSource.Play();
    }
    
    public void FadeToAmbientMusic()
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }
        _fadeCoroutine = StartCoroutine(FadeOutThenPlay(_ambientMusic));
    }

    public void PlayChaseMusic()
    {
        if (_audioSource.clip == _chaseMusic)
        {
            return;
        }
        
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        _audioSource.Stop();
        _audioSource.clip = _chaseMusic;
        _audioSource.volume = 1f;
        _audioSource.Play();
    }

    private IEnumerator FadeOutThenPlay(AudioClip nextClip)
    {
        var startVolume = _audioSource.volume;
        var elapsed = 0f;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / _fadeDuration);
            yield return null;
        }

        _audioSource.Stop();
        _audioSource.clip = nextClip;
        _audioSource.volume = 1f;
        _audioSource.Play();
        _fadeCoroutine = null;
    }
}
