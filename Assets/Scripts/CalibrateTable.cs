using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalibrateTable : MonoBehaviour
{
    public Transform staticAvatarWrist;
    public OVRSkeleton skeleton;
    public GameObject target;
    public Vector3 offset;

    public void calibrateTable()
    {
        if (skeleton.Bones.Count == 0)
        {
            return;
        }

        Vector3 diff = staticAvatarWrist.position - skeleton.Bones[0].Transform.position;
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
