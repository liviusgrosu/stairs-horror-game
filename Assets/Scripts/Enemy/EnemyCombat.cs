using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    [SerializeField] private int _attackDamage = 20;
    [SerializeField] private float _attackCooldown = 2f;
    [SerializeField] private float _attackRange = 2f;
    [SerializeField] private Collider _damageCollider;

    private float _cooldownTimer;
    private bool _isAttacking;
    private bool _instantKill;

    public float AttackRange => _attackRange;
    public bool IsAttacking => _isAttacking;

    public void SetInstantKill(bool value)
    {
        _instantKill = value;
    }

    public void StartAttack()
    {
        _isAttacking = true;
        _cooldownTimer = 0f;
    }

    public void EndAttack()
    {
        _isAttacking = false;
    }

    public void ResetCooldown()
    {
        _cooldownTimer = 0f;
    }

    public bool TickCooldown(float deltaTime)
    {
        _cooldownTimer += deltaTime;
        return _cooldownTimer >= _attackCooldown;
    }

    public void EnableDamageCollider()
    {
        if (_damageCollider)
        {
            _damageCollider.enabled = true;
        }
    }

    public void DisableDamageCollider()
    {
        if (_damageCollider)
        {
            _damageCollider.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (PlayerHealth.Instance)
        {
            if (_instantKill)
            {
                if (FurnaceManager.Instance)
                {
                    FurnaceManager.Instance.NotifyInstantDeathOccurred();
                }
                PlayerHealth.Instance.TakeDamage(PlayerHealth.Instance.MaxHealth);
            }
            else
            {
                PlayerHealth.Instance.TakeDamage(_attackDamage);
            }
        }

        if (_damageCollider)
        {
            _damageCollider.enabled = false;
        }
    }
}
