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
    private TextMeshProUGUI _entranceDoorText, _normalRockHoverText, _mineralDepositHoverText, _blockageRockHoverText, _usedFurnaceText, _lockedDoorText, _needFurnaceItemText;
    private bool DisplayingHoverText;
    private Coroutine _hoverTextCoroutine;

    private int _emberBallsHeld;

    public bool HasWon, HasDied;
    public bool IsPaused;

    [SerializeField] private GameObject _controlsOverlay;

    [SerializeField] private CanvasGroup _mineralStatsCanvasGroup;
    private Coroutine _mineralStatsCoroutine;
    private float _mineralStatsTimer;
    private bool _mineralStatsVisible;

    private GameObject player;

    [SerializeField] private Volume _deathPostProcessVolume;
    [SerializeField] private Image _blackScreen;

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
        if (Input.GetKeyDown(KeyCode.Escape) && !HasWon && !HasDied)
        {
            ToggleControlsOverlay();
        }
    }

    public void Start()
    {
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
        player.GetComponent<CharacterController>().height = 0.1f;
        player.GetComponent<CapsuleCollider>().height = 0.1f;

        StartCoroutine(DeathBlurRoutine());

        foreach (var enemy in FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
        {
            enemy.Disengage();
        }

        var navObstacle = player.GetComponent<NavMeshObstacle>();
        if (navObstacle) navObstacle.enabled = true;

        ToggleCursorLock(true);
        GameOverScreen.SetActive(true);
        OverlayUI.SetActive(true);

        HasDied = true;

        /*if (PickaxeHand.Instance)
        {
            PickaxeHand.Instance.GetComponent<Animator>().Play("Hand - Death");
        }*/

        var bloodVFX = player.transform.Find("Other SFX/Blood - Player - VFX");
        if (bloodVFX != null)
        {
            bloodVFX.gameObject.SetActive(true);
            var ps = bloodVFX.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();
        }
    }

    public void OpenPitDeathScreen()
    {
        HasDied = true;
        StartCoroutine(PitDeathRoutine());
    }

    private IEnumerator PitDeathRoutine()
    {
        if (_blackScreen)
        {
            _blackScreen.gameObject.SetActive(true);
            var color = _blackScreen.color;
            var elapsed = 0f; 
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(0f, 1f, elapsed / 1f);
                _blackScreen.color = color;
                yield return null;
            }
            color.a = 1f;
            _blackScreen.color = color;
        }

        yield return new WaitForSeconds(1f);

        OpenGameOverScreen();
    }

    private IEnumerator DeathBlurRoutine()
    {
        if (_deathPostProcessVolume == null) yield break;

        if (!_deathPostProcessVolume.profile.TryGet(out DepthOfField dof))
            yield break;

        _deathPostProcessVolume.gameObject.SetActive(true);

        const float startFocusDistance = 5f;
        const float blurDuration = 1f;

        dof.active = true;
        dof.mode.Override(DepthOfFieldMode.Bokeh);
        dof.focusDistance.Override(startFocusDistance);

        float elapsed = 0f;

        while (elapsed < blurDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / blurDuration;
            dof.focusDistance.Override(Mathf.Lerp(startFocusDistance, 0f, t));
            yield return null;
        }

        dof.focusDistance.Override(0f);
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
    }

    // Hooked to the settings panel's Resume/Back button.
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
    }

    private void ToggleCursorLock(bool state)
    {
        Cursor.lockState = state ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = state;
    }
    
    public void ShowEntranceDoorText()
    {
        if (DisplayingHoverText)
        {
            return;
        }

        DisplayingHoverText = true;
        if (_hoverTextCoroutine != null) StopCoroutine(_hoverTextCoroutine);
        _hoverTextCoroutine = StartCoroutine(FadeTextInAndOut(_entranceDoorText));
    }
    
    public void ShowBlockageRockText()
    {
        if (DisplayingHoverText)
        {
            return;
        }

        DisplayingHoverText = true;
        if (_hoverTextCoroutine != null) StopCoroutine(_hoverTextCoroutine);
        _hoverTextCoroutine = StartCoroutine(FadeTextInAndOut(_blockageRockHoverText));
    }

    public void ShowNormalRockHoverText()
    {
        if (DisplayingHoverText)
        {
            return;
        }

        DisplayingHoverText = true;
        if (_hoverTextCoroutine != null) StopCoroutine(_hoverTextCoroutine);
        _hoverTextCoroutine = StartCoroutine(FadeTextInAndOut(_normalRockHoverText));
    }
    
    public void ShowMineralDepositHoverText()
    {
        if (DisplayingHoverText)
        {
            return;
        }

        DisplayingHoverText = true;
        if (_hoverTextCoroutine != null) StopCoroutine(_hoverTextCoroutine);
        _hoverTextCoroutine = StartCoroutine(FadeTextInAndOut(_mineralDepositHoverText));
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

    // --- Ember ball inventory ---

    public int EmberBallsHeld => _emberBallsHeld;

    public void AddEmberBall()
    {
        _emberBallsHeld++;
    }

    // Consumes one ember ball if any are held. Returns false when the player has none.
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

        DisplayingHoverText = true;
        if (_hoverTextCoroutine != null) StopCoroutine(_hoverTextCoroutine);
        _hoverTextCoroutine = StartCoroutine(FadeTextInAndOut(_lockedDoorText));
    }

    private IEnumerator FadeTextInAndOut(TextMeshProUGUI text)
    {
        const float fadeDuration = 0.25f; 
        const float pauseDuration = 2f; 
    
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
        StartCoroutine(FadeOutAllAudio());
    }

    private IEnumerator FadeOutAllAudio()
    {
        const float fadeDuration = 3f;
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
                if (audioSources[i] != null)
                {
                    audioSources[i].volume = Mathf.Lerp(startVolumes[i], 0f, t);
                }
            }

            yield return null;
        }

        for (int i = 0; i < audioSources.Length; i++)
        {
            if (audioSources[i] != null)
            {
                audioSources[i].volume = 0f;
                audioSources[i].Stop();
            }
        }
    }
}
