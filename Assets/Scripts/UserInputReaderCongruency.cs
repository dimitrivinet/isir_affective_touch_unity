using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserInputReaderCongruency : MonoBehaviour
{
    [SerializeField]
    private int[] OkButtons;

    [SerializeField]
    private int[] UpToDownAxes;

    [SerializeField]
    private int[] LeftToRightAxes;

    [SerializeField]
    private Toggle Congruency;

    [SerializeField]
    private TextMeshProUGUI CongruencyText;

    [SerializeField]
    private Toggle Incongruency;

    [SerializeField]
    private TextMeshProUGUI IncongruencyText;

    [SerializeField]
    private TextMeshProUGUI SubmitText;

    [SerializeField]
    private SwitchControllerPassthrough SwitchControllerPassthrough;
    
    [SerializeField]
    private Experiment ExperimentManager;

    [SerializeField]
    private string OutputCsvPath;

    private int currSelectedItem;
    private int currSelectedToggle;
    private bool writeToFile;

    void Reset()
    {   
        Debug.Log("reset");
        currSelectedItem = 0;
        currSelectedToggle = 0;
        Congruency.isOn = false;
        Incongruency.isOn = false;
        ExperimentManager.UserGaveInput = "true";
    }

    void Submit()
    {
        if (!(Congruency.isOn || Incongruency.isOn))
            return;
        
        Trial trial = ExperimentManager.Trials[ExperimentManager.CurrTrial];
        bool congruencyReal = trial.TactileSpeed.ToString() == trial.VisualSpeed.ToString();

        string[] data = new[]{
            trial.Stimulus,
            trial.TactileSpeed.ToString(),
            trial.VisualSpeed.ToString(),
            congruencyReal ? "congruent" : "incongruent",
            Congruency.isOn ? "congruent" : "incongruent",
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

    void Awake()
    {
        Reset();

        if (MainManager.Instance.OutputCsvPath != null)
        {
            OutputCsvPath = MainManager.Instance.OutputCsvPath;
        }
        if (OutputCsvPath == null)
        {
            writeToFile = false;
        }
        else
        {
            writeToFile = true;
            File.Create(OutputCsvPath);
        }
    }

    void Update()
    {
        bool okButtonPressed = false;
        string joystickMovementUpDown = "none";
        string joystickMovementLeftRight = "none";
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
                    else if (axValue > 0.5)
                        joystickMovementUpDown = "down";
                }
            }

            foreach (var axId in LeftToRightAxes)
            {
                float axValue = joycon.Axes[axId];
                if (joycon.AxisLatches[axId].Rising)
                {
                    if (axValue < -0.5)
                        joystickMovementLeftRight = "left";
                    else if (axValue > 0.5)
                        joystickMovementLeftRight = "right";
                }
            }
        }

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
        Debug.Log(currSelectedItem + " " + currSelectedToggle + " " + okButtonPressed);
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
