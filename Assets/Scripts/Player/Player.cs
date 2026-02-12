using UnityEngine;

namespace ItTakesTwo
{
    public class Player:Entity<Player>
    {
        public PlayerInputManager inputs { get; protected set; }
        public PlayerAttributesManager Attributes { get; protected set; }
		public int jumpCounter { get; protected set; }

        protected virtual void InitializeInputs() => inputs = GetComponent<PlayerInputManager>();
        protected virtual void InitializeStats() => Attributes = GetComponent<PlayerAttributesManager>();

        protected override void Awake()
        {
            base.Awake();
            InitializeInputs();
            InitializeStats();
        }

        public virtual void Accelerate(Vector3 direction)
        {
            var turningDrag = isGrounded  ? Attributes.current.runningTurningDrag : Attributes.current.turningDrag;
            var acceleration = isGrounded  ? Attributes.current.runningAcceleration : Attributes.current.acceleration;
            var finalAcceleration = isGrounded ? acceleration : Attributes.current.airAcceleration;
            var topSpeed = Attributes.current.topSpeed;

            Accelerate(direction, turningDrag, finalAcceleration, topSpeed);
            
        }

        public virtual void Friction()
        {
            Decelerate(Attributes.current.friction);
        }
        public virtual void Fall()
		{
			if (!isGrounded)
			{
				states.Change<FallState>();
			}
		}
        
		public virtual void Gravity()
		{
			if (!isGrounded && verticalVelocity.y > -Attributes.current.gravityTopSpeed)
			{
				var speed = verticalVelocity.y;
				var force = verticalVelocity.y > 0 ? Attributes.current.gravity : Attributes.current.fallGravity;
				speed -= force * gravityMultiplier * Time.deltaTime;
				speed = Mathf.Max(speed, -Attributes.current.gravityTopSpeed);
				verticalVelocity = new Vector3(0, speed, 0);
			}
		}

		public virtual void Jump()
		{
			var canMultiJump = (jumpCounter > 0) && (jumpCounter < Attributes.current.multiJumps);
			var canCoyoteJump = (jumpCounter == 0) && (Time.time < lastGroundTime + Attributes.current.coyoteJumpThreshold);
			
			
			if (isGrounded || canMultiJump || canCoyoteJump)
			{
				if (inputs.GetJumpDown())
				{
					
					Jump(Attributes.current.maxJumpHeight);
				}
			}

			if (inputs.GetJumpUp() && (jumpCounter > 0) && (verticalVelocity.y > Attributes.current.minJumpHeight))
			{
				verticalVelocity = Vector3.up * Attributes.current.minJumpHeight;
			}
		}

		public virtual void Jump(float height)
		{
			jumpCounter++;
			verticalVelocity = Vector3.up * height;
			states.Change<FallState>();
		}

        public virtual void FaceDirectionSmooth(Vector3 direction) => FaceDirection(direction, Attributes.current.rotationSpeed);

        protected virtual void FaceDirection(Vector3 direction, float rotationSpeed)
        {
            if (direction.sqrMagnitude <= 0) return;

            var target = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }
        public void ApplyCorrection(Vector3 pos, Vector3 vel, float hardSnapDistance = 2f, float lerpSpeed = 10f)
        {
            var delta = (transform.position - pos).magnitude;
            if (delta > hardSnapDistance)
            {
                transform.position = pos;
                velocity = vel;
                return;
            }

            transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * lerpSpeed);
            velocity = Vector3.Lerp(velocity, vel, Time.deltaTime * lerpSpeed);
        }
        
    }
}