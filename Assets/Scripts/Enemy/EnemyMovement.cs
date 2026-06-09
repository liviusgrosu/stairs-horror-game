using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private float _walkSpeed = 1f;
    [SerializeField] private float _runSpeed = 2.5f;
    [SerializeField] private float _angularSpeed = 500f;

    [Header("Debug")]
    [SerializeField] private bool _stayInPlace;

    private NavMeshAgent _agent;
    private float _initialStoppingDistance;

    public float WalkSpeed => _walkSpeed;
    public float RunSpeed => _runSpeed;
    public Vector3 Destination => _agent.destination;
    public float StoppingDistance => _agent.stoppingDistance;
    public bool IsPathUnreachable => _agent.pathStatus != NavMeshPathStatus.PathComplete;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.angularSpeed = _angularSpeed;
        _initialStoppingDistance = _agent.stoppingDistance;
        if (_stayInPlace)
        {
            _walkSpeed = 0f;
        }
    }

    public void WalkTo(Vector3 position)
    {
        _agent.isStopped = false;
        _agent.speed = _walkSpeed;
        _agent.stoppingDistance = 0f;
        _agent.SetDestination(position);
    }

    public void RunTo(Vector3 position)
    {
        _agent.isStopped = false;
        _agent.speed = _runSpeed;
        _agent.SetDestination(position);
    }

    public void SetDestination(Vector3 position)
    {
        _agent.SetDestination(position);
    }

    public void Halt()
    {
        _agent.isStopped = true;
    }

    public void HardStop()
    {
        _agent.velocity = Vector3.zero;
        _agent.isStopped = true;
    }

    public void Cancel()
    {
        _agent.isStopped = true;
        _agent.ResetPath();
    }

    public void Resume()
    {
        _agent.isStopped = false;
    }

    public void RestoreInitialStoppingDistance()
    {
        _agent.stoppingDistance = _initialStoppingDistance;
    }

    public bool HasArrived(float tolerance = 1f)
    {
        return Vector3.Distance(transform.position, _agent.destination) <= _agent.stoppingDistance + tolerance;
    }

    public void Disable()
    {
        _agent.isStopped = true;
        _agent.enabled = false;
    }

    public bool TrySampleRandomPoint(Vector3 origin, float radius, out Vector3 point, int attempts = 8)
    {
        for (var i = 0; i < attempts; i++)
        {
            var offset = Random.insideUnitSphere * radius;
            offset.y = 0f;
            var candidate = origin + offset;
            if (NavMesh.SamplePosition(candidate, out var hit, radius, NavMesh.AllAreas))
            {
                point = hit.position;
                return true;
            }
        }
        point = Vector3.zero;
        return false;
    }
}
