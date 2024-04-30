using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardUserInputReaderPleasantness : UserInputReaderPleasantness
{
    new void Update()
    {
        bool okButtonPressed = false;
        string joystickMovementUpDown = "none";
        float[] axisValues = new float[2];

        if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
            okButtonPressed = true;

        if (Input.GetKeyDown(KeyCode.UpArrow) ||Input.GetKeyDown(KeyCode.Keypad5))
            joystickMovementUpDown = "up";
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.Keypad2))
            joystickMovementUpDown = "down";

        axisValues[0] += (Input.GetKey(KeyCode.LeftArrow) ||Input.GetKey(KeyCode.Keypad1)) ? -1 : 0;
        axisValues[1] += (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.Keypad3)) ? 1 : 0; 

        if (joystickMovementUpDown == "down")  // joystick latch is down
        {
            currSelectedItem = Math.Min(3, currSelectedItem + 1);
        }
        else if (joystickMovementUpDown == "up")  // joystick latch is up
        {
            currSelectedItem = Math.Max(0, currSelectedItem - 1);
        }

        PleasantnessText.color = Color.black;
        IntensityText.color = Color.black;
        SubmitText.color = Color.black;
        PleasantnessFill.color = Color.white;
        IntensityFill.color = Color.white;
        switch (currSelectedItem)
        {
            case 0:
                break;
            case 1:
                PleasantnessText.color = Color.green;
                PleasantnessFill.color = Color.green;

                for (int i = 0; i < axisValues.Length; i++)
                {
                    if (Math.Abs(axisValues[i]) > 0.3)
                        Pleasantness.value += axisValues[i] * SliderCoeff;
                }

                break;
            case 2:
                IntensityText.color = Color.green;
                IntensityFill.color = Color.green;

                for (int i = 0; i < axisValues.Length; i++)
                {
                    if (Math.Abs(axisValues[i]) > 0.3)
                        Intensity.value += axisValues[i] * SliderCoeff;
                }

                break;
            case 3:
                SubmitText.color = Color.green;
                if (okButtonPressed)
                    Submit();

                break;
            default:
                break;
        }
    }
}
