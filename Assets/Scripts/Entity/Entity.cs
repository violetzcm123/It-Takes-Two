using UnityEngine;

namespace ItTakesTwo
{
    public class Entity : MonoBehaviour
    {
        
        public CharacterController controller { get; protected set; }
        /// <summary>
        /// Returns the collider height of this Entity.
        /// </summary>
        public float height => controller.height;
        /// <summary>
        /// The center of the Character Controller collider.
        /// </summary>
        public Vector3 center => controller.center;
        public float radius => controller.radius;
        /// <summary>
        /// Returns the original height of this Entity.
        /// </summary>
        public float originalHeight { get; protected set; }
        /// <summary>
		/// Returns the last frame this Entity was grounded.
		/// </summary>
		public float lastGroundTime { get; protected set; }
        public Vector3 position => transform.position + center;
        //用原始身高的脚底位置来判断，避免角色蹲下/缩放碰撞体时导致误判
        public Vector3 unsizedPosition => position - transform.up * height * 0.5f + transform.up * originalHeight * 0.5f;
        //脚步判定参考点，用于判断某个点是否在角色脚下
        public Vector3 stepPosition => position - transform.up * (height * 0.5f - controller.stepOffset);
        
        protected readonly float m_groundOffset = 0.1f;
        public Vector3 velocity { get; set; }

        public Vector3 lateralVelocity
        {
            get { return new Vector3(velocity.x, 0, velocity.z); }
            set { velocity = new Vector3(value.x, velocity.y, value.z); }
        }

        public Vector3 verticalVelocity
        {
            get { return new Vector3(0, velocity.y, 0); }
            set { velocity = new Vector3(velocity.x, value.y, velocity.z); }
        }

        public bool isGrounded { get; protected set; } = true;
        // 客户端关闭本地模拟，改由服务器状态快照驱动
        public bool simulationEnabled = true;
        
        public float accelerationMultiplier { get; set; } = 1f;
        public float gravityMultiplier { get; set; } = 1f;
        
        public float topSpeedMultiplier { get; set; } = 1f;

        public float turningDragMultiplier { get; set; } = 1f;

        public float decelerationMultiplier { get; set; } = 1f;
        public void SetGrounded(bool grounded)
        {
            isGrounded = grounded;
        }
        public virtual bool SphereCast(Vector3 direction, float distance, int layer = Physics.DefaultRaycastLayers,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
        {
            return SphereCast(direction, distance, out _, layer, queryTriggerInteraction);
        }

        public virtual bool SphereCast(Vector3 direction, float distance,
            out RaycastHit hit, int layer = Physics.DefaultRaycastLayers,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
        {
            var castDistance = Mathf.Abs(distance - radius);
            return Physics.SphereCast(position, radius, direction,
                out hit, castDistance, layer, queryTriggerInteraction);
        }
    }

    public class Entity<T> : Entity where T : Entity<T>
    {
        public EntityStateManager<T> states { get; protected set; }
        
        protected virtual void InitializeController()
        {
            controller = GetComponent<CharacterController>();
            if (!controller)
            {
                controller = gameObject.AddComponent<CharacterController>();
            }

            controller.skinWidth = 0.005f;
            controller.minMoveDistance = 0;
            originalHeight = controller.height;
        }

        protected virtual void InitializeStateManager() => states = GetComponent<EntityStateManager<T>>();

        protected virtual void Awake()
        {
            InitializeController();
            InitializeStateManager();
        }

        protected virtual void HandleStates() => states.Step();

        protected virtual void HandleController()
        {
            if (controller != null && controller.enabled)
            {
                controller.Move(velocity * Time.deltaTime);
                return;
            }

            transform.position += velocity * Time.deltaTime;
        }

        protected virtual void HandleGround()
        {
            var distance = (height * 0.5f) + m_groundOffset;
            
            if(SphereCast(Vector3.down, distance,out var hit)&& verticalVelocity.y <= 0)
            {
                if (!isGrounded)
                {
                    if (EvaluateLanding(hit))
                    {
                        EnterGround(hit);
                    }
                }
            }
            else
            {
                ExitGround();
            }
        }

        protected virtual void Update()
        {
            if (!simulationEnabled)
                return;
            if (controller.enabled)
            {
                HandleStates();
                HandleController();
                HandleGround();
            }
        }

        protected virtual void Accelerate(Vector3 direction, float turningDrag, float acceleration, float topSpeed)
        {
            if (direction.sqrMagnitude > 0)
            {
                var speed = Vector3.Dot(direction, lateralVelocity);
                var velocity = direction * speed;
                var turningVelocity = lateralVelocity - velocity;
                var turningDelta = turningDrag * turningDragMultiplier * Time.deltaTime;
                var targetTopSpeed = topSpeed * topSpeedMultiplier;

                if (lateralVelocity.magnitude < targetTopSpeed || speed < 0)
                {
                    speed += acceleration * accelerationMultiplier * Time.deltaTime;
                    speed = Mathf.Clamp(speed, -targetTopSpeed, targetTopSpeed);
                }

                velocity = direction * speed;
                turningVelocity = Vector3.MoveTowards(turningVelocity, Vector3.zero, turningDelta);
                lateralVelocity = velocity + turningVelocity;
            }
        }

        protected virtual void Decelerate(float deceleration)
        {
            var delta = deceleration * decelerationMultiplier * Time.deltaTime;
            lateralVelocity = Vector3.MoveTowards(lateralVelocity, Vector3.zero, delta);
        }

        protected virtual bool EvaluateLanding(RaycastHit hit)
        {
            return IsPointUnderStep(hit.point);
        }

        protected virtual bool IsPointUnderStep(Vector3 point)=> stepPosition.y > point.y;
        protected virtual void EnterGround(RaycastHit hit)
        {
            if (!isGrounded)
            {
                isGrounded = true;
            }
        }

        protected virtual void ExitGround()
        {
            if (isGrounded)
            {
                isGrounded = false;
                transform.parent = null;
                verticalVelocity = Vector3.Max(verticalVelocity, Vector3.zero);
                
            }
        }
    }
}