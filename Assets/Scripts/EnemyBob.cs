using UnityEngine;

public class EnemyBob : MonoBehaviour
{
    public float amplitude = 0.25f;
    public float frequency = 1f;

    private static readonly int MoveState = Animator.StringToHash("Move");

    private Vector3 _startPos;
    private Animator _animator;

    private void Start()
    {
        _startPos = transform.localPosition;
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (!IsMoving())
        {
            transform.localPosition = _startPos;
            return;
        }

        var offset = Mathf.Sin(Time.time * frequency) * amplitude;
        transform.localPosition = _startPos + Vector3.up * offset;
    }

    private bool IsMoving()
    {
        if (!_animator) return false;
        return _animator.GetCurrentAnimatorStateInfo(0).shortNameHash == MoveState;
    }
}
