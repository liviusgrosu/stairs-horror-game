using UnityEngine;

public class FalseFurnace : MonoBehaviour
{
    [SerializeField] private bool _triggerOnce = true;
    private bool _triggered;
    private bool _playerInside;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        SetPlayerInside(true);

        if (_triggerOnce && _triggered) return;
        _triggered = true;

        if (FurnaceManager.Instance)
        {
            FurnaceManager.Instance.RequestAdvanceEncounter();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        SetPlayerInside(false);
    }

    private void OnDisable()
    {
        SetPlayerInside(false);
    }

    private void SetPlayerInside(bool inside)
    {
        if (_playerInside == inside) return;
        _playerInside = inside;

        if (FurnaceManager.Instance)
        {
            FurnaceManager.Instance.SetPlayerInsideFurnace(inside);
        }
    }
}
