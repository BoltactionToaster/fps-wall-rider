using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallRunState : BaseState
{
    public WallRunState(PlayerController player) : base(player) { }

    float playerSpeedOnWall;
    Vector3 reversedWallNormal;
    Quaternion currentWallDirection;

    float amountRotated = 0.0f;
    //used to calculate what way the player needs to slide on
    float side;

    Coroutine cameraTiltCoroutine = null;

    public override void EnterState()
    {
        Vector3 wallRideDirection;
        var wallDir = Quaternion.AngleAxis(90.0f, Vector3.up);
        //Adjusts the angle of the normal of the wall by 90 degrees so it'll be facing the direction the player might ride in
        Vector3 adjustedWallNormal = wallDir * player.wallNormal;
        //Checks the transform of the player against this adjusted wall normal to see if the angle difference is less than 90 degrees
        if (Vector3.Angle(player.trans.forward, adjustedWallNormal) < 90.0f)
        {
            wallRideDirection = adjustedWallNormal;
            currentWallDirection = wallDir;
        }
        //Since we are wall riding anyways by entering this state we can assume that the opposite direction must be the correct wall
        //ride direction
        else
        {
            wallDir = Quaternion.AngleAxis(-90.0f, Vector3.up);
            wallRideDirection = wallDir * player.wallNormal;

        }

        currentWallDirection = wallDir;

        //Saving if the player sprinted because it would be awkward if you could just stop sprinting mid wall run
        playerSpeedOnWall = player.maxSpeedOnGround *
            (player.inputHandler.GetSprintInput() ? player.sprintSpeedModifier : 1.0f);

        player.characterVelocity = wallRideDirection * playerSpeedOnWall;
        //Saving the reversed normal for convenience in checking later
        reversedWallNormal = player.wallNormal * -1f;
        side = (player.leftWall ? -1.0f : 1.0f);
        //Eliminates the last coroutine if it exists. This happens if the untilt coroutine is still going
        //when the player ends up wall riding again
        if (cameraTiltCoroutine != null)
        {
            player.StopCoroutine(cameraTiltCoroutine);
        }
        //Saves the coroutine for camera tilting to cancel it later if the player unlatches before it is over

        cameraTiltCoroutine = player.StartCoroutine(TiltCamera());
        //Enables dashing & double jump when you start wall riding
        player.canDash = true;
        player.canDoubleJump = true;
    }

    public override void Update()
    {
        if (player.inputHandler.GetJumpInputDown())
        {
            //Just does everything needed to jump
            player.characterVelocity = (Vector3.up + player.wallNormal).normalized * player.jumpForce;
            CameraUntilt();
            player.lastTimeWallRunJump = Time.time;
            player.SetTimeLastJumped();
            player.ChangeState(player.airState);
            return;
        }
        if (Physics.Raycast(player.trans.position + reversedWallNormal * player.characterController.radius, reversedWallNormal,
            out RaycastHit hit, player.wallCheckDistance * 2.0f, player.wallLayerCheck, QueryTriggerInteraction.Ignore))
        {
            //Recalculates the wall normal and move direction when encountering a wall that is curved
            if (hit.normal != reversedWallNormal)
            {
                //These are all the same calculations in the enter state
                player.wallNormal = hit.normal;
                reversedWallNormal = player.wallNormal * -1.0f;

                //This is required to prevent a bug where the player will not be able to ride on cylindrical walls forever
                player.characterVelocity = reversedWallNormal * hit.distance;
                player.ApplyVelocity();

                player.characterVelocity = currentWallDirection * player.wallNormal * playerSpeedOnWall;
            }

            player.ApplyVelocity();
        }
        //Detatches completely if it does not detect any sort of wall.
        else
        {
            CameraUntilt();
            if (player.isGrounded)
            {
                player.ChangeState(player.groundedState);
            }
            else
            {
                player.ChangeState(player.airState);
            }
            return;
        }
    }

    #region CameraInteractions
    //Quite frankly, these should be done elsewhere
    void CameraUntilt()
    {
        if (cameraTiltCoroutine != null)
        {
            player.StopCoroutine(cameraTiltCoroutine);
        }
        cameraTiltCoroutine = player.StartCoroutine(UnTiltCamera());
    }

    IEnumerator TiltCamera()
    {
        var amount = Vector3.forward * Mathf.Lerp(0.0f, player.cameraTilt * side, 0.1f);
        //Need to save the amount rotated in order to track for both positive and negative directions as Unity automatically
        //flips from -1 to 359 under the hood, preventing any check using positive or negative
        while (Mathf.Abs(amountRotated) < player.cameraTilt)
        {
            player.playerCam.transform.Rotate(amount);
            amountRotated += amount.z;
            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator UnTiltCamera()
    {
        var amount = Vector3.back * Mathf.Lerp(0.0f, player.cameraTilt * side, 0.1f);
        while (Mathf.Abs(amountRotated) > 0.0f)
        {
            player.playerCam.transform.Rotate(amount);
            amountRotated += amount.z;
            yield return new WaitForFixedUpdate();
        }
    }
    #endregion
}
