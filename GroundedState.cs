using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundedState : BaseState
{
    public GroundedState(PlayerController player) : base(player) { }

    public override void EnterState()
    {
        player.canDash = true;
        player.canDoubleJump = true;
    }

    public override void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (player.isGrounded)
        {
            Vector3 worldSpaceMoveInput = player.trans.TransformVector(player.inputHandler.GetMovementInput());


            //Calculate the desired velocity from inputs, max speed, and current slope
            //Somewhat slow but it is only calculated for the player, so it should be fine.
            //Also applied a sprint multiplier
            Vector3 targetVelocity = worldSpaceMoveInput * player.maxSpeedOnGround *
                (player.inputHandler.GetSprintInput() ? player.sprintSpeedModifier : 1.0f);
            targetVelocity = player.GetDirectionReorientedOnSlope(targetVelocity.normalized, player.groundNormal) * targetVelocity.magnitude;

            //Smoothly interpolate between current velocity and the target velocity based on acceleration
            player.characterVelocity = Vector3.Lerp(player.characterVelocity, targetVelocity,
                player.movementSharpnessOnGround * Time.deltaTime);

            //Jumping check
            if (player.inputHandler.GetJumpInputDown())
            {
                //Add the jumpSpeed value upwards. When on the ground, the expected y velocity is 0 so there is no need to cancel it
                player.characterVelocity += Vector3.up * player.jumpForce;

                //Remember the last time the player jumped to prevent the player from instantly snapping to the ground
                player.SetTimeLastJumped();
            }
            //No need to check if the player can dash here as it is always enabled on entering this state
            else if (player.inputHandler.GetDashInputDown())
            {
                //Simply changes into the dashing state and exits the method.
                player.ChangeState(player.dashState);
                return;
            }

            //Apply the final calculated velocity
            player.ApplyVelocity();
        }
        else
        {
            player.ChangeState(player.airState);
        }
    }
}
