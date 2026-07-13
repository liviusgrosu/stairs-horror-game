using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CrouchingTutorialTrigger : MonoBehaviour
{
    private static bool _triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered || !other.CompareTag("Player")) return;

        _triggered = true;
        if (GameManager.Instance)
        {
            GameManager.Instance.ShowCrouchingTutorialText();
        }
    }
}
