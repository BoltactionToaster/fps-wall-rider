using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirState : BaseState
{
    private Vector3[] wallRideDetectionDirs;
    public AirState(PlayerController player) : base(player) 
    {
        //Could set this in the inspector to reduce the amount of code, but it's okay for now.
        wallRideDetectionDirs = new Vector3[4];
        wallRideDetectionDirs[0] = Vector3.right;
        wallRideDetectionDirs[1] = (Vector3.right + Vector3.forward).normalized;
        wallRideDetectionDirs[2] = Vector3.left;
        wallRideDetectionDirs[3] = (Vector3.left + Vector3.forward).normalized;
    }


    public override void EnterState()
    {
        // Currently unused.
    }

    public override void Update()
    {
        //Checks both the left and right of the player.
        if (WallRideCheck())
        {
            player.ApplyVelocity();
            player.ChangeState(player.wallRunState);
            return;
        }
        else if (player.isGrounded)
        {
            player.ChangeState(player.groundedState);
            return;
        }

        //Jumping check
        if (player.inputHandler.GetJumpInputDown() && player.canDoubleJump)
        {
            //Add the jumpSpeed value upwards and cancels upwards velocity to prevent the player from being able to launch high
            //when double jumping shortly after jumping and lets the player jump normally when falling large distances
            player.characterVelocity = new Vector3(player.characterVelocity.x, 0.0f, player.characterVelocity.z);
            player.characterVelocity += Vector3.up * player.jumpForce;
            player.canDoubleJump = false;

            //Remember the last time the player jumped to prevent the player from instantly snapping to the ground, may
            //not be needed but is there just in case.
            player.SetTimeLastJumped();
        }
        else if (player.inputHandler.GetDashInputDown() && player.canDash)
        {
            player.ChangeState(player.dashState);
            return;
        }

        Vector3 worldSpaceMoveInput = player.trans.TransformVector(player.inputHandler.GetMovementInput());

        //Adds horizontal speed and clamps it to a maximum value
        player.characterVelocity += worldSpaceMoveInput * player.accelerationSpeedInAir * Time.deltaTime;

        float verticalVelocity = player.characterVelocity.y;
        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(player.characterVelocity, Vector3.up);
        horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, player.maxSpeedInAir * 
            (player.inputHandler.GetSprintInput() ? player.sprintSpeedModifier : 1.0f));
        player.characterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);

        //Apply gravity to the velocity
        player.characterVelocity += Vector3.down * player.gravityDownForce * Time.deltaTime;
        player.ApplyVelocity();
    }

    private bool WallRideCheck()
    {
        if (Time.time >= player.lastTimeWallRunJump + PlayerController.const_WallRunJumpPreventionTime)
        {
            RaycastHit hit;
            for (int i = 0; i < wallRideDetectionDirs.Length; i++)
            {
                var transformedDir = player.trans.TransformVector(wallRideDetectionDirs[i]);
                //Simply shoots a raycast in the directions specified above.
                if (Physics.Raycast(player.trans.position + transformedDir * player.characterController.radius,
                    transformedDir, out hit, player.wallCheckDistance, player.wallLayerCheck, QueryTriggerInteraction.Ignore))
                {
                    if (i > 1)
                    {
                        player.leftWall = true;
                    }
                    else
                    {
                        player.leftWall = false;
                    }
                    player.wallNormal = hit.normal;
                    player.characterVelocity += transformedDir * hit.distance;
                    return true;
                }
            }
        }
        return false;
    }
}
