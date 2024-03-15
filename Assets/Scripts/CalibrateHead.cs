using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalibrateHead : MonoBehaviour
{
    public GameObject[] environment;
    public GameObject head;
    public Vector3 offset;

    public void calibrateHead()
    {
        foreach (var env in environment)
        {
            env.transform.position = head.transform.position + offset;
            env.transform.rotation = head.transform.rotation;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.H))
        {
            calibrateHead();        
        }
    }
}
