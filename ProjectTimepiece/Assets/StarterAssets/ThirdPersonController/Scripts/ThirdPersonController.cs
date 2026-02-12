using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        public float MoveSpeed = 2.0f;
        public float SprintSpeed = 5.335f;
        [Range(0.0f, 0.3f)] public float RotationSmoothTime = 0.12f;
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        public float JumpHeight = 1.2f;
        public float Gravity = -15.0f;
        public float JumpTimeout = 0.50f;
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        public bool Grounded = true;
        public float GroundedOffset = -0.14f;
        public float GroundedRadius = 0.28f;
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        public GameObject CinemachineCameraTarget;
        public float TopClamp = 70.0f;
        public float BottomClamp = -30.0f;
        public float CameraAngleOverride = 0.0f;
        public bool LockCameraPosition = false;

        [Header("Camera Settings")]
        public Vector3 BattleCameraOffset = new Vector3(0, 2f, -4f);


        [Header("Battle System")]
        public GameObject BattleDomePrefab;
        public GameObject BattlePlayerPrefab;

        public Transform PlayerArmature;
        public Transform NPCArmature;
        public Transform BattleBot;   // This is the transform inside BattlePlayerPrefab that the camera should follow

        public MonoBehaviour NPCMovementScript;   // drag your NPC movement script here in inspector


        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        private float _speed;
        private float _animationBlend;
        private float _targetRotation;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private GameObject _activeBattleDome;
        private GameObject _activeBattlePlayer;

        private bool _inBattle = false;

        private const float _threshold = 0.01f;
        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse =>
#if ENABLE_INPUT_SYSTEM
            _playerInput.currentControlScheme == "KeyboardMouse";
#else
            false;
