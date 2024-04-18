using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CalibrateHead : MonoBehaviour
{
    public GameObject anchor;
    public GameObject target;
    public Vector3 offset;

    public void calibrateHead()
    {
        // position
        Vector3 anchorPos = anchor.transform.localPosition;

        target.transform.position = new Vector3(-anchorPos.x, 0.0f, -anchorPos.z);

        // rotation
        target.transform.rotation = Quaternion.Euler(0.0f, -anchor.transform.localEulerAngles.y, 0.0f);
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
