using UnityEngine;

// This script toggles the enemy GameObject
public class EnemyGOActivationZone : MonoBehaviour
{
    [SerializeField] private GameObject enemy;
    [Tooltip("Will this enemy need to be disabled again")]
    [SerializeField] private bool _isTogglable = true;
    [Tooltip("If there's a dead body that will be destroyed to enable an active enemy")]
    [SerializeField] private GameObject propEnemy;
    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }
        if (propEnemy)
        {
            propEnemy.SetActive(false);
        }
        enemy.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") || !_isTogglable)
        {
            return;
        }
        if (propEnemy)
        {
            propEnemy.SetActive(true);
        }
        
        enemy.SetActive(false);
    }
}
