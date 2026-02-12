namespace ItTakesTwo
{
    public class FallState : PlayerState
    {
        protected override void OnEnter(Player player)
        {
            
        }

        protected override void OnExit(Player player)
        {
            
        }

        protected override void OnStep(Player player)
        {
            player.Gravity();
            player.FaceDirectionSmooth(player.lateralVelocity);
            player.Jump();
            
            if (player.isGrounded)
            {
                player.states.Change<IdleState>();
            }
        }
    }
}