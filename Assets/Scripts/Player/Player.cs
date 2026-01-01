using UnityEngine;

namespace ItTakesTwo
{
    public class Player:Entity<Player>
    {
        public PlayerInputManager inputs { get; protected set; }
        public PlayerStatsManager stats { get; protected set; }

        protected virtual void InitializeInputs() => inputs = GetComponent<PlayerInputManager>();
        protected virtual void InitializeStats() => stats = GetComponent<PlayerStatsManager>();

        protected override void Awake()
        {
            base.Awake();
            InitializeInputs();
            InitializeStats();
        }

        public virtual void Accelerate(Vector3 direction)
        {
            var turningDrag = isGrounded  ? stats.current.runningTurningDrag : stats.current.turningDrag;
            var acceleration = isGrounded  ? stats.current.runningAcceleration : stats.current.acceleration;
            var finalAcceleration = isGrounded ? acceleration : stats.current.airAcceleration;
            var topSpeed = stats.current.topSpeed;

            Accelerate(direction, turningDrag, finalAcceleration, topSpeed);
            
        }

        public virtual void Friction()
        {
            Decelerate(stats.current.friction);
        }

        public virtual void FaceDirectionSmooth(Vector3 direction) => FaceDirection(direction, stats.current.rotationSpeed);

        protected virtual void FaceDirection(Vector3 direction, float rotationSpeed)
        {
            if (direction.sqrMagnitude <= 0) return;

            var target = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }
        
    }
}