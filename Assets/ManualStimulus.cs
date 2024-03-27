using System;
using System.Collections;
using System.Threading;
using Meta.WitAi.Utilities;
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

    UnityAction GetTaskOnClick(string type, string arg, int id)
    {
        void TaskOnClickWithArg()
        {
            if (VisualManager.GoByTheRouteOnceRunning)
                return;

            float speed_cms;
            try
            {
                speed_cms = float.Parse(arg);
            }
            catch
            {
                return;
            }

            string tactileArg;
            if (speed_cms < 3)
            {
                tactileArg = "1";
            }
            else
            {
                tactileArg = "10";
            }

            Debug.Log(string.Format("Button clicked: {0}:{1}", type, arg));

            Func<string, IEnumerator> TactileStroke;
            float delay;
            if (type == "robot")
            {
                TactileStroke = RobotStroke;
                
                float visualApproachTime = (float)VisualManager.RouteLengths[0] / speed_cms - 0.1f;
                float tactileApproachTime = 12.8f / float.Parse(tactileArg);
                delay = visualApproachTime - tactileApproachTime - RobotDelayOffset;
            }
            else if (type == "vibreurs")
            {
                TactileStroke = VibratorStroke;
                delay = (float)VisualManager.RouteLengths[0] / speed_cms - 0.1f;
            }
            else
            {
                return;
            }

            if (delay < 0)  // TactileStroke should run before VisualStroke
            {
                // execute VisualStroke after abs(delay) seconds
                IEnumerator coroutine = WaitAndRun(Math.Abs(delay), VisualStroke, arg);
                StartCoroutine(coroutine);

                StartCoroutine(TactileStroke(tactileArg));
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
        StartCoroutine(VisualManager.GoByTheRouteOnce(speed_cms));
    }


}
