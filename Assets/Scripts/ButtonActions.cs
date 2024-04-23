using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using SFB;
using System.IO;
using StackExchange.Redis;

public class ButtonActions : MonoBehaviour
{
    public TextMeshProUGUI CsvPath;
    public TextMeshProUGUI RedisConnString;
    public ObjectPinning VisualTrajectory;
    public Experiment ExperimentManager;

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
    }

    public void RunExperiment()
    {
        ExperimentManager.SpawnStartExperiment();
    }

    public void GoToExperiment()
    {
        SceneManager.LoadScene("Experiment");
    }


    public void GoToExperimentChecked()
    {
        if (RedisConnString.color == Color.green && MainManager.Instance.RedisConnString != null)
        {
            SceneManager.LoadScene("Experiment");
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
    }

    public void TryRedis() 
    {
        try
        {
            using (ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(RedisConnString.text)) {}
            RedisConnString.color = Color.green;
        }
        catch
        {
            RedisConnString.color = Color.red;
        }        
    }

    public void SaveRedisConnString()
    {
        MainManager.Instance.RedisConnString = RedisConnString.text;
    }

    public void ToggleVisualTrajectory()
    {
        VisualTrajectory.freeze = !VisualTrajectory.freeze;
    }
}
