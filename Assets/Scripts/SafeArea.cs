using System.Collections;
using UnityEngine;

public class SafeArea : MonoBehaviour
{
    public static bool PlayerInside { get; private set; }

    [SerializeField] private float _safeRadius = 14f;
    [SerializeField] private float _despawnDistance = 40f;

    private static SafeArea _active;

    private Transform _player;

    private AudioSource _audioSource;
    private float _maxVolume;
    private Coroutine _audioFadeCoroutine;

    private void Awake()
    {
        _active = this;

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource)
        {
            _maxVolume = _audioSource.volume;
            _audioSource.volume = 0f;
        }

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO) _player = playerGO.transform;
    }

    public void FadeInAudio(float duration)
    {
        if (!_audioSource) return;

        if (_audioFadeCoroutine != null) StopCoroutine(_audioFadeCoroutine);
        _audioFadeCoroutine = StartCoroutine(FadeInAudioRoutine(duration));
    }

    private IEnumerator FadeInAudioRoutine(float duration)
    {
        float startVolume = _audioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(startVolume, _maxVolume, elapsed / duration);
            yield return null;
        }

        _audioSource.volume = _maxVolume;
        _audioFadeCoroutine = null;
    }

    private void OnDestroy()
    {
        PlayerInside = false;
        if (_active == this) _active = null;
    }

    public static bool TryGetBoundaryPoint(Vector3 from, out Vector3 point)
    {
        point = from;
        if (!PlayerInside || _active == null) return false;

        Vector3 center = _active.transform.position;
        Vector3 flat = from - center;
        flat.y = 0f;

        float dist = flat.magnitude;
        Vector3 dir = dist > 0.0001f ? flat / dist : Vector3.forward;
        point = center + dir * _active._safeRadius;
        point.y = from.y;
        return true;
    }

    private void Update()
    {
        if (!_player)
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (!playerGO) return;
            _player = playerGO.transform;
        }

        float sqrDistance = (_player.position - transform.position).sqrMagnitude;

        if (sqrDistance >= _despawnDistance * _despawnDistance)
        {
            PlayerInside = false;
            Destroy(gameObject);
            return;
        }

        PlayerInside = sqrDistance <= _safeRadius * _safeRadius;
    }
}
