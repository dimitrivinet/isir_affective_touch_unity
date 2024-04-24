using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserInputReaderPleasantness : MonoBehaviour
{
[SerializeField]
    private int[] OkButtons;

    [SerializeField]
    private int[] UpToDownAxes;

    [SerializeField]
    private int[] LeftToRightAxes;

    [SerializeField]
    private Slider Pleasantness;

    [SerializeField]
    private TextMeshProUGUI PleasantnessText;

    [SerializeField]
    private Slider Intensity;

    [SerializeField]
    private TextMeshProUGUI IntensityText;
    
    [SerializeField]
    private float SliderCoeff;

    [SerializeField]
    private TextMeshProUGUI SubmitText;

    [SerializeField]
    private SwitchControllerPassthrough SwitchControllerPassthrough;

    private int currSelectedItem;

    void Reset()
    {   
        currSelectedItem = 0;
    }

    void Submit()
    {
        // submit answer

        Reset();
    }

    void Awake()
    {
        Reset();
    }

    void Update()
    {
        bool okButtonPressed = false;
        string joystickMovementUpDown = "none";
        float[] axisValues = new float[LeftToRightAxes.Length];

        foreach (var joycon in SwitchControllerPassthrough.Joycons.Values)
        {
            foreach (var buttonId in OkButtons)
            {
                if (joycon.ButtonLatches[buttonId].Rising)
                    okButtonPressed = true;
            }

            foreach (var axId in UpToDownAxes)
            {
                float axValue = joycon.Axes[axId];
                if (joycon.AxisLatches[axId].Rising)
                {
                    if (axValue < -0.5)
                        joystickMovementUpDown = "up";
                    else
                        joystickMovementUpDown = "down";
                }
            }

            int i = 0;
            foreach (var axId in LeftToRightAxes)
            {
                float axValue = joycon.Axes[axId];
                axisValues[i] = axValue;
                i++;
            }
        }

        if (joystickMovementUpDown == "down")  // joystick latch is down
        {
            currSelectedItem = Math.Min(2, currSelectedItem + 1);
        }
        if (joystickMovementUpDown == "up")  // joystick latch is up
        {
            currSelectedItem = Math.Max(0, currSelectedItem - 1);
        }

        PleasantnessText.color = Color.black;
        IntensityText.color = Color.black;
        SubmitText.color = Color.black;
        switch (currSelectedItem)
        {
            case 0:
                break;
            case 1:
                PleasantnessText.color = Color.green;

                for (int i = 0; i < axisValues.Length; i++)
                {
                    Pleasantness.value += axisValues[i] * SliderCoeff;
                }

                break;
            case 2:
                IntensityText.color = Color.green;

                for (int i = 0; i < axisValues.Length; i++)
                {
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
