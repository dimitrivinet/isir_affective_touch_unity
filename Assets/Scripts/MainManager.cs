using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

[System.Serializable]
public class Trial
{
    public string Stimulus;
    public float VisualSpeed;
    public float TactileSpeed;

    public Trial(string stimulus, float visualSpeed, float tactileSpeed)
    {
        this.Stimulus = stimulus;
        this.VisualSpeed = visualSpeed;
        this.TactileSpeed = tactileSpeed;
    }
}

public class MainManager : MonoBehaviour
{
    public static MainManager Instance;

    public List<Trial> trials;
    public List<string> trials_str;
    public string RedisConnString;
    public string OutputCsvPath;

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
                var split = trial_str.Split(",");
                string stimulus = split[0];
                float tactileSpeed = float.Parse(split[1]);
                float visualSpeed = float.Parse(split[2]);

                trials.Add(new Trial(stimulus, visualSpeed, tactileSpeed));
            }
            catch
            {
                Debug.Log($"error parsing trial {trial_str}");
                continue;
            }
        }
    }
}
