using UnityEngine;

public class EnemySwapTrigger : MonoBehaviour
{
    [SerializeField] private GameObject zombieToDisable;
    [SerializeField] private GameObject zombieToEnable;

    private bool _triggered;

    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered && !other.CompareTag("Player"))
        {
            return;
        }

        _triggered = true;

        if (zombieToDisable)
        {
            zombieToDisable.SetActive(false);
        }

        if (zombieToEnable)
        {
            zombieToEnable.SetActive(true);
        }
    }
}
