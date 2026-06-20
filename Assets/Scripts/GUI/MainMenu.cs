using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private CanvasGroup _mainMenuGroup;
    [SerializeField] private GameObject _settingsScreen;
    [SerializeField] private SettingsMenu _settingsMenu;
    [SerializeField] private TMP_Text _settingsBackLabel;
    [SerializeField] private GameObject _creditsScreen;

    [Header("Fade")]
    [SerializeField] private Image _fadeOverlay;
    [SerializeField] private float _introFadeDuration = 2f;
    [SerializeField] private float _menuFadeDuration = 1f;

    [Header("Gameplay")]
    [SerializeField] private GameObject _hudOverlay;

    [Header("Debug")]
    [Tooltip("Skip the menu and start the game immediately when entering play mode.")]
    [SerializeField] private bool _skipMenu;

    [Header("Elevator Intro")]
    [Tooltip("Mist played while the menu is up (the player is in the elevator).")]
    [SerializeField] private ParticleSystem[] _elevatorMist;
    [Tooltip("Mist that takes over once the player presses Play.")]
    [SerializeField] private ParticleSystem[] _normalMist;
    [Tooltip("Looping chain SFX heard during the menu.")]
    [SerializeField] private AudioSource _chainSource;
    [Tooltip("One-shot metal crash SFX played when the player presses Play.")]
    [SerializeField] private AudioSource _crashSource;
    [Tooltip("Camera jolt played when the player presses Play (elevator stopping).")]
    [SerializeField] private SimpleShake _elevatorStopShake;

    private bool _gameStarted;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.InMenu = true;
            GameManager.Instance.GameStarted = false;
        }

        if (_hudOverlay) _hudOverlay.SetActive(false);
        if (_settingsScreen) _settingsScreen.SetActive(false);
        if (_creditsScreen) _creditsScreen.SetActive(false);

        ShowMainMenu();
        StartElevatorIntro();

        if (_fadeOverlay)
        {
            _fadeOverlay.gameObject.SetActive(true);
            _fadeOverlay.transform.SetAsLastSibling();
            var c = _fadeOverlay.color;
            c.a = 1f;
            _fadeOverlay.color = c;
            StartCoroutine(FadeImage(_fadeOverlay, 1f, 0f, _introFadeDuration));
        }

        if (_skipMenu)
        {
            Play();
        }
    }

    private void Update()
    {
        if (!_gameStarted)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void ShowMainMenu()
    {
        if (_settingsScreen) _settingsScreen.SetActive(false);
        if (_creditsScreen) _creditsScreen.SetActive(false);
        if (_settingsMenu) _settingsMenu.SetCloseAction(null);

        _mainMenuGroup.gameObject.SetActive(true);
        _mainMenuGroup.alpha = 1f;
        _mainMenuGroup.interactable = true;
        _mainMenuGroup.blocksRaycasts = true;
        _mainMenuGroup.transform.SetAsLastSibling();

        if (_fadeOverlay) _fadeOverlay.transform.SetAsLastSibling();
    }

    public void Play()
    {
        if (_gameStarted) return;
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        _gameStarted = true;
        _mainMenuGroup.interactable = false;
        _mainMenuGroup.blocksRaycasts = false;

        EndElevatorIntro();

        float start = _mainMenuGroup.alpha;
        float t = 0f;
        while (t < _menuFadeDuration)
        {
            t += Time.deltaTime;
            _mainMenuGroup.alpha = Mathf.Lerp(start, 0f, t / _menuFadeDuration);
            yield return null;
        }

        _mainMenuGroup.alpha = 0f;
        _mainMenuGroup.gameObject.SetActive(false);

        if (_hudOverlay) _hudOverlay.SetActive(true);

        if (_fadeOverlay) _fadeOverlay.transform.SetSiblingIndex(0);

        if (GameManager.Instance)
        {
            GameManager.Instance.InMenu = false;
            GameManager.Instance.GameStarted = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void StartElevatorIntro()
    {
        if (_elevatorMist != null)
        {
            foreach (var ps in _elevatorMist)
            {
                if (ps) ps.Play();
            }
        }

        if (_normalMist != null)
        {
            foreach (var ps in _normalMist)
            {
                if (ps) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        if (_chainSource) _chainSource.Play();
    }

    private void EndElevatorIntro()
    {
        if (_chainSource) _chainSource.Stop();
        if (_crashSource) _crashSource.Play();
        if (_elevatorStopShake) _elevatorStopShake.Shake();

        if (_elevatorMist != null)
        {
            foreach (var ps in _elevatorMist)
            {
                if (ps) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        if (_normalMist != null)
        {
            foreach (var ps in _normalMist)
            {
                if (ps) ps.Play();
            }
        }
    }

    public void OpenSettings()
    {
        _mainMenuGroup.gameObject.SetActive(false);

        if (_settingsBackLabel) _settingsBackLabel.text = "Back";
        if (_settingsScreen)
        {
            _settingsScreen.SetActive(true);
            _settingsScreen.transform.SetAsLastSibling();
        }
        if (_settingsMenu) _settingsMenu.SetCloseAction(BackFromSettings);
    }

    private void BackFromSettings()
    {
        if (_settingsScreen) _settingsScreen.SetActive(false);
        if (_settingsMenu) _settingsMenu.SetCloseAction(null);
        if (_settingsBackLabel) _settingsBackLabel.text = "Resume";
        ShowMainMenu();
    }

    public void OpenCredits()
    {
        _mainMenuGroup.gameObject.SetActive(false);
        if (_creditsScreen)
        {
            _creditsScreen.SetActive(true);
            _creditsScreen.transform.SetAsLastSibling();
        }
    }

    public void CloseCredits()
    {
        if (_creditsScreen) _creditsScreen.SetActive(false);
        ShowMainMenu();
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static IEnumerator FadeImage(Image img, float from, float to, float dur)
    {
        var c = img.color;
        c.a = from;
        img.color = c;

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, t / dur);
            img.color = c;
            yield return null;
        }

        c.a = to;
        img.color = c;
    }
}
