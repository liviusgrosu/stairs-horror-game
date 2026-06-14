using UnityEngine;

public class EnemyAudio : MonoBehaviour
{
    [SerializeField] private AudioSource _loopAudioSource;
    [SerializeField] private AudioSource _oneShotAudioSource;

    [SerializeField] private AudioClip _idleSound;
    [SerializeField] private AudioClip _chaseSound;
    [SerializeField] private AudioClip _takeDamageSound;
    [SerializeField] private AudioClip _dieSound;
    [SerializeField] private AudioClip _suspiciousSound;
    [SerializeField] private AudioClip _investigateSound;
    [SerializeField] private AudioClip _calmDownSound;

    [Header("Debug")]
    [SerializeField] private bool _shutUpPlease;

    private void Awake()
    {
        if (_loopAudioSource)
        {
            _loopAudioSource.enabled = !_shutUpPlease;
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

    public void PlayHurt()
    {
        if (!_oneShotAudioSource)
        {
            return;
        }
        
        if (_takeDamageSound)
        {
            _oneShotAudioSource.PlayOneShot(_takeDamageSound);
        }
    }

    public void PlayDie()
    {
        if (!_oneShotAudioSource)
        {
            return;
        }
        
        if (_dieSound)
        {
            _oneShotAudioSource.PlayOneShot(_dieSound);
        }
    }

    public void PlaySuspicious()
    {
        if (!_oneShotAudioSource)
        {
            return;
        }
        
        if (_suspiciousSound)
        {
            _oneShotAudioSource.PlayOneShot(_suspiciousSound);
        }
    }

    public void PlayInvestigate()
    {
        if (!_oneShotAudioSource)
        {
            return;
        }
        
        if (_investigateSound)
        {
            _oneShotAudioSource.PlayOneShot(_investigateSound);
        }
    }

    public void PlayCalmDown()
    {
        if (!_oneShotAudioSource)
        {
            return;
        }
        
        if (_calmDownSound)
        {
            _oneShotAudioSource.PlayOneShot(_calmDownSound);
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
