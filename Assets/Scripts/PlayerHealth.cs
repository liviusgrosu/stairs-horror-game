using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public struct HealthStatus
{
    public Sprite sprite;
    public string text;
}

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance;
    
    public int MaxHealth = 100;
    private int _currentHealth;

    [Header("Regen")]
    [SerializeField] private float _regenPerSecond = 2f;
    [SerializeField] private float _regenDelayAfterDamage = 5f;
    private float _regenBuffer;
    private float _timeSinceLastDamage;

    [SerializeField] private Image _damageVignette;

    [Header("Hit Flash")]
    [SerializeField] private Image _hitFlashImage;
    [SerializeField] private AnimationCurve _hitFlashCurve;
    [SerializeField] private float _hitFlashDuration = 0.5f;
    private Coroutine _hitFlashCoroutine;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip[] gettingHitSFX;
    [SerializeField] private AudioClip deathSFX;
    [SerializeField] private AudioClip knuckleCrackSFX;
    private AudioSource _audioSource;

    [Header("Health Status UI")]
    [SerializeField] private Image _healthStatusImage;
    [SerializeField] private TMP_Text _healthStatusText;
    [SerializeField] private HealthStatus _healthy;
    [SerializeField] private HealthStatus _damaged;
    [SerializeField] private HealthStatus _dying;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _audioSource = GetComponent<AudioSource>();
    }
    
    private void Start()
    {
        if (DebugManager.IsUnlimitedHealth)
        {
            MaxHealth = DebugManager.UnlimitedHealthAmount;
        }

        _currentHealth = MaxHealth;
        UpdateVignette();
        UpdateHealthStatus();
    }

    private void Update()
    {
        if (DebugManager.IsKillKeyEnabled && Input.GetKeyDown(KeyCode.F))
        {
            TakeDamage(_currentHealth);
        }

        if (DebugManager.IsDamageKeyEnabled && Input.GetKeyDown(KeyCode.P))
        {
            TakeDamage(DebugManager.DamageKeyAmount);
        }

        HandleRegen();
    }

    private void HandleRegen()
    {
        if (_currentHealth <= 0 || _currentHealth >= MaxHealth)
        {
            return;
        }

        _timeSinceLastDamage += Time.deltaTime;
        if (_timeSinceLastDamage < _regenDelayAfterDamage)
        {
            return;
        }

        _regenBuffer += _regenPerSecond * Time.deltaTime;
        if (_regenBuffer < 1f)
        {
            return;
        }

        int whole = Mathf.FloorToInt(_regenBuffer);
        _regenBuffer -= whole;
        _currentHealth = Mathf.Min(_currentHealth + whole, MaxHealth);
        UpdateVignette();
        UpdateHealthStatus();
    }

    public void TakeDamage(int amount)
    {
        if (_currentHealth <= 0)
        {
            return;
        }

        _currentHealth = Mathf.Max(_currentHealth - amount, 0);
        _timeSinceLastDamage = 0f;
        Debug.Log($"Player health: {_currentHealth}/{MaxHealth}");
        UpdateVignette();
        UpdateHealthStatus();

        if (CameraHitEffect.Instance)
        {
            CameraHitEffect.Instance.ApplyHitRotation();
        }

        if (_currentHealth <= 0)
        {
            if (deathSFX && _audioSource)
            {
                _audioSource.PlayOneShot(deathSFX);
            }
            GameManager.Instance.OpenGameOverScreen();
            enabled = false;
            return;
        }
        
        PlayHitSound();
        PlayHitFlash();
    }

    public void TakeFallDamage(int amount)
    {
        if (_currentHealth <= 0)
        {
            return;
        }

        _currentHealth = Mathf.Max(_currentHealth - amount, 0);
        _timeSinceLastDamage = 0f;
        UpdateVignette();
        UpdateHealthStatus();

        if (CameraHitEffect.Instance)
        {
            CameraHitEffect.Instance.ApplyDownwardHit();
        }

        if (knuckleCrackSFX && _audioSource)
        {
            _audioSource.PlayOneShot(knuckleCrackSFX);
        }

        if (_currentHealth <= 0)
        {
            if (deathSFX && _audioSource)
            {
                _audioSource.PlayOneShot(deathSFX);
            }
            GameManager.Instance.OpenGameOverScreen();
            enabled = false;
        }
    }

    public void Heal(int amount)
    {
        _currentHealth = Mathf.Min(_currentHealth + amount, MaxHealth);
        UpdateVignette();
        UpdateHealthStatus();
    }

    private void PlayHitSound()
    {
        if (gettingHitSFX.Length == 0 || !_audioSource)
        {
            return;
        }
        _audioSource.PlayOneShot(gettingHitSFX[Random.Range(0, gettingHitSFX.Length)]);
    }

    private void UpdateHealthStatus()
    {
        if (!_healthStatusImage)
        {
            return;
        }

        var status = _currentHealth switch
        {
            >= 66 => _healthy,
            >= 33 => _damaged,
            _ => _dying
        };

        _healthStatusImage.sprite = status.sprite;
        if (_healthStatusText)
        {
            _healthStatusText.text = status.text;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        /*if (other.CompareTag("Spiked Trap"))
        {
            TakeDamage(_currentHealth);
        }*/
        if (other.CompareTag("Pit"))
        {
            GameManager.Instance.OpenPitDeathScreen();
        }
    }

    private void PlayHitFlash()
    {
        if (!_hitFlashImage) return;

        if (_hitFlashCoroutine != null)
            StopCoroutine(_hitFlashCoroutine);

        _hitFlashCoroutine = StartCoroutine(HitFlashRoutine());
    }

    private System.Collections.IEnumerator HitFlashRoutine()
    {
        var color = _hitFlashImage.color;
        var elapsedTime = 0f;

        while (elapsedTime < _hitFlashDuration)
        {
            elapsedTime += Time.deltaTime;
            var alpha = _hitFlashCurve.Evaluate(elapsedTime / _hitFlashDuration);
            _hitFlashImage.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        _hitFlashImage.color = new Color(color.r, color.g, color.b, 0f);
        _hitFlashCoroutine = null;
    }

    private void UpdateVignette()
    {
        if (!_damageVignette)
        {
            return;
        }

        var healthPercent = (float)_currentHealth / MaxHealth;
        var alpha = healthPercent >= 0.5f ? 0f : Mathf.Lerp(0.8f, 0f, healthPercent / 0.5f);
        _damageVignette.color = new Color(1f, 1f, 1f, alpha);
    }
}
