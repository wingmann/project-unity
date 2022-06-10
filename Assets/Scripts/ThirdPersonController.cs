using UnityEngine;
using UnityEngine.InputSystem;
using Wingmann.Project.InputSystem;

// Note: animations are called via the controller for both the character and capsule using animator null checks.

namespace Wingmann.Project
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.5f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 6.0f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        // Cinemachine.
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // Player.
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // Timeout deltatime.
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // Animation IDs.
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        private PlayerInput _playerInput;
        private Animator _animator;
        private CharacterController _controller;
        private Inputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;
        private bool IsCurrentDeviceMouse => _playerInput.currentControlScheme == "KeyboardMouse";

        private void Awake()
        {
            // Get a reference to our main camera.
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<Inputs>();
            _playerInput = GetComponent<PlayerInput>();

            AssignAnimationIDs();

            // Reset our timeouts on start.
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            JumpAndGravity();
            GroundedCheck();
            Move();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            // Set sphere position, with offset.
            var spherePosition = new Vector3(
                transform.position.x,
                transform.position.y - GroundedOffset,
                transform.position.z);

            Grounded = Physics.CheckSphere(
                spherePosition,
                GroundedRadius,
                GroundLayers,
                QueryTriggerInteraction.Ignore);

            // Update animator if using character.
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            // If there is an input and camera position is not fixed.
            if ((_input.look.sqrMagnitude >= _threshold) && (LockCameraPosition is false))
            {
                // Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse
                    ? 1.0f
                    : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // Clamp our rotations so our values are limited 360 degrees.
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target.
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
                _cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw,
                0.0f);
        }

        private void Move()
        {
            // Set target speed based on move speed, sprint speed and if sprint is pressed.
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // A simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon.

            // Note: Vector2's operator == uses approximation so is not floating point error prone,
            // and is cheaper than magnitude.
            // If there is no input, set the target speed to default.
            if (_input.move == Vector2.zero)
            {
                targetSpeed = default;
            }

            // a reference to the players current horizontal velocity
            var currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;

            float inputMagnitude = _input.analogMovement
                ? _input.move.magnitude
                : 1.0f;

            // Accelerate or decelerate to target speed.
            if ((currentHorizontalSpeed < targetSpeed - speedOffset) ||
                (currentHorizontalSpeed > targetSpeed + speedOffset))
            {
                // Creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed.
                _speed = Mathf.Lerp(
                    currentHorizontalSpeed,
                    targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // Round speed to 3 decimal places.
                _speed = Mathf.Round(_speed * 1_000.0f) / 1_000.0f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(
                _animationBlend,
                targetSpeed,
                Time.deltaTime * SpeedChangeRate);

            if (_animationBlend < 0.01f)
            {
                _animationBlend = default;
            }

            // Normalise input direction.
            var inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // Note: Vector2's operator != uses approximation so is not floating point error prone,
            // and is cheaper than magnitude.
            // If there is a move input rotate player when the player is moving.
            if (_input.move != Vector2.zero)
            {
                _targetRotation =
                    Mathf.Atan2(inputDirection.x, inputDirection.z) *
                    Mathf.Rad2Deg +
                    _mainCamera.transform.eulerAngles.y;

                float rotation = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y,
                    _targetRotation,
                    ref _rotationVelocity,
                    RotationSmoothTime);

                // Rotate to face input direction relative to camera position.
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            var targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // Move the player
            _controller.Move(
                targetDirection.normalized *
                (_speed * Time.deltaTime) +
                new Vector3(0.0f, _verticalVelocity, 0.0f) *
                Time.deltaTime);

            // Update animator if using character.
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // Reset the fall timeout timer.
                _fallTimeoutDelta = FallTimeout;

                // Update animator if using character.
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // Stop our velocity dropping infinitely when grounded.
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2.0f;
                }

                // Jump.
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // The square root of H * -2 * G = how much velocity needed to reach desired height.
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2.0f * Gravity);

                    // Update animator if using character.
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // Jump timeout.
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // Reset the jump timeout timer.
                _jumpTimeoutDelta = JumpTimeout;

                // Fall timeout.
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // Update animator if using character.
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // If we are not grounded, do not jump.
                _input.jump = false;
            }

            // Apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time).
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360.0f)
            {
                lfAngle += 360.0f;
            }
            else if (lfAngle > 360.0f)
            {
                lfAngle -= 360.0f;
            }

            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            var transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            var transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            Gizmos.color = Grounded
                ? transparentGreen
                : transparentRed;

            // When selected, draw a gizmo in the position of, and matching radius of, the grounded collider.
            Gizmos.DrawSphere(
                new Vector3(
                    transform.position.x,
                    transform.position.y - GroundedOffset,
                    transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);

                    AudioSource.PlayClipAtPoint(
                        FootstepAudioClips[index],
                        transform.TransformPoint(_controller.center),
                        FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(
                    LandingAudioClip,
                    transform.TransformPoint(_controller.center),
                    FootstepAudioVolume);
            }
        }
    }
}
