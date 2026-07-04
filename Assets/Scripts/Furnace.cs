using System.Collections;
using UnityEngine;

public class Furnace : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private GameObject _emberBall;
    [SerializeField] private float _audioFadeDuration = 2f;

    [Header("Ray Growth")]
    [SerializeField] private Transform _ray;
    [SerializeField] private float _rayTargetScaleZ = 100f;
    [SerializeField] private float _rayGrowDuration = 2f;

    [Header("Rise Audio")]
    [SerializeField] private AudioSource _riseAudioSource;
    [SerializeField] private AudioClip _riseStartSound;

    private static readonly int LightTrigger = Animator.StringToHash("Light Furnace");

    [Header("On Activate")]
    [SerializeField] private GameObject[] _activate;
    [SerializeField] private GameObject[] _inactivate;

    [Header("Debug")]
    [Tooltip("Start the furnace already activated on play, without needing an ember ball.")]
    [SerializeField] private bool _debugActivate;

    private bool _used;
    public bool Used => _used;

    private bool _playerInside;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        SetPlayerInside(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        SetPlayerInside(false);
    }

    private void OnDisable()
    {
        SetPlayerInside(false);
    }

    private void SetPlayerInside(bool inside)
    {
        if (_playerInside == inside) return;
        _playerInside = inside;

        if (FurnaceManager.Instance)
        {
            FurnaceManager.Instance.SetPlayerInsideFurnace(inside);
        }
    }

    private void Awake()
    {
        if (!_animator)
        {
            _animator = GetComponent<Animator>();
        }
    }

    private void Start()
    {
        if (_debugActivate)
        {
            Activate();
        }
    }

    public void Interact()
    {
        if (_used)
        {
            if (GameManager.Instance)
            {
                GameManager.Instance.ShowUsedFurnaceText();
            }
            return;
        }

        if (GameManager.Instance && !GameManager.Instance.TryUseEmberBall())
        {
            GameManager.Instance.ShowNeedFurnaceItemText();
            return;
        }

        Activate();
    }

    private void Activate()
    {
        if (_used) return;

        _used = true;

        foreach (var go in _activate)
            if (go) go.SetActive(true);

        foreach (var go in _inactivate)
            if (go) go.SetActive(false);

        if (_animator)
        {
            if (_emberBall)
            {
                _emberBall.SetActive(true);
            }
            StartCoroutine(PlayLightAnimation());
        }
        else
        {
            OnFurnaceLit();
        }
    }

    private IEnumerator PlayLightAnimation()
    {
        _animator.SetTrigger(LightTrigger);

        yield return null;

        while (_animator.IsInTransition(0))
        {
            yield return null;
        }

        while (_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }

        OnFurnaceLit();
    }

    public void OnFurnaceLit()
    {
        if (_ray)
        {
            StartCoroutine(GrowRay());
        }
        
        PlayRiseAudio();

        if (FurnaceManager.Instance)
        {
            FurnaceManager.Instance.NotifyFurnaceUsed();
        }
    }

    private IEnumerator GrowRay()
    {
        var startScale = _ray.localScale;
        var targetScale = new Vector3(startScale.x, startScale.y, _rayTargetScaleZ);
        var elapsed = 0f;

        while (elapsed < _rayGrowDuration)
        {
            elapsed += Time.deltaTime;
            var t = elapsed / _rayGrowDuration;
            _ray.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        _ray.localScale = targetScale;
    }

    private void PlayRiseAudio()
    {
        if (!_riseAudioSource || !_riseStartSound) return;

        _riseAudioSource.clip = _riseStartSound;
        _riseAudioSource.loop = true;
        _riseAudioSource.Play();
    }
}
