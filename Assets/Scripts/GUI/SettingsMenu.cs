using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the in-game settings panel: music/SFX volume sliders, a fullscreen
/// toggle and a resolution dropdown. Volume values are stored via
/// <see cref="SoundSettings"/> and applied live by the audio managers.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private TMP_Text _musicValueLabel;
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private TMP_Text _sfxValueLabel;

    [Header("Display")]
    [SerializeField] private Toggle _fullscreenToggle;
    [SerializeField] private TMP_Dropdown _resolutionDropdown;

    private Resolution[] _resolutions;

    private void Start()
    {
        if (_musicSlider)
        {
            _musicSlider.minValue = 0f;
            _musicSlider.maxValue = 1f;
            _musicSlider.SetValueWithoutNotify(SoundSettings.MusicVolume);
            _musicSlider.onValueChanged.AddListener(OnMusicChanged);
            UpdateMusicLabel(SoundSettings.MusicVolume);
        }

        if (_sfxSlider)
        {
            _sfxSlider.minValue = 0f;
            _sfxSlider.maxValue = 1f;
            _sfxSlider.SetValueWithoutNotify(SoundSettings.SfxVolume);
            _sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            UpdateSfxLabel(SoundSettings.SfxVolume);
        }

        if (_fullscreenToggle)
        {
            _fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
            _fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }

        if (_resolutionDropdown)
        {
            PopulateResolutions();
            _resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }
    }

    private void OnMusicChanged(float value)
    {
        SoundSettings.MusicVolume = value;
        UpdateMusicLabel(value);
    }

    private void OnSfxChanged(float value)
    {
        SoundSettings.SfxVolume = value;
        UpdateSfxLabel(value);
    }

    private void UpdateMusicLabel(float value)
    {
        if (_musicValueLabel) _musicValueLabel.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private void UpdateSfxLabel(float value)
    {
        if (_sfxValueLabel) _sfxValueLabel.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private void OnFullscreenChanged(bool isOn)
    {
        Screen.fullScreen = isOn;
    }

    private void PopulateResolutions()
    {
        var seen = new HashSet<string>();
        var filtered = new List<Resolution>();
        var options = new List<string>();
        int current = 0;

        foreach (var resolution in Screen.resolutions)
        {
            string key = resolution.width + " x " + resolution.height;
            if (!seen.Add(key)) continue;

            filtered.Add(resolution);
            options.Add(key);

            if (resolution.width == Screen.width && resolution.height == Screen.height)
            {
                current = filtered.Count - 1;
            }
        }

        _resolutions = filtered.ToArray();
        _resolutionDropdown.ClearOptions();
        _resolutionDropdown.AddOptions(options);
        _resolutionDropdown.SetValueWithoutNotify(current);
        _resolutionDropdown.RefreshShownValue();
    }

    private void OnResolutionChanged(int index)
    {
        if (_resolutions == null || index < 0 || index >= _resolutions.Length) return;

        var resolution = _resolutions[index];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode);
    }

    /// <summary>Hooked to the Resume/Back button. Closes the menu and unpauses.</summary>
    public void Close()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.ResumeGame();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
