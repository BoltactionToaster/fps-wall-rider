using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    //Using the old input system for the sake of ease when it comes to getting it up and running
    //As I personally have not had a project where I had to use the new input system.

    //Also currently these settings are only changeable in the inspector.
    [Tooltip("Sensitivity multiplier for moving the camera around")]
    public float lookSensitivity = 1f;
    [Tooltip("Limit to consider an input when using a trigger on a controller")]
    public float triggerAxisThreshold = 0.4f;
    [Tooltip("Used to flip the vertical input axis")]
    public bool invertYAxis = false;
    [Tooltip("Used to flip the horizontal input axis")]
    public bool invertXAxis = false;

    // Start is called before the first frame update
    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    /// <summary>
    /// Determines if input can be processed in the game as opposed to input for menus.
    /// </summary>
    /// <returns></returns>
    private bool CanProcessGameInput()
    {
        return Cursor.lockState == CursorLockMode.Locked;
    }

    /// <summary>
    /// Gets the X and Z directions the player is actively moving in.
    /// </summary>
    /// <returns></returns>
    public Vector3 GetMovementInput()
    {
        //Might not need to be Vector3, might only need to be vector2
        if (CanProcessGameInput())
        {
            Vector3 moveDirections = new Vector3(Input.GetAxisRaw("Horizontal"), 
                0f, Input.GetAxisRaw("Vertical"));

            //Prevents diagonal movement from being faster than movement in any single direction.
            moveDirections = Vector3.ClampMagnitude(moveDirections, 1);
            return moveDirections;
        }
        return Vector3.zero;
    }

    /// <summary>
    /// Returns a bool based on if the standard input for Sprint has been held down this frame.
    /// </summary>
    /// <returns></returns>
    public bool GetSprintInput()
    {
        if (CanProcessGameInput())
        {
            return Input.GetButton("Sprint");
        }
        return false;
    }

    public bool GetDashInputDown()
    {
        if (CanProcessGameInput())
        {
            return Input.GetButtonDown("Dash");
        }
        return false;
    }

    /// <summary>
    /// Returns a bool based on if the standard input for Jump has been pressed down this frame.
    /// </summary>
    /// <returns></returns>
    public bool GetJumpInputDown()
    {
        if (CanProcessGameInput())
        {
            return Input.GetButtonDown("Jump");
        }
        return false;
    }

    #region Look Inputs
    /// <summary>
    /// Grabs the horizontal look input for both controllers/gamepads and mouse
    /// </summary>
    /// <returns></returns>
    public float GetLookInputsHorizontal()
    {
        return GetMouseOrStickLookAxis("Mouse X", "Look X", invertXAxis);
    }

    /// <summary>
    /// Grabs the vertical look input for both controllers/gamepads and mouse
    /// </summary>
    /// <returns></returns>
    public float GetLookInputsVertical()
    {
        return GetMouseOrStickLookAxis("Mouse Y", "Look Y", invertYAxis);
    }

    private float GetMouseOrStickLookAxis(string mouseInputName, string stickInputName, bool invertInput)
    {
        if (CanProcessGameInput())
        {
            //Checks if input is from mouse or controller
            bool isGamepad = Input.GetAxis(stickInputName) != 0f;
            float i = isGamepad ? Input.GetAxis(stickInputName) : Input.GetAxisRaw(mouseInputName);

            //Handles inverting input for...those who enjoy it
            if (invertInput)
                i *= -1f;

            //Applies sensitivity multiplier
            i *= lookSensitivity;

            if (isGamepad)
            {
                //Only controller input needs to be multiplied by delta time, as mouse input already is
                i *= Time.deltaTime;
            }
            else
            {
                //Reduce mouse input to be roughly equal to stick movement
                i *= 0.01f;
            }
            return i;
        }
        return 0f;
    }
    #endregion
}
