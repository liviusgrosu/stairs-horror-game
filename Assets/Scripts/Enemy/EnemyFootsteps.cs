using UnityEngine;
using UnityEngine.AI;

public class EnemyFootsteps : MonoBehaviour
{
    [Header("Gravel Footstep Sounds")]
    public AudioClip[] gravelSounds;

    [Header("Stone Footstep Sounds")]
    public AudioClip[] stoneSounds;

    [Header("Footstep Settings")]
    [Tooltip("Time in seconds between footsteps while walking")]
    public float walkStepInterval = 0.45f;

    [Tooltip("Time in seconds between footsteps while running")]
    public float runStepInterval = 0.25f;
    
    [Tooltip("Speed at which the interval switches from walk to run")]
    public float runSpeedThreshold = 2f;

    [Range(0f, 1f)]
    public float footstepVolume = 0.8f;

    [Tooltip("How far down to raycast to detect the surface")]
    public float raycastDistance = 1.5f;

    [Tooltip("Tag applied to gravel surfaces in the scene")]
    public string _gravelTag = "Gravel";

    [Tooltip("Tag applied to stone surfaces in the scene")]
    public string stoneTag = "Stone";

    [SerializeField]
    private AudioSource _audioSource;
    private NavMeshAgent agent;

    private float stepTimer;
    private int lastClipIndex = -1;
    private AudioClip[] lastSoundSet;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        HandleFootsteps();
    }

    private void HandleFootsteps()
    {
        var speed = agent.velocity.magnitude;

        if (speed > 0.1f)
        {
            var currentInterval = speed > runSpeedThreshold ? runStepInterval : walkStepInterval;

            stepTimer += Time.deltaTime;

            if (stepTimer >= currentInterval)
            {
                PlayFootstepForSurface();
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = walkStepInterval;
        }
    }

    private void PlayFootstepForSurface()
    {
        var soundSet = GetSoundSetForSurface();

        if (soundSet == null || soundSet.Length == 0)
        {
            return;
        }

        var clip = GetRandomClip(soundSet);
        _audioSource.PlayOneShot(clip, footstepVolume);
    }

    private AudioClip[] GetSoundSetForSurface()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out var hit, raycastDistance))
        {
            var tag = hit.collider.tag;

            if (tag == _gravelTag)
            {
                return gravelSounds;
            }

            if (tag == stoneTag)
            {
                return stoneSounds;
            }

            return stoneSounds;
        }
        return stoneSounds;
    }

    private AudioClip GetRandomClip(AudioClip[] sounds)
    {
        if (sounds.Length == 1)
        {
            return sounds[0];
        }

        if (sounds != lastSoundSet)
        {
            lastClipIndex = -1;
            lastSoundSet = sounds;
        }

        int index;
        do
        {
            index = Random.Range(0, sounds.Length);
        }
        while (index == lastClipIndex);

        lastClipIndex = index;
        return sounds[index];
    }
}
