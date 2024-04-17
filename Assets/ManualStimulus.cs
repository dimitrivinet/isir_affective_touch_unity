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

    private IEnumerator WaitAndRun(float seconds, Action<string> callable, string arg)
    {
        yield return new WaitForSeconds(seconds);
        callable(arg);
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
        yield return null;
    }

    UnityAction GetTaskOnClick(string type, string arg, int id)
    {
        void TaskOnClickWithArg()
        {
            if (VisualManager.GoByTheRouteOnceRunning)
                return;
                
            if (UseManualText)
            {
                // RobotDelayOffset = float.Parse(ManualText.text.Trim());
                // VibratorDelayOffset = float.Parse(ManualText.text.Trim());
                arg = ManualText.text.Trim();
            }
            else
            {
                RobotDelayOffset = RobotDelays[id];
                VibratorDelayOffset = VibratorDelays[id];
            }

            float visualSpeed;
            try
            {
                visualSpeed = float.Parse(arg);
            }
            catch
            {
                Debug.Log("cant parse arg");
                return;
            }

            string tactileArg;
            // if (visualSpeed < 3)
            if (id <= 4)
            {
                tactileArg = "1";
            }
            else
            {
                tactileArg = "10";
            }
            float tactileSpeed = float.Parse(tactileArg);
            // tactileArg = arg;  // temporary override

            Debug.Log(string.Format("Button clicked: {0}:{1}", type, arg));

            float visualApproachTime = (float)VisualManager.RouteLengths[0] / visualSpeed;
            float congurentVisualApproachTime = (float)VisualManager.RouteLengths[0] / tactileSpeed;
            float trueApproachTime = congurentVisualApproachTime - (congurentVisualApproachTime - visualApproachTime);
            

            StrokeLengthCm = visualSpeed / tactileSpeed * 9f;

            Func<string, IEnumerator> TactileStroke;
            float delay;
            if (type == "robot")
            {
                TactileStroke = RobotStroke;
                
                float tactileApproachTime = 12.8f / tactileSpeed;  // 12.8 = approach distance in cm
                delay = trueApproachTime - tactileApproachTime - RobotDelayOffset;
                // delay = visualApproachTime - tactileApproachTime - RobotDelays[id];
            }
            else if (type == "vibreurs")
            {
                TactileStroke = VibratorStroke;
                delay = trueApproachTime - VibratorDelayOffset;
                // delay = (float)VisualManager.RouteLengths[0] / speed_cms - VibratorDelays[id];
            }
            else
            {
                return;
            }
            Debug.Log("delai total: " + delay);

            if (delay < 0)  // TactileStroke should run before VisualStroke
            {
                // // execute VisualStroke after abs(delay) seconds
                // IEnumerator coroutine = WaitAndRun(Math.Abs(delay), VisualStroke, arg);
                // StartCoroutine(coroutine);

                StartCoroutine(TactileStroke(tactileArg));
                
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
                IEnumerator coroutine = WaitAndRun(Math.Abs(delay), VisualStroke, arg);
                StartCoroutine(coroutine);
            }
            else  // TactileStroke should run after VisualStroke
            {
                // execute TactileStroke after delay seconds
                IEnumerator coroutine = WaitAndRun(delay, TactileStroke(tactileArg));
                StartCoroutine(coroutine);

                VisualStroke(arg);
            }
        }
        return TaskOnClickWithArg;
    }

    private IEnumerator RobotStroke(string speed_cms_s)
    {
        Redis.Publish(RedisChannels.stroke_speed, speed_cms_s.ToString());
        yield return null;        
    }

    private IEnumerator VibratorStroke(string speed_cms_s)
    {
        float speed_cms = float.Parse(speed_cms_s);
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

    private void VisualStroke(string speed_cms)
    {
        StartCoroutine(VisualManager.GoByTheRouteOnce(speed_cms, StrokeLengthCm));
    }


}
