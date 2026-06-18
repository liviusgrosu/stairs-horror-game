using System.Collections;
using UnityEngine;

public class EnemyAudio : MonoBehaviour
{
    [SerializeField] private AudioSource _loopAudioSource;
    [SerializeField] private AudioSource _oneShotAudioSource;

    [SerializeField] private AudioClip _idleSound;
    [SerializeField] private AudioClip _chaseSound;
    [SerializeField] private AudioClip _screamSound;

    [Header("Scream")]
    [Range(0f, 1f)]
    [SerializeField] private float _screamSpatialBlend = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool _shutUpPlease;

    private float _oneShotDefaultSpatialBlend = 1f;
    private Coroutine _restoreSpatialBlendRoutine;

    private void Awake()
    {
        if (_loopAudioSource)
        {
            _loopAudioSource.enabled = !_shutUpPlease;
        }

        if (_oneShotAudioSource)
        {
            _oneShotDefaultSpatialBlend = _oneShotAudioSource.spatialBlend;
        }
    }

    public void PlayIdleLoop()
    {
        if (!_loopAudioSource)
        {
            return;
        }

        _loopAudioSource.Stop();
        _loopAudioSource.clip = _idleSound;
        _loopAudioSource.Play();
    }

    public void PlayChaseLoop()
    {
        if (!_loopAudioSource)
        {
            return;
        }

        _loopAudioSource.Stop();
        _loopAudioSource.clip = _chaseSound;
        _loopAudioSource.Play();
    }

    public void PlayScream()
    {
        if (!_oneShotAudioSource)
        {
            return;
        }

        if (_screamSound)
        {
            if (_restoreSpatialBlendRoutine != null)
            {
                StopCoroutine(_restoreSpatialBlendRoutine);
            }

            _oneShotAudioSource.spatialBlend = _screamSpatialBlend;
            _oneShotAudioSource.PlayOneShot(_screamSound);
            _restoreSpatialBlendRoutine = StartCoroutine(RestoreSpatialBlendAfter(_screamSound.length));
        }
    }

    private IEnumerator RestoreSpatialBlendAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        _oneShotAudioSource.spatialBlend = _oneShotDefaultSpatialBlend;
        _restoreSpatialBlendRoutine = null;
    }
    
    public void StopLoop()
    {
        if (_loopAudioSource)
        {
            _loopAudioSource.Stop();
        }
    }

    public void StopAll()
    {
        if (_loopAudioSource)
        {
            _loopAudioSource.Stop();
        }
        if (_oneShotAudioSource)
        {
            _oneShotAudioSource.Stop();
        }
    }
}
