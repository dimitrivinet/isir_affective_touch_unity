using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardUserInputReaderCongruency : UserInputReaderCongruency
{
    new void Update()
    {
        bool okButtonPressed = false;
        string joystickMovementUpDown = "none";
        string joystickMovementLeftRight = "none";

        if (Input.GetKeyDown(KeyCode.LeftArrow) ||Input.GetKeyDown(KeyCode.Keypad1))
            joystickMovementLeftRight = "left";
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Keypad3))
            joystickMovementLeftRight = "right";

        if (Input.GetKeyDown(KeyCode.UpArrow) ||Input.GetKeyDown(KeyCode.Keypad5))
            joystickMovementUpDown = "up";
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.Keypad2))
            joystickMovementUpDown = "down";

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            okButtonPressed = true;

        if (joystickMovementUpDown == "down")  // joystick latch is down
        {
            currSelectedItem = Math.Min(2, currSelectedItem + 1);
        }
        else if (joystickMovementUpDown == "up")  // joystick latch is up
        {
            currSelectedItem = Math.Max(0, currSelectedItem - 1);
        }

        IncongruencyText.color = Color.black;
        CongruencyText.color = Color.black;
        SubmitText.color = Color.black;
        switch (currSelectedItem)
        {
            case 0:
                break;
            case 1:  // selecting one of the toggles
                if (joystickMovementLeftRight == "left")
                {
                    currSelectedToggle = Math.Max(0, currSelectedToggle - 1);
                }
                else if (joystickMovementLeftRight == "right")
                {
                    currSelectedToggle = Math.Min(1, currSelectedToggle + 1);
                }
                switch (currSelectedToggle)
                {
                    case 0:
                        CongruencyText.color = Color.green;
                        if (okButtonPressed)
                        {
                            Congruency.isOn = true;
                            Incongruency.isOn = false;
                        }
                        break;
                    case 1:
                        IncongruencyText.color = Color.green;
                        if (okButtonPressed)
                        {
                            Incongruency.isOn = true;
                            Congruency.isOn = false;
                        }
                        break;
                    default:
                        break;
                }
                break;
            case 2:
                SubmitText.color = Color.green;
                if (okButtonPressed)
                    Submit();
                break;
            default:
                break;
        }
    }
}
