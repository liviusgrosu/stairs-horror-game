using UnityEngine;

[RequireComponent(typeof(EnemyPerception))]
[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyCombat))]
[RequireComponent(typeof(EnemyAudio))]
public class EnemyAI : MonoBehaviour
{
    private static readonly int GetUpTrigger = Animator.StringToHash("Get Up");
    private static readonly int IsAttacking = Animator.StringToHash("IsAttacking");

    public enum State
    {
        Idle,
        GettingUp,
        Move,
        Attack
    }

    [Header("General")]
    public bool Toggle = true;
    [Tooltip("Skip Idle/Getting Up and start already chasing the player")]
    [SerializeField] private bool _startEngaged;
    [Tooltip("When Start Engaged, despawn-on-distance only arms after the enemy gets within this range of the player")]
    [SerializeField] private float _startEngagedArmDistance = 10f;

    [Header("Attack State")]
    [SerializeField] private float _toPlayerRotateAttackSpeed = 500f;

    [SerializeField] private Animator animator;

    private State _currentState = State.Idle;
    private bool _pendingStartEngage;
    private bool _despawnArmed = true;
    private EnemyPerception _perception;
    private EnemyMovement _movement;
    private EnemyHealth _health;
    private EnemyCombat _combat;
    private EnemyAudio _audio;

    private float GetDistanceFromPlayer => Vector3.Distance(transform.position, _perception.Player.position);

    private void Awake()
    {
        _perception = GetComponent<EnemyPerception>();
        _movement = GetComponent<EnemyMovement>();
        _health = GetComponent<EnemyHealth>();
        _combat = GetComponent<EnemyCombat>();
        _audio = GetComponent<EnemyAudio>();
    }

    private void OnEnable()
    {
        if (_perception) _perception.OnStimulus += OnStimulus;
        if (_health)
        {
            _health.OnHitStunStart += HandleHitStunStart;
            _health.OnHitStunEnd += HandleHitStunEnd;
            _health.OnDied += HandleDied;
        }
    }

    private void OnDisable()
    {
        if (_perception) _perception.OnStimulus -= OnStimulus;
        if (_health)
        {
            _health.OnHitStunStart -= HandleHitStunStart;
            _health.OnHitStunEnd -= HandleHitStunEnd;
            _health.OnDied -= HandleDied;
        }
    }

    private void Start()
    {
        if (_startEngaged)
        {
            _audio.PlayChaseLoop();
            _pendingStartEngage = true;
        }
        else
        {
            _audio.PlayIdleLoop();
        }
    }

    private void OnStimulus(Stimulus s)
    {
        if (!Toggle || _health.IsDead || _health.IsTakingHit) return;
        if (_currentState != State.Idle) return;
        if (!s.FromPlayer) return;

        WakeUp();
    }

    private void WakeUp()
    {
        _movement.Cancel();
        animator.SetTrigger(GetUpTrigger);
        _audio.PlayChaseLoop();
        _currentState = State.GettingUp;
    }

    public void Activate()
    {
        Toggle = true;
    }

    public void Disengage()
    {
        _combat.EndAttack();
        animator.SetBool(IsAttacking, false);
        _movement.Cancel();
        _audio.PlayIdleLoop();
        _currentState = State.Idle;
    }

    private void Update()
    {
        if (!Toggle || _health.IsTakingHit) return;

        if (_pendingStartEngage)
        {
            _pendingStartEngage = false;
            _despawnArmed = false;
            animator.Play("Move", 0, 0f);
            _movement.RunTo(_perception.Player.position);
            _currentState = State.Move;
        }

        switch (_currentState)
        {
            case State.GettingUp:
                GettingUpState();
                break;
            case State.Move:
                MoveState();
                break;
            case State.Attack:
                AttackState();
                break;
        }
    }

    private void GettingUpState()
    {
        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (!stateInfo.IsName("Move")) return;

        _movement.RunTo(_perception.Player.position);
        _currentState = State.Move;
    }

    private void MoveState()
    {
        var distance = GetDistanceFromPlayer;

        if (!_despawnArmed && distance <= _startEngagedArmDistance)
        {
            _despawnArmed = true;
        }

        if (_despawnArmed && distance > _perception.MaxEngageDistance)
        {
            Destroy(gameObject);
            return;
        }

        _movement.SetDestination(_perception.Player.position);

        if (distance <= _combat.AttackRange)
        {
            _movement.HardStop();
            _combat.StartAttack();
            animator.SetBool(IsAttacking, true);
            animator.Play("Attack", 0, 0f);
            _currentState = State.Attack;
        }
    }

    private void AttackState()
    {
        var directionToPlayer = (_perception.Player.position - transform.position).normalized;
        directionToPlayer.y = 0f;
        if (directionToPlayer != Vector3.zero)
        {
            var targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _toPlayerRotateAttackSpeed * Time.deltaTime);
        }

        var cooldownReached = _combat.TickCooldown(Time.deltaTime);

        if (GetDistanceFromPlayer > _combat.AttackRange * 1.5f)
        {
            _combat.EndAttack();
            animator.SetBool(IsAttacking, false);
            _movement.Resume();
            _movement.RunTo(_perception.Player.position);
            _currentState = State.Move;
            return;
        }

        if (cooldownReached)
        {
            _combat.ResetCooldown();
            animator.SetBool(IsAttacking, true);
            animator.Play("Attack", 0, 0f);
        }
    }

    private void HandleHitStunStart()
    {
        _movement.HardStop();
        _audio.PlayHurt();
        animator.CrossFadeInFixedTime("Take Hit", 0.1f, 0);
    }

    private void HandleHitStunEnd()
    {
        if (_currentState == State.Attack)
        {
            _combat.ResetCooldown();
            animator.SetBool(IsAttacking, true);
            animator.Play("Attack", 0, 0f);
            return;
        }

        _movement.Resume();
        _movement.SetDestination(_perception.Player.position);
    }

    private void HandleDied()
    {
        Toggle = false;
        _movement.Disable();

        foreach (var col in GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        _audio.StopAll();
        _audio.PlayDie();
        if (MusicManager.Instance)
        {
            MusicManager.Instance.FadeToAmbientMusic();
        }
        animator.Play("Die", 0, 0f);
    }
}
