using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserInputReaderPleasantness : MonoBehaviour
{
    [SerializeField]
    protected int[] OkButtons;

    [SerializeField]
    protected int[] UpToDownAxes;

    [SerializeField]
    protected int[] LeftToRightAxes;

    [SerializeField]
    protected Slider Pleasantness;

    [SerializeField]
    protected TextMeshProUGUI PleasantnessText;

    [SerializeField]
    protected Slider Intensity;

    [SerializeField]
    protected TextMeshProUGUI IntensityText;
    [SerializeField]
    protected Image PleasantnessFill;
    [SerializeField]
    protected Image IntensityFill;
    
    [SerializeField]
    protected float SliderCoeff;

    [SerializeField]
    protected TextMeshProUGUI SubmitText;

    [SerializeField]
    protected SwitchControllerPassthrough SwitchControllerPassthrough;
    
    [SerializeField]
    protected Experiment ExperimentManager;

    [SerializeField]
    protected string OutputCsvPath;

    protected int currSelectedItem;
    protected System.Random rng;
    protected bool writeToFile;

    protected void Reset()
    {   
        currSelectedItem = 0;
        Pleasantness.value = rng.Next(1, 1001);
        Intensity.value = rng.Next(1, 1001);
        ExperimentManager.UserGaveInput = "true";
    }

    protected void Submit()
    {
        Trial trial = ExperimentManager.Trials[ExperimentManager.CurrTrial];
        float pleasantness = Pleasantness.value / 10.0f;  // [0 to 1000] to [0 to 100]
        float intensity = Intensity.value / 10.0f;
        
        string[] data = new[]{
            trial.Stimulus,
            trial.TactileSpeed.ToString(),
            trial.VisualSpeed.ToString(),
            pleasantness.ToString(),
            intensity.ToString()
        };
        string line = string.Join(',', data);
        Debug.Log(line);
        if (writeToFile)
        {
            using (StreamWriter outputFile = new(OutputCsvPath, true))
            {
                outputFile.WriteLine(line);
            }
        }

        Reset();
    }

    protected void Awake()
    {
        rng = new System.Random();
        Reset();

        if (MainManager.Instance != null)
        {
            if (MainManager.Instance.OutputCsvPath != null)
            {
                OutputCsvPath = MainManager.Instance.OutputCsvPath;
            }    
        }
        try
        {
            if (!File.Exists(OutputCsvPath))
            {
                File.Create(OutputCsvPath);
            }
                writeToFile = true;
        }
        catch
        {
            writeToFile = false;
        }
    }

    protected void Update()
    {
        var joycons = new Dictionary<string, Joycon>(SwitchControllerPassthrough.Joycons);
        bool okButtonPressed = false;
        string joystickMovementUpDown = "none";
        float[] axisValues = new float[LeftToRightAxes.Length];

        foreach (var joycon in joycons.Values)
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
                    else if (axValue > 0.5)
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
            currSelectedItem = Math.Min(3, currSelectedItem + 1);
        }
        else if (joystickMovementUpDown == "up")  // joystick latch is up
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
                    if (Math.Abs(axisValues[i]) > 0.3)
                        Pleasantness.value += axisValues[i] * SliderCoeff;
                }

                break;
            case 2:
                IntensityText.color = Color.green;

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
