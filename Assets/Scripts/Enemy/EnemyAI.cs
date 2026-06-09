using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(EnemyPerception))]
[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyCombat))]
[RequireComponent(typeof(EnemyAudio))]
public class EnemyAI : MonoBehaviour
{
    private static readonly int MovementBlend = Animator.StringToHash("MovementBlend");
    private static readonly int IsAttacking = Animator.StringToHash("IsAttacking");

    public enum State
    {
        Idle,
        Patrol,
        Engage,
        Attack,
        Check,
        Return,
        Investigate,
        Searching,
        Suspicious
    }

    [Header("General")]
    [Tooltip("Turn on/off the behaviour")]
    public bool Toggle = true;
    [Tooltip("Angle until rotation is complete")]
    [SerializeField] private float _rotationTolerance;

    [Header("Idle State")]
    [SerializeField] private float _startingRotationSpeed = 500f;

    [Header("Check State")]
    [Tooltip("How long the enemy will wait before returning to idle state")]
    [SerializeField] private float _checkStateTime = 2f;

    [Header("Searching State")]
    [SerializeField] private float _searchDuration = 8f;
    [SerializeField] private float _searchRadius = 6f;
    [SerializeField] private float _searchPauseTime = 1f;
    [SerializeField] private float _searchPointTolerance = 1f;

    [Header("Suspicious State")]
    [SerializeField] private float _suspicionDuration = 3f;
    [SerializeField] private float _suspicionRotateSpeed = 500f;
    [SerializeField] private int _suspicionEscalateCount = 2;

    [Header("Engage State")]
    [SerializeField] private float _engageGiveUpTime = 4f;
    [SerializeField] private float _engageLostSightGiveUpTime = 3f;

    [Header("Attack State")]
    [Tooltip("How fast the enemy will rotate to the player after finishing an attack")]
    [SerializeField] private float _toPlayerRotateAttackSpeed = 500f;

    [SerializeField]
    private State _initialState = State.Idle;
    private State _currentState = State.Idle;
    private EnemyPerception _perception;
    private EnemyMovement _movement;
    private EnemyHealth _health;
    private EnemyCombat _combat;
    private EnemyAudio _audio;

    private Vector3 _startingPosition;
    private Quaternion _startingRotation;
    private float _checkStateElapsedTime;
    private float _getDistanceFromPlayer => Vector3.Distance(transform.position, _perception.Player.position);
    private Vector3 _investigateTarget;
    private bool _investigatingPlayer;
    private float _lastNoiseTime;
    private float _searchElapsedTime;
    private float _searchPauseTimer;
    private bool _hasSearchPoint;
    private Vector3 _currentSearchPoint;
    private Vector3 _suspicionTarget;
    private float _suspicionElapsedTime;
    private int _suspicionStimulusCount;
    private float _engageUnreachableElapsedTime;
    private Vector3 _engageTarget;
    private bool _engagingPlayer;
    private float _lastSightTime;
    private Vector3 _lastSeenPlayerPosition;

    private bool _shouldPatrol => _initialState == State.Patrol;
    [SerializeField]
    private EnemyPathing _pathing;
    private int _currentPointIndex = 0;

    [SerializeField]
    private Animator animator;

    [Header("Decoy Engage")]
    [SerializeField] private GameObject _decoyEngageVfx;

    [Header("Debug")]
    [SerializeField] private bool neverEngage;
    // [SerializeField] private TextMeshProUGUI _stateText;
    // [SerializeField] private TextMeshProUGUI _sightStimulusText;
    // [SerializeField] private TextMeshProUGUI _hearingStimulusText;

    private void Awake()
    {
        _perception = GetComponent<EnemyPerception>();
        _movement = GetComponent<EnemyMovement>();
        _health = GetComponent<EnemyHealth>();
        _combat = GetComponent<EnemyCombat>();
        _audio = GetComponent<EnemyAudio>();
        _startingRotation = transform.rotation;
        _currentState = _initialState;
    }

    private void OnEnable()
    {
        if (!_perception)
        {
            _perception = GetComponent<EnemyPerception>();
        }
        if (!_health)
        {
            _health = GetComponent<EnemyHealth>();
        }
        if (_perception)
        {
            _perception.OnStimulus += OnStimulus;
        }
        if (_health)
        {
            _health.OnDamaged += HandleDamaged;
            _health.OnHitStunStart += HandleHitStunStart;
            _health.OnHitStunEnd += HandleHitStunEnd;
            _health.OnDied += HandleDied;
        }
    }
    
    private void OnDisable()
    {
        if (_perception)
        {
            _perception.OnStimulus -= OnStimulus;
        }
        if (_health)
        {
            _health.OnDamaged -= HandleDamaged;
            _health.OnHitStunStart -= HandleHitStunStart;
            _health.OnHitStunEnd -= HandleHitStunEnd;
            _health.OnDied -= HandleDied;
        }
    }
    
