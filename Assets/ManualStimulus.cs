using System;
using System.Collections;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ManualStimulus : MonoBehaviour
{
    public Button[] RobotButtons;
    public float[] RobotDelays;
    public Button[] VibratorButtons;
    public float[] VibratorDelays;
    public RedisManager Redis;
    public Vibrators VibratorsManager;
    public RouteFollow VisualManager;
    public float RobotDelayOffset;
    public float VibratorDelayOffset;
    public TMP_InputField ManualText;
    public bool UseManualText = false;
    public float StrokeLengthCm = 9f;
    private bool RobotMoving = false;
    private bool IsRobotMovingRunning = false;
    public bool TurnGreen;

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < RobotButtons.Length; i++)
        {
            Button btn = RobotButtons[i].GetComponent<Button>();
            TextMeshProUGUI text = btn.GetComponentInChildren<TextMeshProUGUI>();
            btn.onClick.AddListener(GetTaskOnClick("robot", text.text, i));
        }
        for (int i = 0; i < VibratorButtons.Length; i++)
        {
            Button btn = VibratorButtons[i].GetComponent<Button>();
            TextMeshProUGUI text = btn.GetComponentInChildren<TextMeshProUGUI>();
            btn.onClick.AddListener(GetTaskOnClick("vibreurs", text.text, i));
        }
    }

    void Update()
    {
        VisualManager.TurnGreen = TurnGreen;
    }

    private IEnumerator WaitAndRun(float seconds, Action<float, StrokeType> callable, float speed_cms, StrokeType strokeType)
    {
        yield return new WaitForSeconds(seconds);
        callable(speed_cms, strokeType);
    }

    private IEnumerator WaitAndRun(float seconds, IEnumerator coroutine)
    {
        yield return new WaitForSeconds(seconds);
        StartCoroutine(coroutine);
    }

    private IEnumerator RunImmediately(Action<string> callable, string arg)
    {
        yield return new WaitForSeconds(0);
        callable(arg);
    }

    IEnumerator IsRobotMoving()
    {
        IsRobotMovingRunning = true;
        RobotMoving = Redis.Get("robot_moving") == "true";
        IsRobotMovingRunning = false;
        yield break;
    }

    public IEnumerator StimulateOnce(float tactileSpeed, float visualSpeed, StrokeType strokeType, float delayOffset)
    {
        if (VisualManager.GoByTheRouteOnceRunning)
            yield break;

        float visualApproachTime = (float)VisualManager.RouteLengths[0] / visualSpeed;
        float congurentVisualApproachTime = (float)VisualManager.RouteLengths[0] / tactileSpeed;
        float trueApproachTime = congurentVisualApproachTime - (congurentVisualApproachTime - visualApproachTime);
        
        StrokeLengthCm = visualSpeed / tactileSpeed * 9f;

        Func<float, IEnumerator> TactileStroke;
        float delay = 0.0f;
        if (strokeType == StrokeType.Robot)
        {
            TactileStroke = RobotStroke;
            
            float tactileApproachTime = 12.8f / tactileSpeed;  // 12.8 = approach distance in cm
            delay = trueApproachTime - tactileApproachTime - RobotDelayOffset;
            // delay = visualApproachTime - tactileApproachTime - RobotDelays[id];
        }
        else if (strokeType == StrokeType.Vibrator)
        {
            TactileStroke = VibratorStroke;
            delay = trueApproachTime - VibratorDelayOffset;
            // delay = (float)VisualManager.RouteLengths[0] / speed_cms - VibratorDelays[id];
        }
        else
        {
            yield break;
        }
        Debug.Log("delai total: " + delay);

        if (delay < 0)  // TactileStroke should run before VisualStroke
        {
            // // execute VisualStroke after abs(delay) seconds
            // IEnumerator coroutine = WaitAndRun(Math.Abs(delay), VisualStroke, arg);
            // StartCoroutine(coroutine);

            StartCoroutine(TactileStroke(tactileSpeed));
            
            StartCoroutine(IsRobotMoving());
            int timeout = 3_000;  // 3 seconds
            while (!RobotMoving && timeout > 0)
            {
                if (!IsRobotMovingRunning)
                {
                    StartCoroutine(IsRobotMoving());
                }
                Thread.Sleep(1000 / 60);
                timeout -= 1000 / 60;
            }
            // Thread.Sleep((int)(Math.Abs(delay) * 1000));
            // VisualStroke(arg);
            IEnumerator coroutine = WaitAndRun(Math.Abs(delay), VisualStroke, visualSpeed, strokeType);
            StartCoroutine(coroutine);
        }
        else  // TactileStroke should run after VisualStroke
        {
            // execute TactileStroke after delay seconds
            IEnumerator coroutine = WaitAndRun(delay, TactileStroke(tactileSpeed));
            StartCoroutine(coroutine);

            VisualStroke(visualSpeed, strokeType);
        }
    }

    UnityAction GetTaskOnClick(string type, string arg, int id)
    {
        Debug.Log(string.Format("Button clicked: {0}:{1}", type, arg));

        StrokeType strokeType;
        float delayOffset = 0.0f;
        if (type == "robot")
        {
            strokeType = StrokeType.Robot;
            delayOffset = RobotDelays[id];

        }
        else if (type == "vibreurs")
        {
            strokeType = StrokeType.Vibrator;
            delayOffset = VibratorDelays[id];
        }
        else
        {
            return () => {};  // in case the type was incorrect, return a function that returns nothing
        }

        float tactileSpeed;
        if (id <= 4)
        {
            tactileSpeed = 1.0f;
        }
        else
        {
            tactileSpeed = 10.0f;
        }
        float visualSpeed = float.Parse(arg);

        void TaskOnClickWithArg()
        {
            StartCoroutine(StimulateOnce(tactileSpeed, visualSpeed, strokeType, delayOffset));
        }
        
        return TaskOnClickWithArg;
    }

    private IEnumerator RobotStroke(float speed_cms)
    {
        Redis.Publish(RedisChannels.stroke_speed, speed_cms.ToString());
        yield break;        
    }

    private IEnumerator VibratorStroke(float speed_cms)
    {
        float time_margin_s = 0.5f;
        float stimulation_time = 9.0f / speed_cms + time_margin_s;
        string speed_cms_str;
        if (speed_cms < 1)
        {
            speed_cms_str = speed_cms.ToString();
        }
        else
        {
            var speed_cms_int = (int)speed_cms;
            speed_cms_str = speed_cms_int.ToString();
        }

        VibratorsManager.SetActive(true);
        VibratorsManager.SetVibrate(speed_cms_str, 0.5);
        VibratorsManager.LoadWav();
        VibratorsManager.TrigWav();

        Debug.Log("start stim, speed: " + speed_cms_str);
        yield return new WaitForSecondsRealtime(stimulation_time);
        Debug.Log("end stim");

        VibratorsManager.StopVibrating();
        VibratorsManager.SetActive(false);

        // stimulus done, prepare for next stimulus
        Redis.Set(RedisChannels.stimulus_done, "true");
    }

    private void VisualStroke(float speed_cms, StrokeType strokeType)
    {
        StartCoroutine(VisualManager.GoByTheRouteOnce(speed_cms, StrokeLengthCm, strokeType));
    }


}