#endif

        private void Awake()
        {
            if (!CompareTag("Player"))
            {
                Debug.LogWarning("ThirdPersonController should be on an object tagged 'Player'");
            }

            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();

#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#endif

            AssignAnimationIDs();

            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            // Always allow grounding logic even during battle
            GroundedCheck();

            if (_inBattle) return;

            _hasAnimator = TryGetComponent(out _animator);

            JumpAndGravity();
            Move();
        }


        private void LateUpdate()
        {
            if (_inBattle) return;
            CameraRotation();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_inBattle) return;

            // Check root object for NPC tag
            Transform root = other.transform.root;

            if (root.CompareTag("NPC"))
            {
                StartBattle(root);
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (_inBattle) return;

            Transform root = hit.transform.root;

            if (!root.CompareTag("NPC")) return;

            StartBattle(root);
        }




        private void DisablePlayerArmatureControl()
        {
            if (PlayerArmature == null) return;

            if (PlayerArmature.TryGetComponent(out CharacterController cc))
                cc.enabled = false;

#if ENABLE_INPUT_SYSTEM
            if (PlayerArmature.TryGetComponent(out PlayerInput input))
                input.enabled = false;
#endif
        }

        private void EnableBattleBotControls()
        {
            if (BattleBot == null) return;

            if (BattleBot.TryGetComponent(out CharacterController cc))
                cc.enabled = true;

#if ENABLE_INPUT_SYSTEM
            if (BattleBot.TryGetComponent(out PlayerInput input))
                input.enabled = true;
#endif
        }


        private void MovePlayerOutsideBattleDome(Vector3 battleCenter)
        {
            if (_controller == null) return;

            Vector3 directionFromCenter = (transform.position - battleCenter).normalized;

            if (directionFromCenter == Vector3.zero)
                directionFromCenter = transform.forward;

            float safeDistance = 6f;

            Vector3 safePosition = battleCenter + directionFromCenter * safeDistance;

            // Cast down to find ground
            RaycastHit hit;
            if (Physics.Raycast(safePosition + Vector3.up * 10f, Vector3.down, out hit, 50f, GroundLayers))
            {
                // IMPORTANT: place player SLIGHTLY ABOVE ground to avoid embedding
                safePosition.y = hit.point.y + 0.92f;
            }
            else
            {
                safePosition.y = transform.position.y + 0.2f;
            }

            // --- HARD RESET SEQUENCE ---

            // 1. Disable controller completely
            _controller.enabled = false;

            // 2. Move transform directly
            transform.position = safePosition;

            // 3. Reset all movement forces
            _verticalVelocity = 0f;
            _speed = 0f;

            // 4. Re-enable controller
            _controller.enabled = true;

            // 5. FORCE an immediate grounded refresh
            Grounded = true;

            // 6. Manually run one grounded check so it stabilizes
            GroundedCheck();
        }


        private void StartBattle(Transform npcTransform)
        {
            _inBattle = true;

            // STOP THE NPC CORRECTLY
            var npcController = npcTransform.GetComponent<NPCThirdPersonController>();

            if (npcController != null)
            {
                npcController.StopNPC();
            }

            // Calculate center point
            Vector3 battleCenter = (transform.position + npcTransform.position) * 0.5f;

            // Move PLAYER outside first
            MovePlayerOutsideBattleDome(battleCenter);

            // ALSO move NPC outside
            MoveNPCOutsideBattleDome(npcTransform, battleCenter);

            // Now spawn dome
            _activeBattleDome = Instantiate(
                BattleDomePrefab,
                battleCenter,
                Quaternion.identity
            );

            _activeBattleDome.tag = "BattleDome";

            // Spawn Battle Player (robot)
            _activeBattlePlayer = Instantiate(
                BattlePlayerPrefab,
                battleCenter,
                transform.rotation
            );

            BattleBot = _activeBattlePlayer.transform;

            MoveCameraToBattleBot();

            DisableOverworldPlayer();
            EnableBattleBotControls();
        }

        private void MoveNPCOutsideBattleDome(Transform npc, Vector3 battleCenter)
        {
            Vector3 directionFromCenter = (npc.position - battleCenter).normalized;

            if (directionFromCenter == Vector3.zero)
                directionFromCenter = npc.forward;

            float safeDistance = 6f;

            Vector3 safePosition = battleCenter + directionFromCenter * safeDistance;

            RaycastHit hit;
            if (Physics.Raycast(safePosition + Vector3.up * 10f, Vector3.down, out hit, 50f, GroundLayers))
            {
                safePosition.y = hit.point.y + -0.005f;
            }

            npc.position = safePosition;
        }



        private void MoveCameraToBattleBot()
        {
            if (CinemachineCameraTarget == null || BattleBot == null || NPCArmature == null)
                return;

            // Unparent from old player
            CinemachineCameraTarget.transform.SetParent(null);

            // Position camera target behind the BattleBot
            Vector3 battleBotPosition = BattleBot.position;

            // Place camera slightly behind and above the robot
            Vector3 offset = BattleBot.TransformDirection(BattleCameraOffset);

            CinemachineCameraTarget.transform.position = battleBotPosition + offset;

            // Make camera look at the NPC
            Vector3 lookDirection = (NPCArmature.position - CinemachineCameraTarget.transform.position).normalized;

            Quaternion lookRotation = Quaternion.LookRotation(lookDirection);

            CinemachineCameraTarget.transform.rotation = lookRotation;

            // Sync internal yaw/pitch so Cinemachine doesn't snap back
            _cinemachineTargetYaw = lookRotation.eulerAngles.y;
            _cinemachineTargetPitch = lookRotation.eulerAngles.x;
        }




        private void DisableOverworldPlayer()
        {
#if ENABLE_INPUT_SYSTEM
            if (_playerInput != null)
                _playerInput.enabled = false;
#endif

            // Disable movement script only, NOT the visuals
            this.enabled = false;
        }

        public void EndBattle()
        {
            if (_activeBattlePlayer != null)
                Destroy(_activeBattlePlayer);

            if (_activeBattleDome != null)
                Destroy(_activeBattleDome);

            // Return camera to the original player target
            if (CinemachineCameraTarget != null)
            {
                CinemachineCameraTarget.transform.SetParent(transform);

                CinemachineCameraTarget.transform.localPosition = new Vector3(0, 1.4f, 0);
                CinemachineCameraTarget.transform.localRotation = Quaternion.identity;

                _cinemachineTargetYaw = transform.eulerAngles.y;
                _cinemachineTargetPitch = 0f;
            }

#if ENABLE_INPUT_SYSTEM
            if (_playerInput != null)
                _playerInput.enabled = true;
#endif

            enabled = true;
            _inBattle = false;
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
            Vector3 spherePosition = new Vector3(
                transform.position.x,
                transform.position.y - GroundedOffset,
                transform.position.z
            );

            Grounded = Physics.CheckSphere(
                spherePosition,
                GroundedRadius,
                GroundLayers,
                QueryTriggerInteraction.Ignore
            );

            if (_hasAnimator)
                _animator.SetBool(_animIDGrounded, Grounded);
        }

        private void CameraRotation()
        {
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
                _cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw,
                0.0f
            );
        }

        private void Move()
        {
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            float currentHorizontalSpeed = new Vector3(
                _controller.velocity.x, 0.0f, _controller.velocity.z
            ).magnitude;

            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            _speed = Mathf.Lerp(
                currentHorizontalSpeed,
                targetSpeed * inputMagnitude,
                Time.deltaTime * SpeedChangeRate
            );

            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg
                                  + _mainCamera.transform.eulerAngles.y;

                float rotation = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y,
                    _targetRotation,
                    ref _rotationVelocity,
                    RotationSmoothTime
                );

                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            _controller.Move(
                targetDirection.normalized * (_speed * Time.deltaTime) +
                new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime
            );

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _speed);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
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
                    _verticalVelocity = -2f;

                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    if (_hasAnimator)
                        _animator.SetBool(_animIDJump, true);
                }

                if (_jumpTimeoutDelta >= 0.0f)
                    _jumpTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                    _fallTimeoutDelta -= Time.deltaTime;
                else if (_hasAnimator)
                    _animator.SetBool(_animIDFreeFall, true);

                _input.jump = false;
            }

            if (_verticalVelocity < _terminalVelocity)
                _verticalVelocity += Gravity * Time.deltaTime;
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }
    }
}
