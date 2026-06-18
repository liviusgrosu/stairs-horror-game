using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    private CharacterController _controller;
    [SerializeField]
    private float movementSpeed = 6f;
    private float _yVelocity;
    [SerializeField]
    private float gravity = -18f;

    // Crouching
    [Header("Crouching")]
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 8f;
    private bool _isCrouching;
    private float _standingHeight;
    private Vector3 _standingCenter;
    private float _standingCameraHeight;
    private float _crouchCameraTargetY;
    private float _currentCameraOffset;

    public bool IsCrouching => _isCrouching;

    // Sprinting
    [Header("Sprinting")]
    [SerializeField]
    private float sprintMultiplier = 1.8f;
    [SerializeField]
    private float maxSprintTime = 5f;
    [SerializeField]
    private float sprintCooldown = 6f;
    private float _sprintTimer;
    private float _cooldownTimer;
    private bool _isSprinting;
    private bool _isOnCooldown;

    public bool IsSprinting => _isSprinting;

    // Speed smoothing
    [SerializeField]
    private float speedSmoothTime = 0.15f;
    private float _currentSpeed;
    private float _speedSmoothVelocity;

    [Header("Breathing Audio")]
    [SerializeField] private float startBreathingDelay = 5f;
    [SerializeField]
    private AudioClip breathingSlowClip;
    [SerializeField]
    private AudioClip breathingHeavyClip;
    private AudioSource _breathingAudioSource;

    [Header("Head Bob")]
    [SerializeField] private float _bobAmountY = 0.05f;
    [SerializeField] private float _bobAmountX = 0.025f;
    [SerializeField] private float _bobSmooth = 10f;
    private float _bobTimer;
    private CharacterFootsteps _footsteps;

    [Header("Leaning")]
    [SerializeField] private float _leanDistance = 0.5f;
    [SerializeField] private float _leanTilt = 15f;
    [SerializeField] private float _leanSpeed = 8f;
    [SerializeField] private float _leanWallBuffer = 0.2f;
    [SerializeField] private LayerMask _leanObstacleMask = ~0;
    private float _currentLean;

    [Header("Mouse")]
    private Camera _camera;
    private float _yRotation;
    [SerializeField]
    private float mouseSensitivity = 100f;

    [Header("-DEBUG-")]
    [SerializeField]
    private bool unlimitedSprint;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _camera = Camera.main;
    
        var camEuler = _camera.transform.localEulerAngles;
        transform.Rotate(0f, camEuler.y, 0f);
        var pitch = camEuler.x;
        if (pitch > 180f) pitch -= 360f;
        _yRotation = pitch;
        _camera.transform.localRotation = Quaternion.Euler(_yRotation, 0f, 0f);

        _standingHeight = _controller.height;
        _standingCenter = _controller.center;
        _standingCameraHeight = _camera.transform.localPosition.y;

        var capsuleBottom = _standingCenter.y - _standingHeight / 2f;
        var proportion = (_standingCameraHeight - capsuleBottom) / _standingHeight;
        _crouchCameraTargetY = capsuleBottom + proportion * crouchHeight;

        _footsteps = GetComponent<CharacterFootsteps>();
        _breathingAudioSource = gameObject.AddComponent<AudioSource>();
        _breathingAudioSource.loop = true;
        _breathingAudioSource.playOnAwake = false;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (GameManager.Instance)
        {
            if (GameManager.Instance.HasWon || GameManager.Instance.InMenu || GameManager.Instance.IsPaused)
            {
                _controller.Move(Vector3.zero);
                return;
            }

            if (GameManager.Instance.HasDied)
            {
                if (_controller.isGrounded && _yVelocity < 0)
                    _yVelocity = -2f;

                _yVelocity += gravity * Time.deltaTime;
                _controller.Move(Vector3.up * (_yVelocity * Time.deltaTime));
                Look();
                return;
            }
        }
        
        MovePlayer();
        //HandleLean();
        HandleHeadBob();
        Look();
    }

    private void HandleLean()
    {
        var leanInput = 0f;
        if (Input.GetKey(KeyCode.Q))
        {
            leanInput = -1f;
        }
        else if (Input.GetKey(KeyCode.E))
        {
            leanInput = 1f;
        }

        var targetLean = leanInput;

        if (leanInput != 0f)
        {
            var leanDirection = transform.right * leanInput;
            if (Physics.Raycast(transform.position, leanDirection, out var hit, _leanDistance + _leanWallBuffer, _leanObstacleMask))
            {
                var availableDistance = hit.distance - _leanWallBuffer;
                if (availableDistance <= 0f)
                {
                    targetLean = 0f;
                }
                else
                {
                    targetLean = leanInput * (availableDistance / _leanDistance);
                }
            }
        }

        _currentLean = Mathf.Lerp(_currentLean, targetLean, _leanSpeed * Time.deltaTime);
    }

    private void HandleHeadBob()
    {
        var isMoving = _controller.velocity.magnitude > 0.1f && _controller.isGrounded;

        if (isMoving && _footsteps)
        {
            var currentInterval = _isCrouching
                ? _footsteps.crouchStepInterval
                : _isSprinting
                    ? _footsteps.sprintStepInterval
                    : _footsteps.stepInterval;

            _bobTimer += Time.deltaTime / currentInterval;
        }
        else
        {
            _bobTimer = 0f;
        }
    }

    private void Look()
    {
        var mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * mouseSensitivity;
        var mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * mouseSensitivity;

        _yRotation -= mouseY;
        _yRotation = Mathf.Clamp(_yRotation, -80f, 80f);
        _camera.transform.localRotation = Quaternion.Euler(_yRotation, 0f, -_currentLean * _leanTilt);

        var bobY = Mathf.Sin(_bobTimer * Mathf.PI * 2f) * _bobAmountY;
        var bobX = Mathf.Cos(_bobTimer * Mathf.PI) * _bobAmountX;
        
        var cameraPos = _camera.transform.localPosition;
        cameraPos.x = Mathf.Lerp(cameraPos.x, (_currentLean * _leanDistance) + bobX, _bobSmooth * Time.deltaTime);
        cameraPos.y = Mathf.Lerp(cameraPos.y, _currentCameraOffset + bobY, _bobSmooth * Time.deltaTime);
        _camera.transform.localPosition = cameraPos;

        transform.Rotate(Vector3.up * mouseX);
    }

    private void MovePlayer()
    {
        var horizontal = Input.GetAxisRaw("Horizontal");
        var vertical = Input.GetAxisRaw("Vertical");

        if (_controller.isGrounded && _yVelocity < 0)
        {
            _yVelocity = -2f;
        }

        var isMoving = horizontal != 0f || vertical != 0f;
        HandleCrouch();
        HandleSprint(isMoving);
        HandleBreathingAudio();

        var movementDir = transform.right * horizontal + transform.forward * vertical;
        if (!isMoving)
        {
            _currentSpeed = 0f;
            _speedSmoothVelocity = 0f;
        }
        else
        {
            var targetSpeed = _isCrouching ? movementSpeed * crouchSpeedMultiplier :
                          _isSprinting ? movementSpeed * sprintMultiplier : movementSpeed;
            _currentSpeed = Mathf.SmoothDamp(_currentSpeed, targetSpeed, ref _speedSmoothVelocity, speedSmoothTime);
        }
        movementDir = movementDir.normalized * _currentSpeed;

        if (Input.GetButtonDown("Jump") && _controller.isGrounded)
        {
            _yVelocity = Mathf.Sqrt(-2f * gravity);
        }

        _yVelocity += gravity * Time.deltaTime;
        var velocity = movementDir + Vector3.up * _yVelocity;
        _controller.Move(velocity * Time.deltaTime);
    }

    private void HandleCrouch()
    {
        var wantsToCrouch = Input.GetKey(KeyCode.LeftControl);

        if (wantsToCrouch && !_isCrouching)
        {
            _isCrouching = true;
            _isSprinting = false;
            _controller.height = crouchHeight;
            _controller.center = _standingCenter + Vector3.up * ((crouchHeight - _standingHeight) / 2f);
        }
        else if (!wantsToCrouch && _isCrouching)
        {
            if (!Physics.Raycast(transform.position + Vector3.up * crouchHeight, Vector3.up, _standingHeight - crouchHeight + 0.1f))
            {
                _isCrouching = false;
                _controller.height = _standingHeight;
                _controller.center = _standingCenter;
            }
        }

        var targetY = _isCrouching ? _crouchCameraTargetY : _standingCameraHeight;
        _currentCameraOffset = Mathf.Lerp(_currentCameraOffset, targetY, crouchTransitionSpeed * Time.deltaTime);

        var cameraPos = _camera.transform.localPosition;
        cameraPos.y = _currentCameraOffset;
        _camera.transform.localPosition = cameraPos;
    }

    private void HandleBreathingAudio()
    {
        if (_isOnCooldown)
        {
            if (_breathingAudioSource.clip != breathingHeavyClip)
            {
                _breathingAudioSource.clip = breathingHeavyClip;
                _breathingAudioSource.Play();
            }
        }
        else if (_sprintTimer >= startBreathingDelay)
        {
            if (_breathingAudioSource.clip != breathingSlowClip)
            {
                _breathingAudioSource.clip = breathingSlowClip;
                _breathingAudioSource.Play();
            }
        }
        else
        {
            if (_breathingAudioSource.isPlaying)
            {
                _breathingAudioSource.clip = null;
                _breathingAudioSource.Stop();
            }
        }
    }

    private void HandleSprint(bool isMoving)
    {
        if (_isOnCooldown)
        {
            _isSprinting = false;
            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer <= 0f)
            {
                _isOnCooldown = false;
                _sprintTimer = 0f;
            }
            return;
        }

        var wantsToSprint = Input.GetKey(KeyCode.LeftShift) && isMoving;

        if (wantsToSprint)
        {
            _isSprinting = true;
            _sprintTimer += Time.deltaTime;
            
            if (unlimitedSprint)
            {
                return;
            }
            
            if (_sprintTimer >= maxSprintTime)
            {
                _isSprinting = false;
                _isOnCooldown = true;
                _cooldownTimer = sprintCooldown;
            }
        }
        else
        {
            _isSprinting = false;
            _sprintTimer = Mathf.Max(0f, _sprintTimer - Time.deltaTime);
        }
    }
}
