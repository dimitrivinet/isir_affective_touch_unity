using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class FreezeHeadset : MonoBehaviour
{
    public GameObject Headset;
 
    void Start()
    {
        transform.position = Headset.transform.position;
        Headset.transform.localPosition = Vector3.zero;
    }
 
    void LateUpdate()
    {
        transform.position -= Headset.transform.localPosition;
    }
}