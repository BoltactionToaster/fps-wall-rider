using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    #region Variables
    [Header("Camera")]
    [Tooltip("Reference to the main camera used for the player")]
    public Camera playerCam;
    [Tooltip("Rotation speed for moving the camera"), SerializeField]
    private float rotationSpeed = 200f;
    [Tooltip("Camera tilt when riding on walls in degrees")]
    public float cameraTilt = 10.0f;
    [HideInInspector]
    public float cameraVerticalAngle;
    [HideInInspector]
    public Vector3 defaultCameraPos { get; private set; }

    [Header("Movement")]
    [Tooltip("Maximum movement speed when grounded")]
    public float maxSpeedOnGround = 10f;
    [Tooltip("Sprint speed multiplier, based on grounded speed")]
    public float sprintSpeedModifier = 1.65f;
    [Tooltip("Sharpness for the movement when grounded, low makes acceleration and deceleration slow while high makes it fast")]
    public float movementSharpnessOnGround = 15f;

    [Tooltip("The player's transform")]
    public Transform trans;
    [Tooltip("The input handler for the player")]
    public PlayerInput inputHandler;
    [Tooltip("The character controller for the player")]
    public CharacterController characterController;
    [HideInInspector]
    public Vector3 groundNormal;
    [HideInInspector]
    public Vector3 characterVelocity;


    [Tooltip("Force applied upward when jumping")]
    public float jumpForce = 9f;
    [Tooltip("Max movement speed when not grounded")]
    public float maxSpeedInAir = 10f;
    [Tooltip("Acceleration speed when in the air")]
    public float accelerationSpeedInAir = 25f;
    [Tooltip("Force applied downward when in the air to make for smoother jumping")]
    public float gravityDownForce = 20f;
    [Tooltip("Physics layers checked to consider the player grounded")]
    public LayerMask groundCheckLayers = -1;
    [Tooltip("Distance from the bottom of the character controller to test if they're grounded"), SerializeField]
    private float groundCheckDistance = 0.05f;



    [Tooltip("Multiplier for the dash speed")]
    public float dashSpeed = 4.5f;
    [Tooltip("Time it takes for the dash to finish")]
    public float dashTime = 0.2f;
    [Tooltip("The layer which enemies are on to check for when dashing")]
    public LayerMask enemyLayerMask = -1;

    [HideInInspector]
    public bool isGrounded = true;
    [HideInInspector]
    public bool canDoubleJump = true;
    [HideInInspector]
    public bool canDash = true;
    private float lastTimeJumped = 0f;

    [Tooltip("Maximum distance from the wall the player can be to latch on")]
    public float wallCheckDistance = 0.2f;
    [Tooltip("Physics layer that walls are on")]
    public LayerMask wallLayerCheck = -1;


    [HideInInspector]
    public Vector3 wallNormal;
    [HideInInspector]
    public bool leftWall = false;
    [HideInInspector]
    public float lastTimeWallRunJump = 0.0f;

    [Tooltip("Height where the player teleports back into the restaurant"), SerializeField]
    private float fellThroughFloorHeight = -50f;

    const float const_JumpGroundingPreventionTime = 0.2f;
    const float const_GroundCheckDistanceInAir = 0.07f;
    public const float const_WallRunJumpPreventionTime = 0.2f;

    // List of states that exist, though they could be fractured more into smaller states if desired.
    private BaseState currentState;
    public GroundedState groundedState;
    public AirState airState;
    public WallRunState wallRunState;
    public DashState dashState;
    #endregion

    private void Awake()
    {
        // Creating and caching all needed references.
        groundedState = new GroundedState(this);
        airState = new AirState(this);
        wallRunState = new WallRunState(this);
        dashState = new DashState(this);
        currentState = airState;
        currentState.EnterState();
        trans = transform;
    }

    private void Update()
    {
        // Checks if there's ground and rotations the camera before updating the state
        // May need to have a section refactored a bit if there needs to be camera restrictions while dashing or wall riding.
        GroundCheck();
        CameraRotation();
        currentState.Update();
    }

    #region State Stuff
    public void ChangeState(BaseState newState)
    {
        newState.EnterState();
        currentState = newState;
    }
    #endregion

    #region Movement Related
    public void GroundCheck()
    {
        // Make sure that the ground check distance while already in air is very small, to prevent 
        // suddenly snapping to the ground at an uncomfortable distance.
        float chosenGroundCheckDistance = isGrounded ? (characterController.skinWidth + groundCheckDistance) : const_GroundCheckDistanceInAir;

        // Reset values before the ground check
        isGrounded = false;
        groundNormal = Vector3.up;

        // Only try to detect ground if its been a short amount of time since last jump
        if (Time.time >= lastTimeJumped + const_JumpGroundingPreventionTime)
        {
            //if we're grounded, collect info about the ground normal with a downward capsule cast representing our character capsule
            if (Physics.SphereCast(GetCapsuleBottomHemisphere(), characterController.radius - Physics.defaultContactOffset, Vector3.down, out RaycastHit hit,
                chosenGroundCheckDistance, groundCheckLayers, QueryTriggerInteraction.Ignore))
            {

                // Storing the upward direction for the surface found
                groundNormal = hit.normal;

                // Only consider this a valid ground hit if the ground normal goes in the same direction as the character up
                // and if the slope angle is lower than the character controller's limit
                if (Vector3.Dot(hit.normal, transform.up) > 0f && IsNormalUnderSlopeLimit(groundNormal))
                {
                    isGrounded = true;

                    // Handle snapping to the ground
                    if (hit.distance > characterController.skinWidth)
                    {
                        characterController.Move(Vector3.down * hit.distance);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Sets the last time jumped to prevent grounding from happening instantly and sets the grounding state to false
    /// </summary>
    public void SetTimeLastJumped()
    {
        lastTimeJumped = Time.time;
        isGrounded = false;
        groundNormal = Vector3.up;
    }

    /// <summary>
    /// Rotates the camera based off of inputs for the camera.
    /// </summary>
    private void CameraRotation()
    {
        //Rotates the character horizontally
        trans.Rotate(new Vector3(0f, inputHandler.GetLookInputsHorizontal() * rotationSpeed, 0f), Space.Self);

        //Rotates the camera vertically
        cameraVerticalAngle += inputHandler.GetLookInputsVertical() * rotationSpeed;
        cameraVerticalAngle = Mathf.Clamp(cameraVerticalAngle, -89f, 89f);
        //Caching the transform of the camera may be optimal. I am unsure if Unity has started caching transforms in 2020 or not.
        playerCam.transform.localEulerAngles = new Vector3(cameraVerticalAngle, 0f, playerCam.transform.eulerAngles.z);
    }
    
    /// <summary>
    /// Applies velocity changes to characterVelocity.
    /// </summary>
    public void ApplyVelocity()
    {
        //Moves the character and saved the position before the character is moved
        Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
        Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere();
        characterController.Move(characterVelocity * Time.deltaTime);
        //Use the before move vectors to check for obstructions and adjust velocity accordingly
        //Most likely not super efficient due to both normalizing and getting the magnitude of a vector
        if (Physics.CapsuleCast(capsuleBottomBeforeMove, capsuleTopBeforeMove, characterController.radius,
            characterVelocity.normalized, out RaycastHit hit, characterVelocity.magnitude * Time.deltaTime,
            -1, QueryTriggerInteraction.Ignore))
        {
            characterVelocity = Vector3.ProjectOnPlane(characterVelocity, hit.normal);
        }
    }
    #endregion

    #region Utility Methods
    public bool IsNormalUnderSlopeLimit(Vector3 normal)
    {
        return Vector3.Angle(trans.up, normal) <= characterController.slopeLimit;
    }

    /// <summary>
    /// Gets the bottom hemisphere of the capsule used for the player
    /// </summary>
    /// <returns></returns>
    public Vector3 GetCapsuleBottomHemisphere()
    {
        // Can be optimized slightly be caching the reference so it only gets called once a frame
        return trans.position - (trans.up * characterController.radius);
    }

    /// <summary>
    /// Gets the top hemisphere of the capsule used for the player
    /// </summary>
    /// <returns></returns>
    public Vector3 GetCapsuleTopHemisphere()
    {
        // Can be optimized slightly be caching the reference for this so it only gets called once a frame, I believe
        // That is, skipping the math behind it completely.
        return trans.position + (trans.up * (characterController.radius));
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        //Various debug tools used. They do not compile into builds.
        Gizmos.color = Color.red;
        Gizmos.DrawRay(trans.localPosition + trans.TransformVector(Vector3.left * characterController.bounds.extents.x), 
            trans.TransformVector(Vector3.left) * wallCheckDistance);
        Gizmos.DrawWireSphere(GetCapsuleTopHemisphere(), characterController.radius);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(GetCapsuleBottomHemisphere(), characterController.radius);
        Gizmos.DrawRay(trans.localPosition + trans.TransformVector(Vector3.right * characterController.bounds.extents.x), 
            trans.TransformVector(Vector3.right) * wallCheckDistance);

        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(trans.position, playerCam.transform.forward);
    }
#endif

    public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal)
    {
        Vector3 directionRight = Vector3.Cross(direction, trans.up);
        return Vector3.Cross(slopeNormal, directionRight).normalized;
    }
    #endregion
}
