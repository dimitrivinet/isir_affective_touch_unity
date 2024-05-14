using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPinning : MonoBehaviour
{
    public GameObject Target;
    public GameObject Anchor;
    public Vector3 offsetValue;
    public bool lateUpdate = false;
    public bool freeze = false;
 
    // Update is called once per frame
    void Update()
    {
        if (!freeze && !lateUpdate)
            Target.transform.position = Anchor.transform.position + offsetValue;
    }

    
    void LateUpdate()
    {
        if (!freeze && lateUpdate)
            Target.transform.position = Anchor.transform.position + offsetValue;
    }
}
