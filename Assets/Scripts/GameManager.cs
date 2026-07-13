using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    //[SerializeField] private GameObject _upgradeText;
    [SerializeField] private GameObject _questionMarkIcon;
    [SerializeField] private GameObject _pickupIcon;
    
    public GameObject OverlayUI;
    public GameObject GameOverScreen;
    
    public bool InMenu;
    
    private bool triggeredFirstChase, triggeredSecondChase;

    [SerializeField]
    private TextMeshProUGUI _entranceDoorText, _normalRockHoverText, _mineralDepositHoverText, _blockageRockHoverText, 
        _usedFurnaceText, _lockedDoorText, _needFurnaceItemText, _pickupTutorialText, _runningTutorialText, _crouchingTutorialText,
        _jumpingTutorialText, _incorrectFuranceText;
    private bool DisplayingHoverText;
    private Coroutine _hoverTextCoroutine;

    private int _emberBallsHeld;

    public bool HasWon, HasDied;
    public bool IsPaused;

    // Set true by the main menu once the player presses Play. Until then the
    // Escape pause menu stays disabled so it can't fight the main menu.
    public bool GameStarted;

    [Header("Locked Door Messages")]
    [Tooltip("Shown when interacting with the door before any furnace is lit.")]
    [SerializeField, TextArea] private string _doorSolidMessage = "This ice feels really solid";
    [Tooltip("Shown when interacting with the door after the 1st furnace is lit.")]
    [SerializeField, TextArea] private string _doorCracksMessage = "I'm starting to see cracks";
    [Tooltip("Shown when interacting with the door after the 2nd furnace is lit.")]
    [SerializeField, TextArea] private string _doorBreakingMessage = "It's almost about to break";

    [SerializeField] private GameObject _controlsOverlay;

    [SerializeField] private CanvasGroup _mineralStatsCanvasGroup;
    private Coroutine _mineralStatsCoroutine;
    private float _mineralStatsTimer;
    private bool _mineralStatsVisible;

    private GameObject player;

    [SerializeField] private Volume _deathPostProcessVolume;
    [SerializeField] private Image _blackScreen;
    [SerializeField] private float _deathFadeDuration = 2f;
    [SerializeField] private float _blackHoldDuration = 1f;
    [SerializeField] private float _whiteFadeInDuration = 1.5f;
    [SerializeField] private float _whiteFadeOutDuration = 3f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && GameStarted && !HasWon && !HasDied)
        {
            ToggleControlsOverlay();
        }
    }

    public void Start()
    {
        if (DebugManager.ShouldStartWith500Embers)
        {
            _emberBallsHeld = DebugManager.ManyEmberCount;
        }
        else if (DebugManager.ShouldStartWithEmbers)
        {
            _emberBallsHeld = DebugManager.StartingEmberCount;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (_mineralStatsCanvasGroup != null)
            _mineralStatsCanvasGroup.alpha = 0f;

        if (_deathPostProcessVolume != null)
            _deathPostProcessVolume.gameObject.SetActive(false);

        player = GameObject.Find("Player");
    }
    
    public void TogglePickupIcon(bool state)
    {
        _pickupIcon.SetActive(state);
    }

    public void ToggleQuestionMark(bool state)
    {
        _questionMarkIcon.SetActive(state);
    }

    public void ToggleOffAllText()
    {
        _pickupIcon.SetActive(false);
        //_upgradeText.SetActive(false);
        _questionMarkIcon.SetActive(false);
    }

    public void OpenGameOverScreen()
    {
        TriggerRespawn(true);
    }

    public void OpenPitDeathScreen()
    {
        TriggerRespawn(false);
    }

    private void TriggerRespawn(bool playDeathCollapse)
    {
        if (HasDied) return;
        HasDied = true;
        StartCoroutine(RespawnRoutine(playDeathCollapse));
    }

    private IEnumerator RespawnRoutine(bool playDeathCollapse)
    {
        foreach (var enemy in FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
        {
            if (enemy) enemy.Disengage();
        }

        var playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement)
        {
            playerMovement.ForceCrouch();
        }

        if (playDeathCollapse && CameraHitEffect.Instance)
        {
            yield return CameraHitEffect.Instance.PlayDeathCollapse();
        }

        var clearBlack = new Color(0f, 0f, 0f, 0f);
        var black = new Color(0f, 0f, 0f, 1f);
        var white = new Color(1f, 1f, 1f, 1f);
        var clearWhite = new Color(1f, 1f, 1f, 0f);

        yield return FadeScreen(clearBlack, black, _deathFadeDuration);

        foreach (var enemy in FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
        {
            if (enemy) Destroy(enemy.gameObject);
        }

        if (FurnaceManager.Instance)
        {
            FurnaceManager.Instance.OnPlayerRespawned();
        }

        TeleportToRespawn();

        if (PlayerHealth.Instance)
        {
            PlayerHealth.Instance.ResetForRespawn();
        }

        SafeArea safeArea = null;
        if (FurnaceManager.Instance && player)
        {
            safeArea = FurnaceManager.Instance.SpawnSafeArea(player.transform.position);
        }

        if (playerMovement)
        {
            playerMovement.ResetCrouchState();
        }

        if (CameraHitEffect.Instance)
        {
            CameraHitEffect.Instance.ResetDeathCollapse();
        }

        HasDied = false;
        ToggleCursorLock(false);

        yield return new WaitForSeconds(_blackHoldDuration);

        yield return FadeScreen(black, white, _whiteFadeInDuration);

        if (safeArea)
        {
            safeArea.FadeInAudio(_whiteFadeOutDuration);
        }

        yield return FadeScreen(white, clearWhite, _whiteFadeOutDuration);

        if (_blackScreen)
        {
            _blackScreen.color = clearBlack;
            _blackScreen.gameObject.SetActive(false);
        }
    }

    private void TeleportToRespawn()
    {
        if (player == null)
        {
            player = GameObject.Find("Player");
        }
        if (player == null) return;

        var point = RespawnPoint.GetRandom();
        if (point == null) return;

        var controller = player.GetComponent<CharacterController>();
        if (controller) controller.enabled = false;

        player.transform.position = point.transform.position;
        player.transform.rotation = point.transform.rotation;

        if (controller) controller.enabled = true;

        var navObstacle = player.GetComponent<NavMeshObstacle>();
        if (navObstacle) navObstacle.enabled = false;
    }

    private IEnumerator FadeScreen(Color from, Color to, float duration)
    {
        if (_blackScreen == null) yield break;

        _blackScreen.gameObject.SetActive(true);
        _blackScreen.transform.SetAsLastSibling();
        _blackScreen.color = from;

        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _blackScreen.color = Color.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        _blackScreen.color = to;
    }

    public void ToggleControlsOverlay()
    {
        var isOpen = _controlsOverlay && _controlsOverlay.activeSelf;
        CloseAllMenus();

        if (isOpen)
        {
            return;
        }

        IsPaused = true;
        InMenu = true;

        if (_controlsOverlay)
        {
            _controlsOverlay.SetActive(true);
        }

        ToggleCursorLock(true);
        Time.timeScale = 0f;
        SetBreathingPaused(true);
    }

    public void ResumeGame()
    {
        CloseAllMenus();
    }

    private void CloseAllMenus()
    {
        IsPaused = false;
        InMenu = false;

        if (_controlsOverlay)
        {
            _controlsOverlay.SetActive(false);
        }

        ToggleCursorLock(false);
        Time.timeScale = 1f;
        SetBreathingPaused(false);
    }

    private void SetBreathingPaused(bool paused)
    {
        var playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement)
        {
            if (paused) playerMovement.PauseBreathing();
            else playerMovement.ResumeBreathing();
        }

        foreach (var enemyAudio in FindObjectsByType<EnemyAudio>(FindObjectsSortMode.None))
        {
            if (paused) enemyAudio.PauseLoop();
            else enemyAudio.ResumeLoop();
        }
    }

    private void ToggleCursorLock(bool state)
    {
        Cursor.lockState = state ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = state;
    }
    
    public void ShowUsedFurnaceText()
    {
        if (DisplayingHoverText || !_usedFurnaceText)
        {
            return;
        }

        DisplayingHoverText = true;
        if (_hoverTextCoroutine != null) StopCoroutine(_hoverTextCoroutine);
        _hoverTextCoroutine = StartCoroutine(FadeTextInAndOut(_usedFurnaceText));
    }

    public void ShowNeedFurnaceItemText()
    {
        if (DisplayingHoverText || !_needFurnaceItemText)
        {
            return;
        }

        DisplayingHoverText = true;
        if (_hoverTextCoroutine != null) StopCoroutine(_hoverTextCoroutine);
        _hoverTextCoroutine = StartCoroutine(FadeTextInAndOut(_needFurnaceItemText));
    }

    public int EmberBallsHeld => _emberBallsHeld;

    public void AddEmberBall()
    {
        _emberBallsHeld++;
    }

    public bool TryUseEmberBall()
    {
        if (_emberBallsHeld <= 0)
        {
            return false;
        }

        _emberBallsHeld--;
        return true;
    }

    public void ShowLockedDoorText()
    {
        if (DisplayingHoverText || !_lockedDoorText)
        {
            return;
        }

        int furnacesLit = FurnaceManager.Instance ? FurnaceManager.Instance.UsedFurnaceCount : 0;
        _lockedDoorText.text = furnacesLit switch
        {
            0 => _doorSolidMessage,
            1 => _doorCracksMessage,
            _ => _doorBreakingMessage,
        };

        DisplayingHoverText = true;
        if (_hoverTextCoroutine != null) StopCoroutine(_hoverTextCoroutine);
        _hoverTextCoroutine = StartCoroutine(FadeTextInAndOut(_lockedDoorText));
    }

    public void ShowPickupTutorialText()
    {
        if (DisplayingHoverText || !_pickupTutorialText)
        {
            return;
        }

        DisplayingHoverText = true;
        if (_hoverTextCoroutine != null) StopCoroutine(_hoverTextCoroutine);
        _hoverTextCoroutine = StartCoroutine(FadeTextInAndOut(_pickupTutorialText));
    }
    
    public void ShowRunningTutorialText()
    {
        if (DisplayingHoverText || !_runningTutorialText)
        {
            return;
        }

        DisplayingHoverText = true;
        if (_hoverTextCoroutine != null) StopCoroutine(_hoverTextCoroutine);
        _hoverTextCoroutine = StartCoroutine(FadeTextInAndOut(_runningTutorialText));
    }
    
    public void ShowCrouchingTutorialText()
    {
        if (DisplayingHoverText || !_crouchingTutorialText)
        {
            return;
        }

        DisplayingHoverText = true;
        if (_hoverTextCoroutine != null) StopCoroutine(_hoverTextCoroutine);
        _hoverTextCoroutine = StartCoroutine(FadeTextInAndOut(_crouchingTutorialText));
    }
    
    public void ShowJumpingTutorialText()
    {
        if (DisplayingHoverText || !_jumpingTutorialText)
        {
            return;
        }

        DisplayingHoverText = true;
        if (_hoverTextCoroutine != null) StopCoroutine(_hoverTextCoroutine);
        _hoverTextCoroutine = StartCoroutine(FadeTextInAndOut(_jumpingTutorialText));
    }
    
    public void ShowIncorrectFurnaceText()
    {
        if (DisplayingHoverText || !_incorrectFuranceText)
        {
            return;
        }

        DisplayingHoverText = true;
        if (_hoverTextCoroutine != null) StopCoroutine(_hoverTextCoroutine);
        _hoverTextCoroutine = StartCoroutine(FadeTextInAndOut(_incorrectFuranceText));
    }

    private IEnumerator FadeTextInAndOut(TextMeshProUGUI text)
    {
        const float fadeDuration = 0.25f; 
        const float pauseDuration = 5f; 
    
        text.gameObject.SetActive(true);
        var originalColor = text.color;
        text.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
    
        var elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            var alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            text.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
    
        text.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
    
        yield return new WaitForSeconds(pauseDuration);
    
        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            var alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            text.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
    
        text.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        text.gameObject.SetActive(false);
        DisplayingHoverText = false;
    }
    
    public void WinGame()
    {
        HasWon = true;
        // Free the cursor so the win screen's Quit button is clickable.
        ToggleCursorLock(true);
        SetBreathingPaused(true);
        StartCoroutine(FadeOutAllAudio());
    }

    private IEnumerator FadeOutAllAudio()
    {
        const float fadeDuration = 3f;

        if (MusicManager.Instance) MusicManager.Instance.FadeOutForWin(fadeDuration);

        var audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        var startVolumes = new float[audioSources.Length];

        for (int i = 0; i < audioSources.Length; i++)
        {
            startVolumes[i] = audioSources[i].volume;
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            for (int i = 0; i < audioSources.Length; i++)
            {
                if (audioSources[i] == null) continue;
                if (MusicManager.Instance && MusicManager.Instance.IsMusicSource(audioSources[i])) continue;
                audioSources[i].volume = Mathf.Lerp(startVolumes[i], 0f, t);
            }

            yield return null;
        }

        for (int i = 0; i < audioSources.Length; i++)
        {
            if (audioSources[i] == null) continue;
            if (MusicManager.Instance && MusicManager.Instance.IsMusicSource(audioSources[i])) continue;
            audioSources[i].volume = 0f;
            audioSources[i].Stop();
        }
    }
}
