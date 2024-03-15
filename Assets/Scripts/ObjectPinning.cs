using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPinning : MonoBehaviour
{
    public GameObject firstChild;
    public GameObject secondChild;
    public Vector3 offsetValue;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        firstChild.transform.position = secondChild.transform.position + offsetValue;
    }
}
