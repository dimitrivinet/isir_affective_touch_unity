using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class Trial
{
    public string Stimulus;
    public float Speed;

    public Trial(string stimulus, float speed)
    {
        this.Stimulus = stimulus;
        this.Speed = speed;
    }
}

public class MainManager : MonoBehaviour
{
    public static MainManager Instance;

    public List<Trial> trials;
    public List<string> trials_str;
    public string RedisConnString;

    private void Awake()
    {
        // start of new code
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        // end of new code

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ParseTrials()
    {
        foreach (string trial_str in trials_str)
        {
            try
            {
                var split = trial_str.Split(";");
                string stimulus = split[0];
                float speed = float.Parse(split[1]);

                trials.Add(new Trial(stimulus, speed));
            }
            catch
            {
                continue;
            }
        }
    }

    public void RunTrials()
    {
        foreach (Trial trial in trials)
        {
            if (trial.Stimulus == "1")  // vibrators
            {}
            else if (trial.Stimulus == "2")  // robot
            {}
        }
    }
}
