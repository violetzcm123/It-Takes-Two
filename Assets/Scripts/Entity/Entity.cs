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
        /// <summary>
        /// Returns the original height of this Entity.
        /// </summary>
        public float originalHeight { get; protected set; }
        public Vector3 position => transform.position + center;
        public Vector3 unsizedPosition => position - transform.up * height * 0.5f + transform.up * originalHeight * 0.5f;
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
        
        public float accelerationMultiplier { get; set; } = 1f;
        
        public float topSpeedMultiplier { get; set; } = 1f;

        public float turningDragMultiplier { get; set; } = 1f;

        public float decelerationMultiplier { get; set; } = 1f;
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

        protected virtual void Update()
        {
            if (controller != null)
            {
                isGrounded = controller.isGrounded;
            }

            if (states != null)
            {
                HandleStates();
            }

            HandleController();
        }

        public virtual void Accelerate(Vector3 direction, float turningDrag, float acceleration, float topSpeed)
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

        public virtual void Decelerate(float deceleration)
        {
            var delta = deceleration * decelerationMultiplier * Time.deltaTime;
            lateralVelocity = Vector3.MoveTowards(lateralVelocity, Vector3.zero, delta);
        }
    }
}