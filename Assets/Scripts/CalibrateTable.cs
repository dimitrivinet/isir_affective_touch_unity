using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalibrateTable : MonoBehaviour
{
    public Transform staticAvatarWrist;
    public Transform movingAvatarWrist;
    public GameObject target;
    public Vector3 offset;

    public void calibrateTable()
    {
        Vector3 diff = staticAvatarWrist.position - movingAvatarWrist.position;
        target.transform.position += diff;
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
