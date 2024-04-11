using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ObjectPinning2P : MonoBehaviour
{
    public GameObject Target;
    public GameObject Anchor1;
    public GameObject Anchor2;
    public Vector3 posOffset;
    public Quaternion rotOffset = Quaternion.identity;
    public bool freeze = false;

    // Update is called once per frame
    void Update()
    {
        if (!freeze)
        {
            Vector3 anchorDiff = Anchor2.transform.position - Anchor1.transform.position;
            CartesianToSpherical(anchorDiff, out float r, out float theta, out float phi);
            Target.transform.rotation = Quaternion.Euler(0f, -RadToDeg(theta), RadToDeg(phi)) * rotOffset;

            Vector3 rotAwarePosOffset = new(posOffset.x * (float)Math.Sin(theta) + posOffset.z * (float)Math.Cos(theta), posOffset.y, posOffset.x * (float)Math.Cos(theta) + posOffset.z * (float)Math.Sin(theta));
            Target.transform.position = ((Anchor1.transform.position + Anchor2.transform.position) / 2) + rotAwarePosOffset;
        }
    }

    public static void CartesianToSpherical(Vector3 cartCoords, out float outR, out float outTheta, out float outPhi){
        if (cartCoords.x == 0)
            cartCoords.x = Mathf.Epsilon;

        outR = Mathf.Sqrt((cartCoords.x * cartCoords.x)
                        + (cartCoords.y * cartCoords.y)
                        + (cartCoords.z * cartCoords.z));

        outTheta = Mathf.Atan(cartCoords.z / cartCoords.x);
        if (cartCoords.x < 0)
            outTheta += Mathf.PI;

        outPhi = Mathf.Asin(cartCoords.y / outR);
    }

    public static float RadToDeg(float rads)
    {
        return rads * 180f / (float)Math.PI;
    }
}
