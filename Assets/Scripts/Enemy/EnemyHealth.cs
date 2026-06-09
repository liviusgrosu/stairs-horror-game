using System;
using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private float _hitStunDuration = 0.5f;

    [Header("Blood Pool")]
    [SerializeField] private Transform _bloodPool;
    [SerializeField] private float _bloodPoolExpandTime = 3f;

    private int _currentHealth;
    private bool _isDead;
    private bool _isTakingHit;

    public bool IsDead => _isDead;
    public bool IsTakingHit => _isTakingHit;

    public event Action<int> OnDamaged;
    public event Action OnHitStunStart;
    public event Action OnHitStunEnd;
    public event Action OnDied;

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (_isDead)
        {
            return;
        }

        _currentHealth -= amount;
        OnDamaged?.Invoke(amount);

        if (_currentHealth <= 0)
        {
            _isDead = true;
            OnDied?.Invoke();
            if (_bloodPool)
            {
                StartCoroutine(ExpandBloodPool());
            }
            return;
        }

        StartCoroutine(HitStun());
    }

    private IEnumerator HitStun()
    {
        _isTakingHit = true;
        OnHitStunStart?.Invoke();
        yield return new WaitForSeconds(_hitStunDuration);
        _isTakingHit = false;
        if (!_isDead)
        {
            OnHitStunEnd?.Invoke();
        }
    }

    private IEnumerator ExpandBloodPool()
    {
        yield return new WaitForSeconds(1f);
        var elapsedTime = 0f;
        var targetScale = new Vector3(0.3f, _bloodPool.localScale.y, 0.3f);

        while (elapsedTime < _bloodPoolExpandTime)
        {
            elapsedTime += Time.deltaTime;
            var t = elapsedTime / _bloodPoolExpandTime;
            var scale = _bloodPool.localScale;
            scale.x = Mathf.Lerp(0f, targetScale.x, t);
            scale.z = Mathf.Lerp(0f, targetScale.z, t);
            _bloodPool.localScale = scale;
            yield return null;
        }

        _bloodPool.localScale = targetScale;
    }
}
