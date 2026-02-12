using UnityEngine;

namespace ItTakesTwo
{
    public class WalkState: PlayerState
    {
        protected override void OnEnter(Player player) { }
        protected override void OnExit(Player player) { }

        protected override void OnStep(Player player)
        {
            player.Gravity();
            player.Jump();
            player.Fall();
            var inputDirection = player.inputs.GetMovementCameraDirection();
            
            if (inputDirection.sqrMagnitude > 0)
            {
                var dot = Vector3.Dot(inputDirection, player.lateralVelocity);

                if (dot >= player.Attributes.current.brakeThreshold)
                {
                    player.Accelerate(inputDirection);
                    player.FaceDirectionSmooth(player.lateralVelocity);
                }
            }
            else
            {
                player.Friction();

                if (player.lateralVelocity.sqrMagnitude <= 0)
                {
                    player.states.Change<IdleState>();
                }
            }
        }

    }
}