    private void Start()
    {
        _startingPosition = transform.position;

        if (Toggle && _shouldPatrol)
        {
            SetPathingDestination();
        }

        _audio.PlayIdleLoop();
    }


    private void OnStimulus(Stimulus s)
    {
        if (s.Kind == StimulusKind.Sight && s.FromPlayer)
        {
            _lastSightTime = Time.time;
            _lastSeenPlayerPosition = s.Position;
        }

        if (_health.IsDead || _health.IsTakingHit || !Toggle)
        {
            return;
        }

        if (s.Kind == StimulusKind.Sound && s.Tier != StimulusTier.Faint)
        {
            _lastNoiseTime = Time.time;
        }

        if (_currentState is State.Engage or State.Attack)
        {
            return;
        }

        if (s.Tier == StimulusTier.Strong && !neverEngage)
        {
            if (s.FromPlayer)
            {
                EnterEngage(_perception.Player.position, true);
                return;
            }
            if (s.Kind == StimulusKind.Sound)
            {
                EnterEngage(s.Position, false);
                return;
            }
        }

        if (s.Tier == StimulusTier.Moderate)
        {
            if (s.Kind == StimulusKind.Sound)
            {
                if (_currentState == State.Investigate)
                {
                    var trackingPlayerLive = _investigatingPlayer && s.FromPlayer;
                    if (!trackingPlayerLive)
                    {
                        var distToNew = Vector3.Distance(transform.position, s.Position);
                        var distToCurrent = Vector3.Distance(transform.position, _investigateTarget);
                        if (distToNew >= distToCurrent)
                        {
                            return;
                        }
                    }
                }
                EnterInvestigate(s.Position, s.FromPlayer);
                return;
            }

            if (_currentState == State.Investigate)
            {
                return;
            }
            EnterInvestigate(s.Position, s.FromPlayer);
            return;
        }

        if (s.Tier == StimulusTier.Faint)
        {
            // TODO: If post-engage, immediatly go into engage
            if (_currentState is State.Investigate or State.Searching)
            {
                EnterInvestigate(s.Position, s.FromPlayer);
                return;
            }

            EnterSuspicious(s.Position, s.FromPlayer);
        }
    }

    private void EnterEngage(Vector3 target, bool isPlayer)
    {
        _engageTarget = target;
        _engagingPlayer = isPlayer;
        if (isPlayer)
        {
            _lastSeenPlayerPosition = target;
            _lastSightTime = Time.time;
        }
        _movement.RunTo(target);
        _audio.PlayChaseLoop();
        _engageUnreachableElapsedTime = 0f;
        _currentState = State.Engage;
        SetDecoyEngageVfx(!isPlayer);
    }

    private void SetDecoyEngageVfx(bool active)
    {
        if (_decoyEngageVfx && _decoyEngageVfx.activeSelf != active)
        {
            _decoyEngageVfx.SetActive(active);
        }
    }

    public void Activate()
    {
        if (Toggle)
        {
            return;
        }
        Toggle = true;
        if (_shouldPatrol)
        {
            SetPathingDestination();
        }
    }

    public void ForceEngage()
    {
        if (_health.IsDead || !Toggle || neverEngage)
        {
            return;
        }
        if (_currentState is State.Engage or State.Attack)
        {
            return;
        }
        EnterEngage(_perception.Player.position, true);
    }

    private void EnterInvestigate(Vector3 target, bool fromPlayer)
    {
        var wasInvestigating = _currentState == State.Investigate;
        _investigateTarget = target;
        _investigatingPlayer = fromPlayer;
        _movement.WalkTo(target);
        _currentState = State.Investigate;
        if (!wasInvestigating)
        {
            _audio.PlayInvestigate();
        }
    }

    private void EnterSuspicious(Vector3 target, bool fromPlayer)
    {
        var wasSuspicious = _currentState == State.Suspicious;
        if (wasSuspicious)
        {
            _suspicionStimulusCount++;
            if (_suspicionStimulusCount >= _suspicionEscalateCount)
            {
                EnterInvestigate(target, fromPlayer);
                return;
            }
        }
        else
        {
            _suspicionElapsedTime = 0f;
            _suspicionStimulusCount = 0;
        }
        _suspicionTarget = target;
        _movement.Cancel();
        _currentState = State.Suspicious;
        if (!wasSuspicious)
        {
            _audio.PlaySuspicious();
        }
    }

