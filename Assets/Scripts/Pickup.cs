using UnityEngine;

public class Pickup : MonoBehaviour
{
    private Transform _camera;
    [SerializeField]
    private GameObject _hoveringOver;
    private IInteractable _hoveringOverInteractable;

    public LayerMask ignoreMask;

    [SerializeField]
    private AudioSource _audioSource;

    [SerializeField]
    private AudioClip _pickupSound;

    private void Awake()
    {
        if (!_audioSource)
        {
            _audioSource = GetComponent<AudioSource>();
        }
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
            else if (_hoveringOver.CompareTag("Furnace"))
            {
                GameManager.Instance.ToggleQuestionMark(true);
            }
            else if (_hoveringOver.CompareTag("Incorrect Furnace"))
            {
                GameManager.Instance.ToggleQuestionMark(true);
            }
            else if (_hoveringOver.CompareTag("EmberBall"))
            {
                GameManager.Instance.TogglePickupIcon(true);
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
            else if (_hoveringOver.CompareTag("Furnace"))
            {
                var furnace = _hoveringOver.GetComponent<Furnace>();
                if (furnace)
                {
                    furnace.Interact();
                }
            }
            else if (_hoveringOver.CompareTag("Incorrect Furnace"))
            {
                GameManager.Instance.ShowIncorrectFurnaceText();
            }
            else if (_hoveringOver.CompareTag("EmberBall"))
            {
                var ember = _hoveringOver.GetComponent<EmberBall>();
                if (ember)
                {
                    ember.Collect();
                    if (_audioSource && _pickupSound)
                    {
                        _audioSource.PlayOneShot(_pickupSound);
                    }
                    _hoveringOver = null;
                }
            }
        }
    }
}
