using UnityEngine;

public class Statue : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private GameObject _idleCollider;
    [SerializeField] private GameObject _prayingCollider;

    private static readonly int PrayTrigger = Animator.StringToHash("Pray");

    private void Awake()
    {
        if (!_animator)
        {
            _animator = GetComponent<Animator>();
        }
    }

    public void Pray()
    {
        if (_animator)
        {
            _animator.SetTrigger(PrayTrigger);
        }

        if (_idleCollider)
        {
            _idleCollider.SetActive(false);
        }

        if (_prayingCollider)
        {
            _prayingCollider.SetActive(true);
        }
    }
}
