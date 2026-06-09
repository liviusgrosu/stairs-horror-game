using System.Collections;
using UnityEngine;

public class EnemyAIActivationZone : MonoBehaviour
{
    [SerializeField] private EnemyAI enemy;
    [SerializeField] private float _activationDelay;

    private bool _triggered;

    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered)
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        _triggered = true;
        StartCoroutine(ActivateAfterDelay());
    }

    private IEnumerator ActivateAfterDelay()
    {
        yield return new WaitForSeconds(_activationDelay);
        enemy.Activate();
    }
}
