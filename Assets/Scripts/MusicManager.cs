using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [SerializeField] private AudioSource _ambientSource;
    [SerializeField] private AudioSource _chaseSource;
    [SerializeField] private AudioClip _ambientMusic;
    [SerializeField] private AudioClip _chaseMusic;
    [Tooltip("Volume units per second the chase track fades back to ambient once no enemy is chasing")]
    [SerializeField] private float _releaseFadeSpeed = 0.5f;

    private float _requestedBlend;
    private bool _blendRequested;
    private float _currentBlend;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        SetupSource(_ambientSource, _ambientMusic, 1f);
        SetupSource(_chaseSource, _chaseMusic, 0f);
    }

    private static void SetupSource(AudioSource source, AudioClip clip, float volume)
    {
        if (!source) return;
        source.clip = clip;
        source.loop = true;
        source.volume = volume;
        source.Play();
    }

    // Called each frame by a chasing enemy. blend: 0 = full ambient, 1 = full chase.
    // The closest enemy wins, so we keep the highest blend requested this frame.
    public void ReportChaseBlend(float blend)
    {
        blend = Mathf.Clamp01(blend);
        if (!_blendRequested || blend > _requestedBlend)
        {
            _requestedBlend = blend;
        }
        _blendRequested = true;
    }

    private void LateUpdate()
    {
        if (_blendRequested)
        {
            _currentBlend = _requestedBlend;
        }
        else
        {
            // No enemy chasing this frame: ease back to full ambient instead of snapping.
            _currentBlend = Mathf.MoveTowards(_currentBlend, 0f, _releaseFadeSpeed * Time.deltaTime);
        }

        if (_chaseSource) _chaseSource.volume = _currentBlend;
        if (_ambientSource) _ambientSource.volume = 1f - _currentBlend;

        _blendRequested = false;
        _requestedBlend = 0f;
    }
}
