using System;
using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class BodyEncounter : MonoBehaviour
{
    public event Action<BodyEncounter> PlayerEntered;
    public event Action<BodyEncounter> BodyDropped;

    [SerializeField] private GameObject[] objectsToHide;
    [SerializeField] private GameObject[] objectsToShow;
    [SerializeField] private Sway sway;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip exitClip;

    [Range(0f, 1f)]
    [SerializeField] private float swayToneDownFactor = 0.3f;

    [SerializeField] private GameObject particleEffect;
    [SerializeField] private float particleRadius = 10f;

    private Transform _player;
    private bool _particleActive;
    private bool _playerInside;
    private bool _hasTriggered;

    private void Start()
    {
        if (particleEffect) particleEffect.SetActive(false);

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO) _player = playerGO.transform;
    }

    private void Update()
    {
        if (!particleEffect || !_player) return;

        var sqrDist = (_player.position - transform.position).sqrMagnitude;
        var shouldBeActive = !_hasTriggered && sqrDist <= particleRadius * particleRadius;
        if (shouldBeActive == _particleActive) return;

        _particleActive = shouldBeActive;
        particleEffect.SetActive(shouldBeActive);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasTriggered || !other.CompareTag("Player")) return;
        _playerInside = true;
        PlayerEntered?.Invoke(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (_hasTriggered || !_playerInside || !other.CompareTag("Player")) return;

        _hasTriggered = true;

        if (audioSource && exitClip)
            audioSource.PlayOneShot(exitClip);

        foreach (var go in objectsToHide)
        {
            if (go) go.SetActive(false);
        }

        foreach (var go in objectsToShow)
        {
            if (go) go.SetActive(true);
        }

        if (sway)
        {
            sway.amplitudeX *= swayToneDownFactor;
            sway.amplitudeZ *= swayToneDownFactor;
        }

        BodyDropped?.Invoke(this);
    }
}
