using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

public class Experiment : MonoBehaviour
{
    public bool Stop = false;
    public double TravelDistanceCm = 9.0;
    public double SecondsBetweenStimTypes = 90;
    public RedisManager Redis;
    public Vibrators VibratorsManager;
    private readonly float[] ValidSpeeds = { 0.5F, 1, 3, 10, 30 };

    public void StartExperiment()
    {
        var trials = MainManager.Instance.trials;

        foreach (var trial in trials)
        {
            if (!ValidSpeeds.Contains(trial.Speed))
            {
                Debug.LogError($"Invalid speed {trial.Speed}");
                return;
            }
        }

        VibratorsManager.ConnectSTM();
        Redis.Set(RedisChannels.user_gave_input, "false");

        int i = 0;
        var last_trial_type = trials[0].Stimulus;

        while (i < trials.Count())
        {
            if (Stop)
            {
                return;
            }

            var trial = trials[i];

            Debug.Log($"trial n.{i}: {trial}");
            Redis.Set(RedisChannels.current_trial, $"{i + 1}");

            if (trial.Stimulus != last_trial_type)
            {
                Debug.Log("Changing trial type");
                Redis.Set(RedisChannels.sleeping, SecondsBetweenStimTypes.ToString());
                Redis.Set($"{RedisChannels.sleeping}:time", DateTime.UtcNow.ToString("o"));
                Thread.Sleep((int)(SecondsBetweenStimTypes * 1000));
                Redis.Set(RedisChannels.sleeping, "false");
                last_trial_type = trial.Stimulus;
            }

            if (Stop)
            {
                return;
            }

            Redis.Set(RedisChannels.stimulus_done, "false");

            var speed_cms = trial.Speed;

            if (trial.Stimulus == "2")  // robot
            {
                Redis.Publish(RedisChannels.stroke_speed, speed_cms.ToString());

                // wait for robot control to set stimulus done to true
                while (!(Redis.Get(RedisChannels.stimulus_done) == "true"))
                {
                    if (Stop)
                    {
                        return;
                    }

                    Thread.Sleep(100);
                }
            }
            else if (trial.Stimulus == "1")  // vibrators
            {
                double time_margin_s = 0.5;
                double stimulation_time = TravelDistanceCm / speed_cms + time_margin_s;
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
                Thread.Sleep((int)(stimulation_time * 1000));
                Debug.Log("end stim");

                VibratorsManager.StopVibrating();
                VibratorsManager.SetActive(false);

                // stimulus done, prepare for next stimulus
                Redis.Set(RedisChannels.stimulus_done, "true");
            }

            bool cont = false;
            while(!cont)
            {
                cont = Redis.Get(RedisChannels.user_gave_input) == "true";
                if (Redis.Get(RedisChannels.user_gave_input) == "redo")
                {
                    i--;
                    cont = true;
                }                                        
                Thread.Sleep(100);
            }
            Redis.Set(RedisChannels.user_gave_input, "false");
            Redis.Set(RedisChannels.sleeping, "3");
            Redis.Set($"{RedisChannels.sleeping}:time", DateTime.UtcNow.ToString("o"));
            Thread.Sleep(3000);
            Redis.Set(RedisChannels.sleeping, "false");

            Debug.Log("next stimulus");

            i += 1;
        }
    }
}
