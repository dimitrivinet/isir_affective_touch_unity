using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RouteFollow : MonoBehaviour
{

    public Transform[] routes;
    public float ApproachSpeed = 1F;
    public float StrokeSpeed = 1F;
    public Transform StrokeRoute;
    public Transform Orientation;
    public Vector3 PosOffset;
    public bool LookAtTraj = true;
    private int routeToGo;

    private float tParam;

    private Vector3 objectPosition;
    public double[] RouteLengths;
    public bool GoByTheRouteOnceRunning = false;
    public Material BrushMaterial;
    
    [Header("Loop movement for debug purposes")]
    public bool debugTraj = false;


    private bool coroutineAllowed;

    // Start is called before the first frame update
    void Start()
    {
        routeToGo = 0;
        tParam = 0f;
        coroutineAllowed = true;

        RouteLengths = new double[routes.Length];
        for (int i = 0; i < routes.Length; i++)
        {
            RouteLengths[i] = GetRouteLengthCm(routes[i]);
            Debug.Log(string.Format("Route {0} length: {1}", i, RouteLengths[i]));
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (coroutineAllowed && debugTraj)
        {
            StartCoroutine(GoByTheRoute(routeToGo));
        }
    }

    double GetRouteLengthCm(Transform route, int steps = 100)
    {
        double ret = 0;

        Vector3 p1 = route.GetChild(1).position;
        Vector3 p0 = route.GetChild(0).position;
        Vector3 p2 = route.GetChild(2).position;
        Vector3 p3 = route.GetChild(3).position;

        Vector3 prev;
        Vector3 next;

        prev = p1;

        float tParam = 0;
        while (tParam < 1)
        {
            tParam += 1F / steps;
            next = Mathf.Pow(1 - tParam, 3) * p0 + 3 * Mathf.Pow(1 - tParam, 2) * tParam * p1 + 3 * (1 - tParam) * Mathf.Pow(tParam, 2) * p2 + Mathf.Pow(tParam, 3) * p3;

            ret += Vector3.Distance(prev, next);
            prev = next;
        }

        return ret * 100;
    }

    public IEnumerator GoByTheRouteOnce(string speed_cms, float stroke_length_cm)
    {
        GoByTheRouteOnceRunning = true;
        bool oldDebugTraj = debugTraj;
        debugTraj = false;
        tParam = 0f;

        float speed_cms_f = float.Parse(speed_cms);
        float oldApproachSpeed = ApproachSpeed;
        float oldStrokeSpeed = StrokeSpeed;

        ApproachSpeed = speed_cms_f;
        StrokeSpeed = speed_cms_f;

        Vector3 p0 = routes[0].GetChild(0).position;

        // for (int i = 0; i < routes.Length; i++)
        // {
        //     yield return GoByTheRoute(i);
        // }

        yield return GoByTheRoute(0);
        BrushMaterial.color = Color.green;
        yield return GoForward(speed_cms_f, stroke_length_cm);
        BrushMaterial.color = new Color32(0x01, 0x3A, 0x65, 0xFF);
        Vector3 oldRoute2Position = routes[2].position;
        routes[2].position = transform.position - PosOffset;
        yield return GoByTheRoute(2);

        routes[2].position = oldRoute2Position;
        ApproachSpeed = oldApproachSpeed;
        StrokeSpeed = oldStrokeSpeed;

        routeToGo = 0;
        debugTraj = oldDebugTraj;

        transform.gameObject.SetActive(false);
        transform.position = p0 + PosOffset;
        yield return new WaitForSeconds(1);
        transform.gameObject.SetActive(true);
        yield return new WaitForEndOfFrame();
        GoByTheRouteOnceRunning = false;
    }

    private IEnumerator GoForward(float strokeSpeedCms, float strokeLengthCm)
    {
        float lengthStroked = 0;
        Vector3 toStroke;
        while (lengthStroked < strokeLengthCm)
        {
            toStroke = strokeSpeedCms / 100f * Time.deltaTime * Vector3.forward;
            // toStroke = strokeSpeedCms / 100f * Time.deltaTime * transform.forward;
            transform.Translate(toStroke);
            lengthStroked += toStroke.magnitude * 100f;
            yield return new WaitForEndOfFrame();
        }
    }

    private IEnumerator GoByTheRoute(int routeNum)
    {
        coroutineAllowed = false;
        // analyser la vitesse du stimulus visuel et déterminer la speed scale pour chaque vitesse

        Vector3 p0 = routes[routeNum].GetChild(0).position;
        Vector3 p1 = routes[routeNum].GetChild(1).position;
        Vector3 p2 = routes[routeNum].GetChild(2).position;
        Vector3 p3 = routes[routeNum].GetChild(3).position;
        transform.rotation = Orientation.rotation;

        float speed = routes[routeNum] == StrokeRoute ? StrokeSpeed : ApproachSpeed;
        // float speedModifier = speed / 9F;
        float speedModifier = speed / (float)RouteLengths[routeNum];

        if (routes[routeNum] == StrokeRoute)
        {
            // oldcolor = 013A65
            BrushMaterial.color = Color.green;
        }
        else
        {
            BrushMaterial.color = new Color32(0x01, 0x3A, 0x65, 0xFF);
        }

        while (tParam < 1)
        {
            tParam += Time.deltaTime * speedModifier;

            objectPosition = Mathf.Pow(1 - tParam, 3) * p0 + 3 * Mathf.Pow(1 - tParam, 2) * tParam * p1 + 3 * (1 - tParam) * Mathf.Pow(tParam, 2) * p2 + Mathf.Pow(tParam, 3) * p3;
            objectPosition += PosOffset;

            if (LookAtTraj)
            {
                transform.LookAt(objectPosition);
                // transform.rotation *= RotOffset;
            }

            transform.position = objectPosition;
            yield return new WaitForEndOfFrame();
        }

        tParam = 0;
        routeToGo += 1;

        if (routeToGo > routes.Length - 1)
        {
            routeToGo = 0;
        }

        coroutineAllowed = true;
    }
}


