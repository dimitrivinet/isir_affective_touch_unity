using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

public class Experiment : MonoBehaviour
{
    public bool Stop = false;
    public float SecondsBetweenStimTypes = 90;
    public float SecondsBetweenStims = 20;
    public ManualStimulus StimulusManager;
    public RedisManager Redis;
    public Vibrators VibratorsManager;
    [SerializeField]
    public Trial[] Trials;
    private readonly float[] ValidSpeeds = { 0.5F, 1, 3, 10, 30 };
    private IEnumerator RoutineEnumerator;
    private Coroutine Routine;

    public void SpawnStartExperiment()
    {
        RoutineEnumerator = StartExperiment();
        Routine = StartCoroutine(RoutineEnumerator);
    }

    public void KillStartExperiment()
    {
        Stop = false;
        StopCoroutine(Routine);
    }

    private IEnumerator StartExperiment()
    {
        List<Trial> trials;
        Debug.Log("Get trials list");
        if (MainManager.Instance != null)
        {
            trials = MainManager.Instance.trials;
        }
        else
        {
            trials = new List<Trial>(Trials);
        }

        if (!trials.Any())
        {
            yield break;
        }

        Debug.Log("Check if trials are ok");
        foreach (var trial in trials)
        {
            if (!ValidSpeeds.Contains(trial.TactileSpeed))
            {
                Debug.LogError($"Invalid speed {trial.TactileSpeed}");
                yield break;
            }
        }

        Debug.Log("Connect STM");
        VibratorsManager.ConnectSTM();
        Debug.Log("Setup Redis variables");
        Redis.Set(RedisChannels.user_gave_input, "false");

        int i = 0;
        Debug.Log("Set initial trial type");
        var last_trial_type = trials[0].Stimulus;

        Debug.Log("Main loop");
        while (i < trials.Count())
        {
            if (Stop)
            {
                yield break;
            }

            var trial = trials[i];

            Debug.Log($"trial n.{i}: {trial}");
            Redis.Set(RedisChannels.current_trial, $"{i + 1}");

            if (trial.Stimulus != last_trial_type)
            {
                Debug.Log("Changing trial type");
                Redis.Set(RedisChannels.sleeping, SecondsBetweenStimTypes.ToString());
                Redis.Set($"{RedisChannels.sleeping}:time", DateTime.UtcNow.ToString("o"));
                yield return new WaitForSeconds(SecondsBetweenStimTypes);
                Redis.Set(RedisChannels.sleeping, "false");
                last_trial_type = trial.Stimulus;
            }

            if (Stop)
            {
                yield break;
            }

            Redis.Set(RedisChannels.stimulus_done, "false");

            StrokeType strokeType = StrokeType.None;
            if (trial.Stimulus == "2")  // robot
            {
                strokeType = StrokeType.Robot;
            }
            else if (trial.Stimulus == "1")  // vibrators
            {
                strokeType = StrokeType.Vibrator;
            }

            Debug.Log($"stimulate once with parameters: tactileSpeed={trial.TactileSpeed}, visualSpeed={trial.VisualSpeed}, strokeType={strokeType}, delay={0.0f}");
            yield return StimulusManager.StimulateOnce(trial.TactileSpeed, trial.VisualSpeed, strokeType, 0.0f);

            // wait for stimulus done to be true
            Debug.Log("waiting for stimulus to be done");
            bool cont = Redis.Get(RedisChannels.stimulus_done) == "true";
            cont = true;  // skip waiting for answer
            while (!cont)
            {
                if (Stop)
                {
                    yield break;
                }

                yield return new WaitForSeconds(0.1f);
                cont = Redis.Get(RedisChannels.stimulus_done) == "true";
            }

            cont = Redis.Get(RedisChannels.user_gave_input) == "true";
            cont = true;  // skip waiting for answer
            float toSleepS = SecondsBetweenStims - 3.0f;
            float checkIntervalS = 0.1f;
            Debug.Log("waiting for user answer");
            while (!cont)
            {
                cont = Redis.Get(RedisChannels.user_gave_input) == "true";
                if (Redis.Get(RedisChannels.user_gave_input) == "redo")
                {
                    Debug.Log("redo");
                    i--;
                    cont = true;
                }
                yield return new WaitForSeconds(checkIntervalS / 1000.0f);
                toSleepS -= checkIntervalS;
            }

            Debug.Log($"sleeping between stim for {toSleepS + 3.0f} seconds");
            if (toSleepS > 0)
            {
                Redis.Set(RedisChannels.sleeping, toSleepS.ToString());
                Redis.Set($"{RedisChannels.sleeping}:time", DateTime.UtcNow.ToString("o"));
                yield return new WaitForSeconds(toSleepS);
            }

            Redis.Set(RedisChannels.user_gave_input, "false");
            Redis.Set(RedisChannels.sleeping, "3");
            Redis.Set($"{RedisChannels.sleeping}:time", DateTime.UtcNow.ToString("o"));
            yield return new WaitForSeconds(3.0f);
            Redis.Set(RedisChannels.sleeping, "false");

            Debug.Log("next stimulus");

            i += 1;
        }
        Debug.Log("All trials done");
        yield break;
    }
}
