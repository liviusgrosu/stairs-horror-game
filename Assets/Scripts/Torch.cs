using UnityEngine;

public class Torch : MonoBehaviour
{
    [SerializeField] private ParticleSystem _flameParticles;

    private bool _used;
    public bool Used => _used;

    private void Awake()
    {
        if (!_flameParticles)
        {
            _flameParticles = GetComponentInChildren<ParticleSystem>();
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

        if (TorchManager.Instance)
        {
            TorchManager.Instance.NotifyTorchUsed();
        }
    }
}
