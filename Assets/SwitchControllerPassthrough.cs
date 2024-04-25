using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using UnityEngine;

public class ButtonLatch
{
    public bool IsSet = false;
    public bool Rising 
    {
        get { bool ret = rising; rising = false; return ret; }
    }
    private bool rising = false;

    public ButtonLatch()
    {
        IsSet = false;
        rising = false;
    }

    public void Update(bool value)
    {
        if (value)
        {
            if (!IsSet)
            {
                IsSet = true;
                rising = true;
            }
        }
        else
        {
            IsSet = false;
        }
    }
}

[Serializable]
public class Joycon
{
    public string Id;
    public string Name;
    public string Guid;
    public float PowerLevel;
    public float[] Axes;
    public ButtonLatch[] AxisLatches;
    public float[] Buttons;
    public ButtonLatch[] ButtonLatches;
    public float[] Hats;
    public ButtonLatch[] HatLatches;
}

public class SwitchControllerPassthrough : MonoBehaviour
{
    [SerializeField]
    private RedisManager Redis;
    public string DebugVar;

    public Dictionary<string, Joycon> Joycons
    {
        get
        {
            Dictionary<string, Joycon> ret;
            try{
                lock (_lock)
                {
                    ret = new(joycons);
                }
            }
            catch {
                return null;
            }
            return ret;
        }
    }
    private Dictionary<string, Joycon> joycons;
    private Thread updaterThread;
    private readonly object _lock = new();

    public float ParseWithDefault(string toParse, float defaultValue)
    {
        float ret = defaultValue;
        try
        {
            ret = float.Parse(toParse, null);
        }
        catch
        {}

        return ret;
    }

    public int ParseWithDefault(string toParse, int defaultValue)
    {
        int ret = defaultValue;
        try
        {
            ret = int.Parse(toParse, null);
        }
        catch
        {}

        return ret;
    }


    void Awake()
    {
        joycons = new();

        updaterThread = new Thread(new ThreadStart(JoyconUpdater));
        updaterThread.Start();
    }

    public void JoyconUpdater()
    {
        while (Thread.CurrentThread.IsAlive)
        {
            UpdateOnce();
            Thread.Sleep(1000/60);
        }
    }

    public void UpdateOnce()
    {
        Dictionary<string, Joycon> joycons_copy;
        try{
            lock (_lock)
            {
                joycons_copy = new(joycons);
            }
        }
        catch {
            return;
        }

        string connectedJoyconIds = Redis.Get("joycon_ids");
        if (connectedJoyconIds == null)  // error with redis, or passthrough program not up
            return;

        string[] connectedJoyconIdsList = connectedJoyconIds.Split(':');
        Debug.Log(connectedJoyconIds);

        // update connected joycons values, creating when necessary
        for (int i = 0; i < connectedJoyconIdsList.Length; i++)
        {
            string k = connectedJoyconIdsList[i];
            if (!joycons_copy.ContainsKey(k))
            {
                Debug.Log("new joycon added");
                joycons_copy.Add(k, new Joycon());
                joycons_copy[k].Name = Redis.Get($"{k}:name");
                joycons_copy[k].Guid = Redis.Get($"{k}:guid");

                int defaultNum = 0;
                string numAxesStr = Redis.Get($"{k}:num_axes");
                int numAxes = ParseWithDefault(numAxesStr, defaultNum);
                joycons_copy[k].Axes = new float[numAxes];
                joycons_copy[k].AxisLatches = new ButtonLatch[numAxes];
                for (int j = 0; j < numAxes; j++)
                    joycons_copy[k].AxisLatches[j] = new ButtonLatch();
                Debug.Log("num axes: " + numAxes);

                string numButtonsStr = Redis.Get($"{k}:num_buttons");
                int numButtons = ParseWithDefault(numButtonsStr, defaultNum);
                joycons_copy[k].Buttons = new float[numButtons];
                joycons_copy[k].ButtonLatches = new ButtonLatch[numButtons];
                for (int j = 0; j < numButtons; j++)
                    joycons_copy[k].ButtonLatches[j] = new ButtonLatch();
                Debug.Log("num buttons: " + numButtons);

                string numHatsStr = Redis.Get($"{k}:num_hats");
                int numHats = ParseWithDefault(numHatsStr, defaultNum);
                joycons_copy[k].Hats = new float[numHats];
                joycons_copy[k].HatLatches = new ButtonLatch[numHats];
                for (int j = 0; j < numHats; j++)
                    joycons_copy[k].HatLatches[j] = new ButtonLatch();
                Debug.Log("num hats: " + numHats);
            }

            string tempStr;
            float temp;
            float tempDef = 0.0f;

            tempStr = Redis.Get($"{k}:power_level");
            temp = ParseWithDefault(tempStr, tempDef);
            joycons_copy[k].PowerLevel = temp;
            
            for (int j = 0; j < joycons_copy[k].Axes.Length; j++)
            {
                tempStr = Redis.Get($"{k}:axes:{j}");
                temp = ParseWithDefault(tempStr, tempDef);
                joycons_copy[k].Axes[j] = temp;
                joycons_copy[k].AxisLatches[j].Update(temp > 0.5f || temp < -0.5f);
            DebugVar = joycons_copy[k].Axes[0].ToString();
            }
            for (int j = 0; j < joycons_copy[k].Buttons.Length; j++)
            {
                tempStr = Redis.Get($"{k}:buttons:{j}");
                temp = ParseWithDefault(tempStr, tempDef);
                joycons_copy[k].Buttons[j] = temp;
                joycons_copy[k].ButtonLatches[j].Update(temp > 0.0f);
            }
            for (int j = 0; j < joycons_copy[k].Hats.Length; j++)
            {
                tempStr = Redis.Get($"{k}:hats:{j}");
                temp = ParseWithDefault(tempStr, tempDef);
                joycons_copy[k].Hats[j] = temp;
                // Joycons[k].HatLatches[j].Update(???);  // not used
            }
        }

        // cleanup disconnected joycons
        List<string> keysToRemove = new();
        foreach (var k in joycons_copy.Keys)
        {
            if (!Array.Exists(connectedJoyconIdsList, el => el == k))
            {
                keysToRemove.Add(k);
            }
        }
        foreach (var kr in keysToRemove)
        {
            joycons_copy.Remove(kr);
        }

        
        try{
            lock (_lock)
            {
                joycons = new(joycons_copy);
            }
        }
        catch {
            return;
        }
    }
}
