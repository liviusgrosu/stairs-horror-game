using UnityEngine;

public class Pickup : MonoBehaviour
{
    private Transform _camera;
    [SerializeField]
    private GameObject _hoveringOver;
    private IInteractable _hoveringOverInteractable;

    public LayerMask ignoreMask;

    private AudioSource _audioSource;

    [SerializeField]
    private AudioClip _pickupSound;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (!GameManager.Instance)
        {
            enabled = false;
        }
        _camera = Camera.main.transform;
    }

    private void Update()
    {
        if (Physics.Raycast(
            _camera.position, _camera.transform.forward * 1.2f, out var hit, 3.0f, ~ignoreMask))
        {
            if (hit.collider.gameObject != _hoveringOver)
            {
                GameManager.Instance.ToggleOffAllText();
                _hoveringOverInteractable?.ToggleOutline(false);
                _hoveringOver = hit.collider.gameObject;
            }

            if (_hoveringOver.CompareTag("Door"))
            {
                GameManager.Instance.ToggleQuestionMark(true);
            }
            else if (_hoveringOver.CompareTag("Torch"))
            {
                GameManager.Instance.ToggleQuestionMark(true);
            }
            else
            {
                _hoveringOver = null;
            }
        }
        else
        {
            GameManager.Instance.ToggleOffAllText();
            _hoveringOverInteractable?.ToggleOutline(false);
            _hoveringOver = null;
        }

        if (Input.GetKeyDown(KeyCode.E) && _hoveringOver)
        {
            if (_hoveringOver.CompareTag("Door"))
            {
                GameManager.Instance.ShowLockedDoorText();
            }
            else if (_hoveringOver.CompareTag("Torch"))
            {
                var torch = _hoveringOver.GetComponent<Torch>();
                if (torch)
                {
                    torch.Interact();
                }
            }
        }
    }
}
