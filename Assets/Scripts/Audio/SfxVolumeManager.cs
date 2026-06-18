using System.Collections.Generic;
using UnityEngine;

public class SfxVolumeManager : MonoBehaviour
{
    public static SfxVolumeManager Instance;

    private readonly Dictionary<AudioSource, float> _baseVolumes = new Dictionary<AudioSource, float>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        SoundSettings.Changed += Apply;
    }

    private void OnDisable()
    {
        SoundSettings.Changed -= Apply;
    }

    private void Start()
    {
        Rescan();
        Apply();
    }

    public void Rescan()
    {
        foreach (var source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
        {
            if (!source) continue;
            if (MusicManager.Instance && MusicManager.Instance.IsMusicSource(source)) continue;
            if (!_baseVolumes.ContainsKey(source)) _baseVolumes[source] = source.volume;
        }
    }

    public void Register(AudioSource source)
    {
        if (!source || _baseVolumes.ContainsKey(source)) return;
        _baseVolumes[source] = source.volume;
        source.volume = source.volume * SoundSettings.SfxVolume;
    }

    private void Apply()
    {
        float volume = SoundSettings.SfxVolume;
        List<AudioSource> dead = null;

        foreach (var pair in _baseVolumes)
        {
            if (!pair.Key)
            {
                (dead ??= new List<AudioSource>()).Add(pair.Key);
                continue;
            }

            pair.Key.volume = pair.Value * volume;
        }

        if (dead != null)
        {
            foreach (var source in dead) _baseVolumes.Remove(source);
        }
    }
}
