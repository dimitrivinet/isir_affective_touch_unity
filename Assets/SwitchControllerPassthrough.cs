using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using UnityEngine;

public class ButtonLatch
{
    public bool IsSet = false;
    public bool Rising = false;

    public ButtonLatch()
    {
        IsSet = false;
        Rising = false;

    }

    public void Update(bool value)
    {
        if (value)
        {
            if (!IsSet)
            {
                IsSet = true;
                Rising = true;
            }
            else
            {
                Rising = false;
            }
        }
        else
        {
            IsSet = false;
            Rising = false;
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

    public Dictionary<string, Joycon> Joycons;

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
        Joycons = new();
    }

    // Update is called once per frame
    void Update()
    {
        string connectedJoyconIds = Redis.Get("joycon_ids");
        if (connectedJoyconIds == null)  // error with redis, or passthrough program not up
            return;

        string[] connectedJoyconIdsList = connectedJoyconIds.Split(':');
        Debug.Log(connectedJoyconIds);

        // update connected joycons values, creating when necessary
        for (int i = 0; i < connectedJoyconIdsList.Length; i++)
        {
            string k = connectedJoyconIdsList[i];
            if (!Joycons.ContainsKey(k))
            {
                Debug.Log("new joycon added");
                Joycons.Add(k, new Joycon());
                Joycons[k].Name = Redis.Get($"{k}:name");
                Joycons[k].Guid = Redis.Get($"{k}:guid");

                int defaultNum = 0;
                string numAxesStr = Redis.Get($"{k}:num_axes");
                int numAxes = ParseWithDefault(numAxesStr, defaultNum);
                Joycons[k].Axes = new float[numAxes];
                Joycons[k].AxisLatches = new ButtonLatch[numAxes];
                for (int j = 0; j < numAxes; j++)
                    Joycons[k].AxisLatches[j] = new ButtonLatch();
                Debug.Log("num axes: " + numAxes);

                string numButtonsStr = Redis.Get($"{k}:num_buttons");
                int numButtons = ParseWithDefault(numButtonsStr, defaultNum);
                Joycons[k].Buttons = new float[numButtons];
                Joycons[k].ButtonLatches = new ButtonLatch[numButtons];
                for (int j = 0; j < numButtons; j++)
                    Joycons[k].ButtonLatches[j] = new ButtonLatch();
                Debug.Log("num buttons: " + numButtons);

                string numHatsStr = Redis.Get($"{k}:num_hats");
                int numHats = ParseWithDefault(numHatsStr, defaultNum);
                Joycons[k].Hats = new float[numHats];
                Joycons[k].HatLatches = new ButtonLatch[numHats];
                for (int j = 0; j < numHats; j++)
                    Joycons[k].HatLatches[j] = new ButtonLatch();
                Debug.Log("num hats: " + numHats);
            }

            string tempStr;
            float temp;
            float tempDef = 0.0f;

            tempStr = Redis.Get($"{k}:power_level");
            temp = ParseWithDefault(tempStr, tempDef);
            Joycons[k].PowerLevel = temp;
            
            for (int j = 0; j < Joycons[k].Axes.Length; j++)
            {
                tempStr = Redis.Get($"{k}:axes:{j}");
                temp = ParseWithDefault(tempStr, tempDef);
                Joycons[k].Axes[j] = temp;
                Joycons[k].AxisLatches[j].Update(temp > 0.5f || temp < -0.5f);
            DebugVar = Joycons[k].Axes[0].ToString();
            }
            for (int j = 0; j < Joycons[k].Buttons.Length; j++)
            {
                tempStr = Redis.Get($"{k}:buttons:{j}");
                temp = ParseWithDefault(tempStr, tempDef);
                Joycons[k].Buttons[j] = temp;
                Joycons[k].ButtonLatches[j].Update(temp > 0.0f);
            }
            for (int j = 0; j < Joycons[k].Hats.Length; j++)
            {
                tempStr = Redis.Get($"{k}:hats:{j}");
                temp = ParseWithDefault(tempStr, tempDef);
                Joycons[k].Hats[j] = temp;
                // Joycons[k].HatLatches[j].Update(???);  // not used
            }
        }

        // cleanup disconnected joycons
        List<string> keysToRemove = new();
        foreach (var k in Joycons.Keys)
        {
            if (!Array.Exists(connectedJoyconIdsList, el => el == k))
            {
                keysToRemove.Add(k);
            }
        }
        foreach (var kr in keysToRemove)
        {
            Joycons.Remove(kr);
        }
    }
}
