using UnityEngine;

public static class SoundSettings
{
    private const string MusicKey = "settings.musicVolume";
    private const string SfxKey = "settings.sfxVolume";

    public static event System.Action Changed;

    private static float _music = -1f;
    private static float _sfx = -1f;

    public static float MusicVolume
    {
        get
        {
            if (_music < 0f) _music = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 1f));
            return _music;
        }
        set
        {
            _music = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicKey, _music);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    public static float SfxVolume
    {
        get
        {
            if (_sfx < 0f) _sfx = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, 1f));
            return _sfx;
        }
        set
        {
            _sfx = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxKey, _sfx);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
