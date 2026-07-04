using UnityEngine;

public class FurnaceGuide : MonoBehaviour
{
    [SerializeField] private float _distanceFromPlayer = 15f;
    [SerializeField] private float _lifetime = 5f;

    [Header("Effects")]
    [SerializeField] private ParticleSystem[] _particles;
    [SerializeField] private AudioSource _audioSource;

    private Transform _player;
    private Transform _target;

    private void Awake()
    {
        if (_particles == null || _particles.Length == 0)
        {
            _particles = GetComponentsInChildren<ParticleSystem>(true);
        }

        if (!_audioSource)
        {
            _audioSource = GetComponentInChildren<AudioSource>(true);
        }
    }

    public void Activate(Transform player, Transform target)
    {
        _player = player;
        _target = target;

        gameObject.SetActive(true);

        UpdateTransform();

        foreach (var ps in _particles)
        {
            if (ps) ps.Play();
        }

        if (_audioSource) _audioSource.Play();

        CancelInvoke(nameof(Deactivate));
        Invoke(nameof(Deactivate), _lifetime);
    }

    public void Deactivate()
    {
        CancelInvoke(nameof(Deactivate));

        foreach (var ps in _particles)
        {
            if (ps) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (_audioSource) _audioSource.Stop();

        gameObject.SetActive(false);
    }

    private void Update()
    {
        UpdateTransform();
    }

    private void UpdateTransform()
    {
        if (!_player || !_target) return;

        Vector3 toTarget = _target.position - _player.position;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        Vector3 dir = toTarget.normalized;

        // Sit on the edge of the circle around the player nearest the furnace...
        transform.position = (_player.position + dir * _distanceFromPlayer) + Vector3.up * 2f;
        // ...and face the furnace.
        transform.rotation = Quaternion.LookRotation(dir);
    }
}
