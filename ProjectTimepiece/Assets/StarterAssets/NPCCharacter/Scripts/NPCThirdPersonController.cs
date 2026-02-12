using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
    public class NPCThirdPersonController : MonoBehaviour
    {
        enum AIState { Idle, Patrol, Chase }
        AIState _state;

        [Header("Movement")]
        public float MoveSpeed = 2f;
        public float RotationSmoothTime = 0.12f;
        public float SpeedChangeRate = 8f;

        [Header("Patrol")]
        public float PatrolRadius = 6f;
        public float IdleTimeMin = 1.5f;
        public float IdleTimeMax = 3f;

        [Header("Detection")]
        public float ViewRadius = 10f;
        [Range(0, 360)] public float ViewAngle = 120f;
        public LayerMask PlayerLayer;
        public LayerMask ObstacleLayers;

        [Header("Obstacle Avoidance")]
        public float ObstacleRadius = 0.4f;
        public float ObstacleStopDistance = 0.6f;
        public float SideRayAngle = 30f;
        public LayerMask ObstacleAvoidLayers;

        [Header("Gravity")]
        public float Gravity = -15f;
        public float TerminalVelocity = -53f;

        [Header("Grounded")]
        public bool Grounded = true;
        public float GroundedOffset = -0.14f;
        public float GroundedRadius = 0.28f;
        public LayerMask GroundLayers;

        public GameObject BattleDomePrefab;
        public GameObject BattlePlayerPrefab;

        public Transform PlayerArmature;
        public Transform NPCArmature;
        public Transform BattleBot;

        private CharacterController _controller;
        private Animator _animator;
        private Transform _player;

        private float _speed;
        private float _animationBlend;
        private float _rotationVelocity;
        private float _verticalVelocity;

        private Vector3 _patrolTarget;
        private float _idleTimer;

        // Animator IDs
        private int _animIDSpeed;
        private int _animIDMotionSpeed;
        private int _animIDGrounded;

        private GameObject _activeBattleDome;
        private GameObject _activeBattlePlayer;

        private bool _stoppedByCollision = false;
        private bool _inBattle = false;

        private bool _hasAnimator;

        // ================= UNITY =================

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _hasAnimator = TryGetComponent(out _animator);

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                _player = playerObj.transform;

            AssignAnimationIDs();
            SetIdle();
        }

        private void Update()
        {
            if (_inBattle)
            {
                return;     // Do absolutely nothing while in battle
            }


            GroundedCheck();
            ApplyGravity();

            if (_player != null && CanSeePlayer())
                _state = AIState.Chase;
            else if (_state == AIState.Chase)
                SetIdle();

            switch (_state)
            {
                case AIState.Idle:
                    UpdateIdle();
                    break;
                case AIState.Patrol:
                    UpdatePatrol();
                    break;
                case AIState.Chase:
                    UpdateChase();
                    break;
            }
        }

        private void MoveNPCOutsideBattleDome(Vector3 battleCenter)
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
                // IMPORTANT: place NPC SLIGHTLY ABOVE ground to avoid embedding
                safePosition.y = hit.point.y + 0.2f;
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

        // ================= STATES =================

        void SetIdle()
        {
            _state = AIState.Idle;
            _idleTimer = Random.Range(IdleTimeMin, IdleTimeMax);
        }

        void UpdateIdle()
        {
            Move(Vector3.zero);

            _idleTimer -= Time.deltaTime;
            if (_idleTimer <= 0f)
                SetPatrol();
        }

        void SetPatrol()
        {
            _state = AIState.Patrol;

            Vector2 rand = Random.insideUnitCircle * PatrolRadius;
            _patrolTarget = transform.position + new Vector3(rand.x, 0f, rand.y);
        }

        void UpdatePatrol()
        {
            Vector3 dir = _patrolTarget - transform.position;
            dir.y = 0f;

            if (dir.magnitude < 0.5f)
            {
                SetIdle();
                return;
            }

            Move(dir.normalized);
        }

        void UpdateChase()
        {
            Vector3 dir = _player.position - transform.position;
            dir.y = 0f;
            Move(dir.normalized);
        }

        // ================= MOVEMENT =================

        void Move(Vector3 direction)
        {
            direction = ObstacleCheck(direction);

            float targetSpeed = direction == Vector3.zero ? 0f : MoveSpeed;
            _speed = Mathf.Lerp(_speed, targetSpeed, Time.deltaTime * SpeedChangeRate);
            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);

            if (direction != Vector3.zero)
            {
                float targetRotation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                float rotation = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y,
                    targetRotation,
                    ref _rotationVelocity,
                    RotationSmoothTime
                );

                transform.rotation = Quaternion.Euler(0f, rotation, 0f);
            }

            Vector3 velocity =
                direction * (_speed * Time.deltaTime) +
                Vector3.up * (_verticalVelocity * Time.deltaTime);

            _controller.Move(velocity);
            UpdateAnimator();
        }

        void ApplyGravity()
        {
            if (Grounded)
            {
                if (_verticalVelocity < 0f)
                    _verticalVelocity = -2f;
            }
            else
            {
                _verticalVelocity += Gravity * Time.deltaTime;
                _verticalVelocity = Mathf.Max(_verticalVelocity, TerminalVelocity);
            }
        }

        Vector3 ObstacleCheck(Vector3 direction)
        {
            if (direction == Vector3.zero)
                return direction;

            Vector3 origin = transform.position + Vector3.up * 0.5f;

            // HARD STOP
            if (Physics.SphereCast(origin, ObstacleRadius, direction,
                out RaycastHit hit, ObstacleStopDistance, ObstacleAvoidLayers))
            {
                return Vector3.zero;
            }

            // SIDE STEERING
            Vector3 left = Quaternion.Euler(0, -SideRayAngle, 0) * direction;
            Vector3 right = Quaternion.Euler(0, SideRayAngle, 0) * direction;

            bool hitLeft = Physics.Raycast(origin, left, ObstacleStopDistance, ObstacleAvoidLayers);
            bool hitRight = Physics.Raycast(origin, right, ObstacleStopDistance, ObstacleAvoidLayers);

            if (hitLeft && !hitRight)
                return right.normalized;

            if (hitRight && !hitLeft)
                return left.normalized;

            return direction;
        }

        // ================= DETECTION =================

        bool CanSeePlayer()
        {
            Vector3 origin = transform.position + Vector3.up;
            Vector3 dir = _player.position - origin;

            if (dir.magnitude > ViewRadius)
                return false;

            if (Vector3.Angle(transform.forward, dir) > ViewAngle * 0.5f)
                return false;

            if (Physics.Raycast(origin, dir.normalized, dir.magnitude, ObstacleLayers))
                return false;

            return true;
        }

        // ================= ANIMATOR =================

        void UpdateAnimator()
        {
            if (!_hasAnimator) return;

            _animator.SetFloat(_animIDSpeed, _animationBlend);
            _animator.SetFloat(_animIDMotionSpeed, 1f);
        }

        void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDGrounded = Animator.StringToHash("Grounded");
        }

        void GroundedCheck()
        {
            Vector3 pos = transform.position;
            pos.y -= GroundedOffset;

            Grounded = Physics.CheckSphere(
                pos,
                GroundedRadius,
                GroundLayers,
                QueryTriggerInteraction.Ignore
            );

            if (_hasAnimator)
                _animator.SetBool(_animIDGrounded, Grounded);
        }

        // ================= DEBUG =================

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(origin + transform.forward * ObstacleStopDistance, ObstacleRadius);

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(origin, transform.forward * ObstacleStopDistance);

            Gizmos.color = Color.green;
            Gizmos.DrawRay(origin,
                Quaternion.Euler(0, -SideRayAngle, 0) * transform.forward * ObstacleStopDistance);
            Gizmos.DrawRay(origin,
                Quaternion.Euler(0, SideRayAngle, 0) * transform.forward * ObstacleStopDistance);
        }

        public void StopNPC()
        {
            _stoppedByCollision = true;
            _inBattle = true;

            // Zero out movement immediately
            _speed = 0f;
            _animationBlend = 0f;

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, 0f);
                _animator.SetFloat(_animIDMotionSpeed, 0f);
            }

            // Disable the character controller so it can't move
            if (_controller != null)
            {
                _controller.enabled = false;
            }
        }


    }
}
