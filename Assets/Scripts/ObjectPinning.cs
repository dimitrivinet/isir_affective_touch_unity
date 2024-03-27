using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPinning : MonoBehaviour
{
    public GameObject Target;
    public GameObject Anchor;
    public Vector3 offsetValue;
    public bool freeze = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!freeze)
            Target.transform.position = Anchor.transform.position + offsetValue;
    }
}
