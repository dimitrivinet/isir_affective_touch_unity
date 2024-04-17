using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class Vibrators : MonoBehaviour
{
    public string UdpHost;
    public int UdpPort;
    public int[] VibIds;
    private UdpClient Udp;

    public void ConnectSTM()
    {
        byte[] sendBytes = Encoding.UTF8.GetBytes("cmdConnect");
        Udp.Send(sendBytes, sendBytes.Length);
    }

    public void StopVibrating()
    {
        byte[] sendBytes = Encoding.UTF8.GetBytes("cmdStopWav");
        Udp.Send(sendBytes, sendBytes.Length);
    }

    public void TrigWav()
    {
        byte[] sendBytes = Encoding.UTF8.GetBytes("cmdTrigWav");
        Udp.Send(sendBytes, sendBytes.Length);
    }

    public void LoadWav()
    {
        byte[] sendBytes = Encoding.UTF8.GetBytes("wavLoad");
        Udp.Send(sendBytes, sendBytes.Length);
    }

    public void SetActive(bool active)
    {
        string messageActive = active ? "True" : "False";

        foreach (var vibId in VibIds)
        {
            byte[] sendBytes = Encoding.UTF8.GetBytes($"wavSetActive;{vibId};{messageActive}");
            Udp.Send(sendBytes, sendBytes.Length);
        }
    }

    public void SetVibrate(string speed, double volume)
    {
        foreach (var vibId in VibIds)
        {
            string wavName = $"signal120_{vibId}_{speed}";

            byte[] setName = Encoding.UTF8.GetBytes($"wavChangedName;{vibId};{wavName}");
            Udp.Send(setName, setName.Length);

            byte[] setVolume = Encoding.UTF8.GetBytes($"wavChangedVolume;{vibId};{volume}");
            Udp.Send(setVolume, setVolume.Length);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Udp = new UdpClient();
        try
        {
            Udp.Connect(UdpHost, UdpPort);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
