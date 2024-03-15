using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalibrateTable : MonoBehaviour
{
    public GameObject[] ToCalibrate;
    public GameObject handRight;
    public Vector3 offset;

    public void calibrateTable()
    {
        Vector3 disp = handRight.transform.position + offset;

        foreach (var toCal in ToCalibrate)
        {
            Vector3 newPos = toCal.transform.position;
            newPos.z = disp.z;
            newPos.y = disp.y;
            toCal.transform.position = newPos;
        }
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            calibrateTable();
        }
    }
}
