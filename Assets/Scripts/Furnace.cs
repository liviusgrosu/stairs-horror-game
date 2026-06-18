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

    private bool _used;
    public bool Used => _used;

    private void Awake()
    {
        if (!_animator)
        {
            _animator = GetComponent<Animator>();
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

        // Needs an ember ball to light. Without one, prompt the player and bail.
        if (GameManager.Instance && !GameManager.Instance.TryUseEmberBall())
        {
            GameManager.Instance.ShowNeedFurnaceItemText();
            return;
        }

        // Mark used immediately so the furnace can't be re-triggered mid-animation.
        _used = true;

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
            // No animator wired up; light it right away.
            OnFurnaceLit();
        }
    }

    private IEnumerator PlayLightAnimation()
    {
        _animator.SetTrigger(LightTrigger);

        // Let the animator react to the trigger this frame.
        yield return null;

        // Wait out the transition into the light state.
        while (_animator.IsInTransition(0))
        {
            yield return null;
        }

        // Wait for the light clip to finish playing.
        while (_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }

        OnFurnaceLit();
    }

    // Runs once the light animation has finished.
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
