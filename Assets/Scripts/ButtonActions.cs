using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using SFB;
using System.IO;
using StackExchange.Redis;
using System;
using System.Text.RegularExpressions;

public class ButtonActions : MonoBehaviour
{
    public TextMeshProUGUI CsvPath;
    public TextMeshProUGUI RedisConnString;
    public TextMeshProUGUI LoadedNCsv;
    public ObjectPinning VisualTrajectory;
    public Experiment ExperimentManager;
    public GameObject CongruencyExperiment;
    public GameObject PleasantnessExperiment;

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleVisualTrajectory();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            RunExperiment();
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            SwitchExperiments();
        }
    }
    
    public void SwitchExperiments()
    {
        CongruencyExperiment.SetActive(!CongruencyExperiment.activeSelf);
        PleasantnessExperiment.SetActive(!PleasantnessExperiment.activeSelf);
    }

    public void RunExperiment()
    {
        ExperimentManager.SpawnStartExperiment();
    }

    public void GoToExperimentLeft()
    {
        SceneManager.LoadScene("Experiment.L");
    }

    public void GoToExperimentRight()
    {
        SceneManager.LoadScene("Experiment.R");
    }

    public void GoToExperimentCheckedLeft()
    {
        if (RedisConnString.color == Color.green && MainManager.Instance.RedisConnString != null)
        {
            SceneManager.LoadScene("Experiment.L");
        }
    }

    public void GoToExperimentCheckedRight()
    {
        if (RedisConnString.color == Color.green && MainManager.Instance.RedisConnString != null)
        {
            SceneManager.LoadScene("Experiment.R");
        }
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene("Menu");
    }

    public void GetCsvPath()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel("Load CSV file for participant:", "", "", false);

        if (paths.Length == 0)
        {
            return;
        }

        CsvPath.text = paths[0];
        CsvPath.color = Color.white;
    }

    public void LoadCsvData()
    {
        var path = CsvPath.text;
        if (!File.Exists(path))
        {
            CsvPath.color = Color.red;
            return;
        }
        List<string> contents = new();
        using (StreamReader sr = File.OpenText(path))
        {
            string s;
            while ((s = sr.ReadLine()) != null)
            {
                contents.Add(s);
            }
        }
        MainManager.Instance.trials_str = contents;
        MainManager.Instance.ParseTrials();

        LoadedNCsv.text = $"Loaded {MainManager.Instance.trials.Count} trials.";
        LoadedNCsv.gameObject.SetActive(true);

        MainManager.Instance.OutputCsvPath = CsvPath.text + ".out";
    }

    private string ParseRedisConnString(string redisConnString)
    {
        string ret = redisConnString.Trim();
        ret = Regex.Replace(ret, @"[^a-zA-Z0-9.:]+", "");
        return ret;
    }

    public void TryRedis() 
    {
        string redisConnString = ParseRedisConnString(RedisConnString.text);
        try
        {
            Debug.Log($"Connecting to redis at '{redisConnString}'");
            // using (ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("10.42.0.1:6379")) {}
            using (ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(redisConnString)) {}
            RedisConnString.color = Color.green;
        }
        catch (Exception ex)
        {
            Debug.Log("Exception: " + ex.Message);
            RedisConnString.color = Color.red;
        }        
        MainManager.Instance.RedisConnString = redisConnString;
    }

    public void SaveRedisConnString()
    {
        string redisConnString = ParseRedisConnString(RedisConnString.text);
        MainManager.Instance.RedisConnString = redisConnString;
    }

    public void ToggleVisualTrajectory()
    {
        VisualTrajectory.freeze = !VisualTrajectory.freeze;
    }
}
