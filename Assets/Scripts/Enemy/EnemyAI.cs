using UnityEngine;

[RequireComponent(typeof(EnemyPerception))]
[RequireComponent(typeof(EnemyMovement))]
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

    [Header("Chase Music")]
    [Tooltip("At/beyond this distance the chase music is silent (full ambient)")]
    [SerializeField] private float _chaseMusicFarDistance = 40f;
    [Tooltip("At/within this distance the chase music is at full volume (no ambient)")]
    [SerializeField] private float _chaseMusicNearDistance = 10f;

    [Header("Attack State")]
    [SerializeField] private float _toPlayerRotateAttackSpeed = 500f;

    [SerializeField] private Animator animator;

    private State _currentState = State.Idle;
    private bool _pendingStartEngage;
    private bool _despawnArmed = true;
    private EnemyPerception _perception;
    private EnemyMovement _movement;
    private EnemyCombat _combat;
    private EnemyAudio _audio;

    private float GetDistanceFromPlayer => Vector3.Distance(transform.position, _perception.Player.position);

    private void Awake()
    {
        _perception = GetComponent<EnemyPerception>();
        _movement = GetComponent<EnemyMovement>();
        _combat = GetComponent<EnemyCombat>();
        _audio = GetComponent<EnemyAudio>();
    }

    private void OnEnable()
    {
        if (_perception) _perception.OnStimulus += OnStimulus;
    }

    private void OnDisable()
    {
        if (_perception) _perception.OnStimulus -= OnStimulus;
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
        if (!Toggle) return;
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

    public void SetStartEngaged(bool value)
    {
        _startEngaged = value;
    }

    // True while the enemy is still dormant (spawned non-engaged and not yet disturbed).
    public bool IsIdle => _currentState == State.Idle;

    // Wakes a dormant enemy so it gets up and chases the player.
    public void Engage()
    {
        if (!Toggle) return;
        if (_currentState != State.Idle) return;
        // Engaged deliberately (e.g. by a torch) possibly from far away: don't despawn-on-distance
        // until it has closed in on the player first.
        _despawnArmed = false;
        WakeUp();
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
        ReportChaseMusic();

        if (!Toggle) return;

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

    // While engaged, crossfade the music proportionally to how close this enemy is to the player.
    private void ReportChaseMusic()
    {
        if (_currentState == State.Idle) return;
        if (!MusicManager.Instance || _perception == null || !_perception.Player) return;

        float blend = Mathf.InverseLerp(_chaseMusicFarDistance, _chaseMusicNearDistance, GetDistanceFromPlayer);
        MusicManager.Instance.ReportChaseBlend(blend);
    }
}
