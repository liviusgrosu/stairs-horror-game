using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class CharacterFootsteps : MonoBehaviour
{
    public static event Action<float> OnFootstepNoise;

    [Header("Gravel Footstep Sounds")]
    public AudioClip[] gravelSounds;

    [Header("Stone Footstep Sounds")]
    public AudioClip[] stoneSounds;

    [Header("Wood Footstep Sounds")]
    public AudioClip[] woodSounds;

    [Header("Grass Footstep Sounds")]
    public AudioClip[] grassSounds;
    
    [Header("Carpet Footstep Sounds")]
    public AudioClip[] carpetSounds;
    
    [Header("Metal Footstep Sounds")]
    public AudioClip[] metalSounds;
    
    
    [Header("Speed Sound Multipliers")]
    [SerializeField] private float _walkSpeedMultiplier = 0.5f;
    [SerializeField] private float _sprintSpeedMultiplier = 1.5f;
    [SerializeField] private float _crouchSpeedMultiplier = 0.2f;

    [Header("Speed Volumes")]
    [Range(0f, 1f)]
    public float walkingVolumeMultiplier = 0.8f;
    [Range(0f, 1f)]
    public float sprintVolumeMultiplier = 1f;
    [Range(0f, 1f)]
    public float crouchVolumeMultiplier = 0.5f;

    public float raycastDistance = 1.5f;

    private const string GravelTag = "Gravel";
    private const string StoneTag = "Stone";
    private const string WoodTag = "Wood";
    private const string GrassTag = "Grass";
    private const string CarpetTag = "Carpet";
    private const string MetalTag = "Metal";

    [Header("Noise")]
    [SerializeField] private float _baseNoiseRadius = 8f;
    [SerializeField] private float _gravelNoise = 0.9f;
    [SerializeField] private float _stoneNoise = 0.5f;
    [SerializeField] private float _woodNoise = 0.65f;
    [SerializeField] private float _grassNoise = 0.4f;
    [SerializeField] private float _carpetNoise = 0.3f;
    [SerializeField] private float _metalNoise = 0.85f;
    
    [Header("Speed Frequencies")]
    public float stepInterval = 0.45f;
    public float sprintStepInterval = 0.3f;
    public float crouchStepInterval = 0.9f;

    [Header("Landing")]
    [SerializeField] private float _landingMinVelocity = 1f;
    [SerializeField] private float _landingMaxVelocity = 10f;

    public bool IsDampened { get; set; }
    public float DampenedSpeedMultiplier { get; set; }
    public float DampenedVolumeMultiplier { get; set; }

    private AudioSource _audioSource;
    private CharacterController _characterController;
    private PlayerMovement _playerMovement;
    private int _ignoreSelfMask;

    private float _stepTimer;
    private int _lastClipIndex = -1;
    private AudioClip[] _lastSoundSet;
    private float _lastNoiseRadius;
    private bool _wasGrounded;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _characterController = GetComponent<CharacterController>();
        _playerMovement = GetComponent<PlayerMovement>();

        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _ignoreSelfMask = ~(1 << gameObject.layer);
    }

    private void Update()
    {
        HandleLanding();
        HandleFootsteps();
    }

    private void HandleFootsteps()
    {
        var isMoving = _characterController.velocity.magnitude > 0.1f;
        var isGrounded = _characterController.isGrounded;

        if (isMoving && isGrounded)
        {
            _stepTimer += Time.deltaTime;

            var currentInterval = _playerMovement.IsCrouching 
                ? crouchStepInterval 
                : _playerMovement.IsSprinting 
                    ? sprintStepInterval 
                    : stepInterval;

            if (_stepTimer >= currentInterval)
            {
                PlayFootstepForSurface();
                EmitFootstepNoise();
                _stepTimer = 0f;
            }
        }
        else
        {
            _stepTimer = 0f;
        }
    }

    private void HandleLanding()
    {
        var isGrounded = _characterController.isGrounded;

        if (!_wasGrounded && isGrounded)
        {
            var fallSpeed = Mathf.Abs(_characterController.velocity.y);
            var t = Mathf.InverseLerp(_landingMinVelocity, _landingMaxVelocity, fallSpeed);
            var volume = IsDampened
                ? DampenedVolumeMultiplier
                : Mathf.Lerp(crouchVolumeMultiplier, sprintVolumeMultiplier, t);

            var soundSet = GetSoundSetForSurface();
            if (soundSet != null && soundSet.Length >= 2)
            {
                var clip1 = GetRandomClip(soundSet);
                var clip2 = GetRandomClip(soundSet);
                _audioSource.PlayOneShot(clip1, volume);
                _audioSource.PlayOneShot(clip2, volume);
            }

            var speedMultiplier = IsDampened
                ? DampenedSpeedMultiplier
                : Mathf.Lerp(_crouchSpeedMultiplier, _sprintSpeedMultiplier, t);
            var (surfaceNoise, surfaceTag) = GetSurfaceNoiseLevel();
            _lastNoiseRadius = _baseNoiseRadius * surfaceNoise * speedMultiplier;
            NoiseEmitter.Emit(transform.position, _lastNoiseRadius, surfaceTag);
            OnFootstepNoise?.Invoke(_lastNoiseRadius);
        }

        _wasGrounded = isGrounded;
    }

    private void EmitFootstepNoise()
    {
        var (surfaceNoise, surfaceTag) = GetSurfaceNoiseLevel();
        var speedMultiplier = IsDampened
            ? DampenedSpeedMultiplier
            : _playerMovement && _playerMovement.IsCrouching
                ? _crouchSpeedMultiplier
                : _playerMovement && _playerMovement.IsSprinting
                    ? _sprintSpeedMultiplier
                    : _walkSpeedMultiplier;

        _lastNoiseRadius = _baseNoiseRadius * surfaceNoise * speedMultiplier;
        NoiseEmitter.Emit(transform.position, _lastNoiseRadius, surfaceTag);
        OnFootstepNoise?.Invoke(_lastNoiseRadius);
    }

    private (float, string) GetSurfaceNoiseLevel()
    {
        if (!Physics.Raycast(transform.position, Vector3.down, out var hit, raycastDistance, _ignoreSelfMask))
        {
            return (_stoneNoise, StoneTag);
        }

        var tag = hit.collider.tag;

        if (tag == GravelTag)
        {
            return (_gravelNoise, GravelTag);
        }
        if (tag == WoodTag)
        {
            return (_woodNoise, WoodTag);
        }
        if (tag == GrassTag)
        {
            return (_grassNoise, GrassTag);
        }
        if (tag == CarpetTag)
        {
            return (_carpetNoise, CarpetTag);
        }
        if (tag == MetalTag)
        {
            return (_metalNoise, MetalTag);
        }
        return (_stoneNoise, StoneTag);
    }

    private void PlayFootstepForSurface()
    {
        var soundSet = GetSoundSetForSurface();

        if (soundSet == null || soundSet.Length == 0)
        {
            return;
        }

        var volume = IsDampened
            ? DampenedVolumeMultiplier
            : _playerMovement && _playerMovement.IsCrouching
                ? crouchVolumeMultiplier
                : _playerMovement && _playerMovement.IsSprinting
                    ? sprintVolumeMultiplier
                    : walkingVolumeMultiplier;

        _audioSource.PlayOneShot(GetRandomClip(soundSet), volume);
    }

    private AudioClip[] GetSoundSetForSurface()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out var hit, raycastDistance, _ignoreSelfMask))
        {
            var tag = hit.collider.tag;

            if (tag == GravelTag)
            {
                return gravelSounds;
            }
            if (tag == WoodTag)
            {
                return woodSounds;
            }
            if (tag == StoneTag)
            {
                return stoneSounds;
            }
            if (tag == GrassTag)
            {
                return grassSounds;
            }
            if (tag == CarpetTag)
            {
                return carpetSounds;
            }
            if (tag == MetalTag)
            {
                return metalSounds;
            }
            return stoneSounds;
        }
        return stoneSounds;
    }

    private void OnDrawGizmos()
    {
        if (_lastNoiseRadius <= 0f)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, _lastNoiseRadius);
        Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, _lastNoiseRadius);
    }

    private AudioClip GetRandomClip(AudioClip[] sounds)
    {
        if (sounds.Length == 1)
        {
            return sounds[0];
        }

        if (sounds != _lastSoundSet)
        {
            _lastClipIndex = -1;
            _lastSoundSet = sounds;
        }

        int index;
        do
        {
            index = Random.Range(0, sounds.Length);
        }
        while (index == _lastClipIndex);

        _lastClipIndex = index;
        return sounds[index];
    }
}
