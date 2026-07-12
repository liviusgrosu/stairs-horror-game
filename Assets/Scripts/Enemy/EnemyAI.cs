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
        Screaming,
        GettingUp,
        Move,
        Attack,
        ReturnToHole,
        LostWait,
        WalkAway
    }

    [Header("General")]
    public bool Toggle = true;
    [Tooltip("Skip Idle/Getting Up and start already chasing the player")]
    [SerializeField] private bool _startEngaged;
    [Tooltip("When Start Engaged, despawn-on-distance only arms after the enemy gets within this range of the player")]
    [SerializeField] private float _startEngagedArmDistance = 10f;

    [Header("Chase Music")]
    [Tooltip("At/within this distance the chase music is at full volume (no ambient)")]
    [SerializeField] private float _chaseMusicNearDistance = 10f;
    [Tooltip("Seconds for the chase music to gradually fade in when first engaged, before distance takes over")]
    [SerializeField] private float _chaseMusicFadeInDuration = 2f;

    [Header("Attack State")]
    [SerializeField] private float _toPlayerRotateAttackSpeed = 500f;

    [Header("Losing The Player")]
    [Tooltip("While chasing, if the enemy reaches the closest point it can (e.g. the mouth of a furnace the player ducked into) and the player is still farther than this, it gives up.")]
    [SerializeField] private float _loseDistance = 3f;
    [Tooltip("How long the reached-but-can't-close condition must hold before the enemy commits to giving up.")]
    [SerializeField] private float _loseConfirmDuration = 0.5f;
    [Tooltip("Safety cap on how long the enemy tries to reach the player's last valid spot before giving up where it stands.")]
    [SerializeField] private float _returnToHoleTimeout = 8f;
    [Tooltip("How long the enemy lingers at the spot where it lost the player before wandering off.")]
    [SerializeField] private float _giveUpWaitDuration = 5f;
    [Tooltip("How far the enemy walks off in a random direction before despawning.")]
    [SerializeField] private float _walkAwayDistance = 30f;

    [SerializeField] private Animator animator;

    private State _currentState = State.Idle;
    private bool _pendingStartEngage;
    private bool _winStopped;
    private bool _despawnArmed = true;
    private float _chaseMusicFadeTimer;
    private float _loseConfirmTimer;
    private float _giveUpWaitTimer;
    private float _returnTimer;
    private Vector3 _walkAwayStart;
    private Vector3 _lastValidPlayerPos;
    private bool _hasLastValidPlayerPos;
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
        _audio.PlayScream();
        _audio.StopLoop();
        _currentState = State.Screaming;
    }

    public void Activate()
    {
        Toggle = true;
    }

    public void SetStartEngaged(bool value)
    {
        _startEngaged = value;
    }

    public void ApplyProfile(EnemySpawnProfile profile)
    {
        if (profile == null) return;

        _startEngaged = profile.StartEngaged;

        var movement = GetComponent<EnemyMovement>();
        if (movement) movement.SetRunSpeed(profile.RunSpeed);

        var perception = GetComponent<EnemyPerception>();
        if (perception) perception.SetEngageDistanceScale(profile.EngageDistanceScale);

        var combat = GetComponent<EnemyCombat>();
        if (combat) combat.SetInstantKill(profile.InstantKill);
    }

    public bool IsIdle => _currentState == State.Idle;

    public bool IsEngaged => _currentState != State.Idle || _pendingStartEngage;

    public void Engage()
    {
        if (!Toggle) return;
        if (_currentState != State.Idle) return;
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
        if (GameManager.Instance && GameManager.Instance.HasWon)
        {
            if (!_winStopped) StopForWin();
            return;
        }

        ReportChaseMusic();

        if (!Toggle) return;

        if (_pendingStartEngage)
        {
            _pendingStartEngage = false;
            _despawnArmed = false;
            _chaseMusicFadeTimer = 0f;
            animator.Play("Move", 0, 0f);
            _audio.PlayChaseLoop();
            _movement.RunTo(_perception.Player.position);
            _currentState = State.Move;
        }

        switch (_currentState)
        {
            case State.Screaming:
                ScreamingState();
                break;
            case State.GettingUp:
                GettingUpState();
                break;
            case State.Move:
                MoveState();
                break;
            case State.Attack:
                AttackState();
                break;
            case State.ReturnToHole:
                ReturnToHoleState();
                break;
            case State.LostWait:
                LostWaitState();
                break;
            case State.WalkAway:
                WalkAwayState();
                break;
        }
    }

    private void ScreamingState()
    {
        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (!stateInfo.IsName("Getting Up")) return;

        _chaseMusicFadeTimer = 0f;
        _currentState = State.GettingUp;
    }

    private void GettingUpState()
    {
        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (!stateInfo.IsName("Move")) return;

        _audio.PlayChaseLoop();
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

        var blockedBySafeArea = SafeArea.TryGetBoundaryPoint(transform.position, out var chaseTarget);
        if (!blockedBySafeArea)
        {
            chaseTarget = _perception.Player.position;
        }

        _movement.SetDestination(chaseTarget);

        if (_movement.TrySampleNavMesh(chaseTarget, out var validPlayerPos))
        {
            _lastValidPlayerPos = validPlayerPos;
            _hasLastValidPlayerPos = true;
        }

        if (_movement.IsBarelyMoving() && (blockedBySafeArea || distance > _loseDistance))
        {
            _loseConfirmTimer += Time.deltaTime;
            if (_loseConfirmTimer >= _loseConfirmDuration)
            {
                EnterReturnToHole();
                return;
            }
        }
        else
        {
            _loseConfirmTimer = 0f;
        }

        if (!blockedBySafeArea && distance <= _combat.AttackRange)
        {
            _movement.HardStop();
            _combat.StartAttack();
            animator.SetBool(IsAttacking, true);
            animator.Play("Attack", 0, 0f);
            _currentState = State.Attack;
        }
    }

    private void EnterReturnToHole()
    {
        _loseConfirmTimer = 0f;
        _returnTimer = 0f;
        _combat.EndAttack();
        animator.SetBool(IsAttacking, false);

        if (!_hasLastValidPlayerPos)
        {
            EnterLostWait();
            return;
        }

        _movement.Resume();
        _movement.RunTo(_lastValidPlayerPos);
        _currentState = State.ReturnToHole;
    }

    private void ReturnToHoleState()
    {
        _returnTimer += Time.deltaTime;
        if (_movement.HasReachedDestination() || _returnTimer >= _returnToHoleTimeout)
        {
            EnterLostWait();
        }
    }

    private void EnterLostWait()
    {
        _loseConfirmTimer = 0f;
        _giveUpWaitTimer = 0f;
        _combat.EndAttack();
        animator.SetBool(IsAttacking, false);
        _movement.HardStop();
        _currentState = State.LostWait;
    }

    private void LostWaitState()
    {
        _giveUpWaitTimer += Time.deltaTime;
        if (_giveUpWaitTimer < _giveUpWaitDuration) return;

        EnterWalkAway();
    }

    private void EnterWalkAway()
    {
        _walkAwayStart = transform.position;
        _movement.Resume();

        if (_movement.TryGetWalkAwayDestination(_walkAwayDistance, out var destination))
        {
            _movement.WalkTo(destination);
            animator.Play("Move", 0, 0f);
            _currentState = State.WalkAway;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void WalkAwayState()
    {
        if (CanReachPlayer())
        {
            Reengage();
            return;
        }

        var travelled = Vector3.Distance(transform.position, _walkAwayStart);
        if (travelled >= _walkAwayDistance || _movement.HasReachedDestination())
        {
            Destroy(gameObject);
        }
    }

    private bool CanReachPlayer()
    {
        if (!_perception.Player) return false;
        if (!_movement.TrySampleNavMesh(_perception.Player.position, out var onMesh)) return false;
        return _movement.HasCompletePathTo(onMesh);
    }

    private void Reengage()
    {
        _despawnArmed = false;
        _loseConfirmTimer = 0f;
        _movement.Resume();
        animator.Play("Move", 0, 0f);
        _movement.RunTo(_perception.Player.position);
        _currentState = State.Move;
    }

    private void AttackState()
    {
        if (SafeArea.PlayerInside)
        {
            _combat.EndAttack();
            animator.SetBool(IsAttacking, false);
            _movement.Resume();
            _movement.RunTo(_perception.Player.position);
            _currentState = State.Move;
            return;
        }

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

    private void StopForWin()
    {
        _winStopped = true;
        _combat.EndAttack();
        animator.SetBool(IsAttacking, false);
        _movement.HardStop();
        animator.speed = 0f;
    }

    private void ReportChaseMusic()
    {
        if (_currentState is State.Idle or State.Screaming) return;
        if (!MusicManager.Instance || _perception == null || !_perception.Player) return;

        float blend;
        if (_chaseMusicFadeTimer < _chaseMusicFadeInDuration)
        {
            _chaseMusicFadeTimer += Time.deltaTime;
            blend = _chaseMusicFadeInDuration > 0f
                ? Mathf.Clamp01(_chaseMusicFadeTimer / _chaseMusicFadeInDuration)
                : 1f;
        }
        else
        {
            blend = Mathf.InverseLerp(_perception.MaxEngageDistance, _chaseMusicNearDistance, GetDistanceFromPlayer);
        }
        MusicManager.Instance.ReportChaseBlend(blend);
    }
}
