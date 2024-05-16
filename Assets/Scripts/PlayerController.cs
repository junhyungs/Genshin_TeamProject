using System.Collections;
using System.Threading;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
#endif

[RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
[RequireComponent(typeof(PlayerInput))]
#endif
public class PlayerController : MonoBehaviour
{
    [Header("Player")]
    public float MoveSpeed = 2.0f;
    public float SprintSpeed = 5.335f;

    [Space(10)]
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;
    public float SpeedChangeRate = 10.0f;
    public float baseSensitivity = 0.12f;
    public float LookSensitivity = 1.0f;

    [Space(10)]
    public float JumpHeight = 1.2f;
    public float Gravity = -15.0f;

    [Space(10)]
    public float JumpTimeout = 0.5f;
    public float FallTimeout = 0.15f;

    [Header("Player Grounded")]
    public bool Grounded = true;
    public float GroundedOffset = -0.14f;
    public float GroundedRadius = 0.28f;
    public LayerMask GroundLayers;

    [Header("Player Climb")]
    public bool Cliff = false;
    public float CliffCheckOffsetY = 0.09f;
    public float CliffCheckOffsetZ = 0.09f;
    public float CliffCheckRadius = 0.18f;
    public LayerMask CliffLayers;

    [Header("Cinemachine")]
    public GameObject CinemachineCameraTarget;
    public float TopClamp = 70.0f;
    public float BottomClamp = -30.0f;
    public float CameraAngleOverride = 0.0f;
    public bool LockCameraPosition = false;

    //cinemachine
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    //player
    private float _speed;
    private float _animationBlend;
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;
    private bool _attackTrigger = true;
    private bool _isClimbing;
    private Vector3 lastGrabCliffDirection;

    //timeout deltatime
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    //animation IDs
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;
    private int _animIDCliffCheck;

#if ENABLE_INPUT_SYSTEM
    private PlayerInput _playerInput;
#endif

    private Animator _animator;
    private CharacterController _controller;
    private PlayerInputHandler _input;
    private GameObject _mainCamera;
    private Rigidbody _rb;

    private const float _threshold = 0.01f;

    private bool _hasAnimator;
    private bool rotateOnMove = true;

    public PlayerSO playerData;

    private void Awake()
    {
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
        _input = GetComponent<PlayerInputHandler>();
#if ENABLE_INPUT_SYSTEM
        _playerInput = GetComponent<PlayerInput>();
#endif
        _rb = GetComponent<Rigidbody>();

        AssignAnimationIDs();

        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
    }

    private void Update()
    {
        _hasAnimator = TryGetComponent(out _animator);


        JumpAndGravity();
        GroundedCheck();
        Move();
        Climb();

        //CliffCheck();

        //if (Grounded == true)
        //{
        //    Move();
        //}

        //if (Cliff == true)
        //{
        //    Climb();
        //}

        if (_input.attack)
        {
            if (_attackTrigger)
            {
                Attack();
                _attackTrigger = false;
            }
        }
        else
        {
            _attackTrigger = true;
        }
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("MoveSpeed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        _animIDCliffCheck = Animator.StringToHash("Cliff");
    }

    private void GroundedCheck()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
            transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
            QueryTriggerInteraction.Ignore);

        if (_hasAnimator)
        {
            _animator.SetBool(_animIDGrounded, Grounded);
        }
    }

    //private void CliffCheck()
    //{
    //    Vector3 spherePosiiton = new Vector3(transform.position.x, transform.position.y + CliffCheckOffsetY,
    //        transform.position.z + CliffCheckOffsetZ);

    //    Cliff = Physics.CheckSphere(spherePosiiton, CliffCheckRadius, CliffLayers,
    //        QueryTriggerInteraction.Ignore);

    //    if (_hasAnimator)
    //    {
    //        _animator.SetBool(_animIDCliffCheck, Cliff);

    //        if (Cliff)
    //        {
    //            Grounded = false;
    //            transform.rotation = Quaternion.identity;
    //            _animator.SetBool("Climb", true);
    //            _animator.SetBool(_animIDGrounded, false);
    //            _animator.SetBool(_animIDFreeFall, false);
    //        }
    //    }
    //}

    private void CameraRotation()
    {
        if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            _cinemachineTargetYaw += _input.look.x * baseSensitivity * LookSensitivity;
            _cinemachineTargetPitch += _input.look.y * baseSensitivity * LookSensitivity;
        }
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
            _cinemachineTargetYaw, 0.0f);

    }

    private void Move()
    {
        float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

        if (_input.move == Vector2.zero) targetSpeed = 0.0f;

        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = 1.0f;

        if (currentHorizontalSpeed < targetSpeed - speedOffset ||
            currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                Time.deltaTime * SpeedChangeRate);

            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);

        Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

        if (_input.move != Vector2.zero)
        {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                _mainCamera.transform.eulerAngles.y;

            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                RotationSmoothTime);
            if (rotateOnMove) transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
        

    //    if(!_isClimbing)
    //    {
    //        float avoidFloorDistance = 0.1f;
    //        float cliffGrabDistance = 0.3f;
    //        if (Physics.Raycast(transform.position + Vector3.up * avoidFloorDistance, targetDirection, out RaycastHit raycastHit, cliffGrabDistance))
    //        {
    //            if (raycastHit.transform.TryGetComponent(out Cliff cliff))
    //            {
    //                GrabCliff(targetDirection);

    //                if (_hasAnimator)
    //                {
    //                    _animator.SetBool("Climb", true);
    //                }

    //                transform.rotation = Quaternion.Euler(0.0f, _mainCamera.transform.eulerAngles.y, 0.0f);
    //            }
    //        }
    //    }
    //    else
    //    {
    //        float avoidFloorDistance = 0.1f;
    //        float cliffGrabDistance = 0.3f;
    //        if (Physics.Raycast(transform.position + Vector3.up * avoidFloorDistance, lastGrabCliffDirection, out RaycastHit raycastHit, cliffGrabDistance))
    //        {
    //            if (!raycastHit.transform.TryGetComponent(out Cliff cliff))
    //            {
    //                DropCliff();
    //                _verticalVelocity = 2f;
    //            }
    //        }
    //        else
    //        {
    //            DropCliff();
    //            _verticalVelocity = 2f;
    //        }

    //        if(Vector3.Dot(targetDirection, lastGrabCliffDirection) < 0)
    //        {
    //            float cliffFloorDropDistance = 0.1f;
    //            if(Physics.Raycast(transform.position, Vector3.down, out RaycastHit floorRaycastHit, cliffFloorDropDistance))
    //            {
    //                DropCliff();
    //            }
    //        }
    //    }

        
    //    if (_isClimbing)
    //    {
    //        targetDirection.x = 0.0f;
    //        targetDirection.y = targetDirection.z;
    //        targetDirection.z = 0.0f;
    //        _verticalVelocity = 0.0f;
    //        Grounded = true;
    //        _speed = targetSpeed;
    //    }

        _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
            new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

        if (_hasAnimator)
        {
            _animator.SetFloat(_animIDSpeed, _animationBlend, 0.05f, Time.deltaTime);
            _animator.SetFloat(_animIDMotionSpeed, inputMagnitude, 0.1f, Time.deltaTime);
        }
    }

    //private void GrabCliff(Vector3 lastGrabCliffDirection)
    //{
    //    _isClimbing = true;
    //    _animator.SetBool("Climb", _isClimbing);
    //    this.lastGrabCliffDirection = lastGrabCliffDirection;
    //}

    //private void DropCliff()
    //{
    //    _isClimbing = false;
    //    _animator.SetBool("Climb", _isClimbing);
    //}

    private void Climb()
    {
        Vector3 spherePosiiton = new Vector3(transform.position.x, transform.position.y + CliffCheckOffsetY,
            transform.position.z + CliffCheckOffsetZ);

        Cliff = Physics.CheckSphere(spherePosiiton, CliffCheckRadius, CliffLayers,
            QueryTriggerInteraction.Ignore);

        if (Cliff)
        {
            Grounded = false;
            _animator.SetBool("Climb", true);

            float verticalInput = _input.move.y;
            float horizontalInput = _input.move.x;

            Vector3 moveDirection = new Vector3(horizontalInput, verticalInput, 0.0f);

            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection.y = Mathf.Clamp(moveDirection.y, -1f, 1f);

            _controller.Move(moveDirection * MoveSpeed * Time.deltaTime);

            if (_hasAnimator)
            {
                _animator.SetFloat("horizontal", horizontalInput);
                _animator.SetFloat("vertical", verticalInput);
            }
        }
        else
        {
            _animator.SetBool("Climb", false);
        }

        //if (_hasAnimator)
        //{
        //    _animator.SetBool(_animIDCliffCheck, Cliff);

        //    if (Cliff)
        //    {
        //        Grounded = false;
        //        transform.rotation = Quaternion.identity;
        //        _animator.SetBool("Climb", true);
        //        _animator.SetBool(_animIDGrounded, false);
        //        _animator.SetBool(_animIDFreeFall, false);
        //    }
        //}
        //if (Cliff == true)
        //{
        //    _rb.constraints = RigidbodyConstraints.FreezeRotationZ;

        //    float verticalInput = _input.move.y;
        //    float horizontalInput = _input.move.x;

        //    Vector3 moveDirection = new Vector3(horizontalInput, verticalInput, 0.0f);

        //    moveDirection = _mainCamera.transform.TransformDirection(moveDirection);
        //    moveDirection.y = Mathf.Clamp(moveDirection.y, -1f, 1f);

        //    _controller.Move(moveDirection * (MoveSpeed * Time.deltaTime));

        //    if (_hasAnimator)
        //    {
        //        _animator.SetFloat("horizontal", horizontalInput);
        //        _animator.SetFloat("vertical", verticalInput);
        //    }
        //}
        //else
        //{
        //    _animator.SetBool("Climb", false);
        //}
    }

    private void JumpAndGravity()
    {
        if (Grounded)
        {
            _fallTimeoutDelta = FallTimeout;

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }

            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2.0f;
            }

            if (_input.jump && _jumpTimeoutDelta <= 0.0f)
            {
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, true);
                }
            }

            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            _jumpTimeoutDelta = JumpTimeout;

            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDFreeFall, true);
                }
            }
            _input.jump = false;
        }

        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }

        //if (_isClimbing)
        //{
        //    if(_hasAnimator)
        //    {
        //        _animator.SetTrigger("ClimbDash");
        //    }
        //}
    }

    

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void Attack()
    {
        if (_hasAnimator)
        {
            _animator.SetTrigger("Attack");
            _animator.SetBool("Attacking", true);
        }
        else
        {
            _animator.SetBool("Attacking", false);
        }

    }

    //private void OnDrawGizmos()
    //{
    //    Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y + CliffCheckOffsetY,
    //        transform.position.z + CliffCheckOffsetZ);

    //    Gizmos.color = Color.yellow;

    //    Gizmos.DrawSphere(spherePosition, CliffCheckRadius);
    //}
}