    private void EnterPatrolOrReturn()
    {
        var wasAlerted = _currentState is State.Suspicious or State.Investigate or State.Searching or State.Check or State.Engage or State.Attack;
        if (_shouldPatrol)
        {
            SetPathingDestination();
            _currentState = State.Patrol;
        }
        else
        {
            _movement.WalkTo(_startingPosition);
            _currentState = State.Return;
        }
        if (wasAlerted)
        {
            _audio.PlayCalmDown();
        }
    }
    
    private void Update()
    {
        if (!Toggle || _health.IsTakingHit)
        {
            return;
        }

        //_stateText.text = _currentState.ToString();

        switch (_currentState)
        {
            case State.Idle:
                IdleState();
                break;
            case State.Patrol:
                PatrolState();
                break;
            case State.Engage:
                EngageState();
                break;
            case State.Attack:
                AttackState();
                break;
            case State.Check:
                CheckState();
                break;
            case State.Return:
                ReturnState();
                break;
            case State.Investigate:
                InvestigateState();
                break;
            case State.Searching:
                SearchingState();
                break;
            case State.Suspicious:
                SuspiciousState();
                break;
        }
    }

    private void IdleState()
    {
        animator.SetFloat(MovementBlend, 0f, 0.1f, Time.deltaTime);
        if (Quaternion.Angle(transform.rotation, _startingRotation) > _rotationTolerance)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, _startingRotation, _startingRotationSpeed * Time.deltaTime);
        }
    }

    private void PatrolState()
    {
        animator.SetFloat(MovementBlend, 0.5f, 0.1f, Time.deltaTime);

        if (_movement.HasArrived(1f))
        {
            _currentPointIndex = (_currentPointIndex + 1) % _pathing.Points.Count;
            SetPathingDestination();
        }
    }

    private void SetPathingDestination()
    {
        _movement.WalkTo(_pathing.Points[_currentPointIndex].position);
    }

    private void EngageState()
    {
        animator.SetFloat(MovementBlend, 1f, 0.1f, Time.deltaTime);

        var target = _engagingPlayer ? _lastSeenPlayerPosition : _engageTarget;
        _movement.SetDestination(target);

        if (_engagingPlayer)
        {
            var sightLostFor = Time.time - _lastSightTime;
            var noiseQuietFor = Time.time - _lastNoiseTime;
            if (sightLostFor >= _engageLostSightGiveUpTime && noiseQuietFor >= _engageLostSightGiveUpTime)
            {
                BailEngageToSearching();
                return;
            }
        }

        if (_movement.IsPathUnreachable)
        {
            _engageUnreachableElapsedTime += Time.deltaTime;
            if (_engageUnreachableElapsedTime >= _engageGiveUpTime)
            {
                BailEngageToSearching();
                return;
            }
        }
        else
        {
            _engageUnreachableElapsedTime = 0f;
        }

        if (!_engagingPlayer)
        {
            if (_movement.HasArrived(1.5f))
            {
                _movement.Halt();
                animator.SetFloat(MovementBlend, 0f, 0.1f, Time.deltaTime);
                if (Time.time - _lastNoiseTime >= 1.5f)
                {
                    BailEngageToSearching();
                }
            }
            return;
        }

        if (_getDistanceFromPlayer <= _combat.AttackRange)
        {
            _movement.HardStop();
            _combat.StartAttack();
            animator.SetBool(IsAttacking, true);
            animator.Play("Attack", 0, 0f);
            _currentState = State.Attack;
        }

        if (_getDistanceFromPlayer > _perception.MaxEngageDistance)
        {
            _movement.Cancel();
            _checkStateElapsedTime = 0f;
            _currentState = State.Check;
            _audio.PlayIdleLoop();
        }
    }

    private void BailEngageToSearching()
    {
        _investigateTarget = _engagingPlayer ? _lastSeenPlayerPosition : _engageTarget;
        _movement.Cancel();
        _searchElapsedTime = 0f;
        _searchPauseTimer = 0f;
        _hasSearchPoint = false;
        _engageUnreachableElapsedTime = 0f;
        _audio.PlayIdleLoop();
        _currentState = State.Searching;
        SetDecoyEngageVfx(false);
    }

    private void AttackState()
    {
        animator.SetFloat(MovementBlend, 0f, 0.1f, Time.deltaTime);

        var directionToPlayer = (_perception.Player.position - transform.position).normalized;
        directionToPlayer.y = 0f;
        if (directionToPlayer != Vector3.zero)
        {
            var targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _toPlayerRotateAttackSpeed * Time.deltaTime);
        }

        var cooldownReached = _combat.TickCooldown(Time.deltaTime);

        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (!stateInfo.IsName("Attack") && _getDistanceFromPlayer > _combat.AttackRange * 1.5f)
        {
            _combat.EndAttack();
            animator.SetBool(IsAttacking, false);
            _movement.Resume();
            _currentState = State.Engage;
            return;
        }

        if (cooldownReached)
        {
            _combat.ResetCooldown();
            animator.SetBool(IsAttacking, true);
            animator.Play("Attack", 0, 0f);
        }
    }

    private void CheckState()
    {
        animator.SetFloat(MovementBlend, 0f, 0.1f, Time.deltaTime);
        _checkStateElapsedTime += Time.deltaTime;

        if (_checkStateElapsedTime >= _checkStateTime)
        {
            _checkStateElapsedTime = 0f;
            EnterPatrolOrReturn();
        }
    }

    private void InvestigateState()
    {
        animator.SetFloat(MovementBlend, 0.5f, 0.1f, Time.deltaTime);
        _movement.SetDestination(_investigateTarget);

        if (_movement.HasArrived(1.5f))
        {
            var noiseStoppedTime = Time.time - _lastNoiseTime;
            if (noiseStoppedTime < 1.5f)
            {
                _movement.Halt();
                animator.SetFloat(MovementBlend, 0f, 0.1f, Time.deltaTime);
                return;
            }

            _movement.Cancel();
            _searchElapsedTime = 0f;
            _searchPauseTimer = 0f;
            _hasSearchPoint = false;
            _currentState = State.Searching;
        }
    }

    private void SearchingState()
    {
        _searchElapsedTime += Time.deltaTime;

        if (_searchElapsedTime >= _searchDuration)
        {
            EnterPatrolOrReturn();
            return;
        }

        if (!_hasSearchPoint)
        {
            animator.SetFloat(MovementBlend, 0f, 0.1f, Time.deltaTime);
            if (_movement.TrySampleRandomPoint(_investigateTarget, _searchRadius, out var newPoint))
            {
                _currentSearchPoint = newPoint;
                _movement.WalkTo(_currentSearchPoint);
                _hasSearchPoint = true;
            }
            return;
        }

        if (Vector3.Distance(transform.position, _currentSearchPoint) > _searchPointTolerance)
        {
            animator.SetFloat(MovementBlend, 0.5f, 0.1f, Time.deltaTime);
            return;
        }

        animator.SetFloat(MovementBlend, 0f, 0.1f, Time.deltaTime);
        _movement.Halt();
        _searchPauseTimer += Time.deltaTime;
        if (_searchPauseTimer >= _searchPauseTime)
        {
            _searchPauseTimer = 0f;
            _hasSearchPoint = false;
        }
    }

    private void SuspiciousState()
    {
        animator.SetFloat(MovementBlend, 0f, 0.1f, Time.deltaTime);

        var direction = _suspicionTarget - transform.position;
        direction.y = 0f;
        if (direction != Vector3.zero)
        {
            var targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _suspicionRotateSpeed * Time.deltaTime);
        }

        _suspicionElapsedTime += Time.deltaTime;
        if (_suspicionElapsedTime >= _suspicionDuration)
        {
            EnterPatrolOrReturn();
        }
    }

    private void ReturnState()
    {
        animator.SetFloat(MovementBlend, 0.5f, 0.1f, Time.deltaTime);
        _movement.SetDestination(_startingPosition);
        if (Vector3.Distance(transform.position, _startingPosition) < 0.15f)
        {
            _movement.RestoreInitialStoppingDistance();
            _currentState = State.Idle;
        }
    }

    public void FallIntoTrap(float fallSpeed, float fallDuration)
    {
        if (!Toggle)
        {
            return;
        }
        Toggle = false;
        _movement.Disable();
        SetDecoyEngageVfx(false);
        foreach (var col in GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }
        _audio.StopAll();
        if (MusicManager.Instance)
        {
            MusicManager.Instance.FadeToAmbientMusic();
        }
        StartCoroutine(FallRoutine(fallSpeed, fallDuration));
    }

    private IEnumerator FallRoutine(float fallSpeed, float fallDuration)
    {
        var elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            transform.position += Vector3.down * (fallSpeed * Time.deltaTime);
            yield return null;
        }
        Destroy(gameObject);
    }

    public void Disengage()
    {
        _combat.EndAttack();
        animator.SetBool(IsAttacking, false);
        _checkStateElapsedTime = 0f;
        _audio.PlayIdleLoop();
        EnterPatrolOrReturn();
    }

    private void HandleDamaged(int amount)
    {
        _combat.ResetCooldown();
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
        }
        else
        {
            _movement.Resume();
            _movement.SetDestination(_perception.Player.position);
            animator.CrossFadeInFixedTime("Movement", 0.15f, 0);
        }
    }

    private void HandleDied()
    {
        Toggle = false;
        _movement.Disable();
        SetDecoyEngageVfx(false);

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

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        if (_currentState is not (State.Investigate or State.Searching))
        {
            return;
        }
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(_investigateTarget, 0.4f);
    }
}
