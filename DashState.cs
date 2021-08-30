using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashState : BaseState
{
    public DashState(PlayerController player) : base(player) { }

    Vector3 dashDirection;

    float dashSpeed;

    float startDashTime;

    public override void EnterState()
    {
        //Gets the dash direction and changes the player velocity to match it with the speed. 
        dashDirection = player.playerCam.transform.forward;
        dashSpeed = player.maxSpeedOnGround * player.dashSpeed;

        player.canDash = false;

        player.characterVelocity = dashDirection * dashSpeed;
        //Sets the dash time to know how long to dash for.
        startDashTime = Time.time;
    }

    public override void Update()
    {
        if (Time.time < startDashTime + player.dashTime)
        {
            //Checks the body of the player to see if they hit something, a more performant and possibly more accurate from the player perspective
            //would be a sphere from the camera forward.
            //Currently checking larger than the radius of the player to make sure the player does not need to be super accurate
            //effectively helping the player.
            if (Physics.CapsuleCast(player.GetCapsuleBottomHemisphere(),player.GetCapsuleTopHemisphere(), 
                player.characterController.radius * 1.5f, dashDirection, out RaycastHit hit, dashSpeed * Time.deltaTime, player.enemyLayerMask, 
                QueryTriggerInteraction.Ignore))
            {
                player.canDash = true;
                //Directly interfaces with a singleton spawner, would interact with an interface in an actual game
                //Or just not exist and do damage from dashes in a different way?
                EnemySpawner.Instance.EnemyKilled(hit.transform);
                player.characterVelocity = Vector3.zero;
                player.ChangeState(player.airState);
                return;
            }
            player.ApplyVelocity();
        }
        //If it is past the dash time, we can safely assume that we are done with the dash.
        else
        {
            player.characterVelocity = Vector3.zero;
            player.ChangeState(player.airState);
        }
    }
}
