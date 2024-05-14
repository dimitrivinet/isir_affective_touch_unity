using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Meta.WitAi;
using Unity.VisualScripting;
using UnityEngine;


public enum StrokeType
{
    None,
    Vibrator,
    Robot
}


public class RouteAnimTrigger
{
    public Animator animator;
    public float triggerTParam;
    public string triggerName;
    public bool triggered;

    public RouteAnimTrigger(Animator animator, float triggerTParam, string triggerName)
    {
        this.animator = animator;
        this.triggerTParam = triggerTParam;
        this.triggerName = triggerName;
        this.triggered = false;
    }

    public IEnumerator TriggerAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        animator.SetTrigger(triggerName);
    }
}


public class RouteFollow : MonoBehaviour
{
    public float test;
    public Transform[] routes;
    public Animator animator;
    public float ApproachSpeed = 1F;
    public float StrokeSpeed = 1F;
    public Transform StrokeRoute;
    public Transform Orientation;
    public Vector3 PosOffset;
    private Vector3 AdjustedPosOffset;
    public bool LookAtTraj = true;
    public bool TurnGreen;
    private int routeToGo;

    private float tParam;

    private Vector3 objectPosition;
    public double[] RouteLengths;
    public bool GoByTheRouteOnceRunning = false;
    public Material BrushMaterial;
    
    [Header("Loop movement for debug purposes")]
    public bool debugTraj = false;
    [Header("0: début, 0.5: centre, 1: fin")]

    [Range(0.0f, 1.0f)]
    public float TrajPosCoeff = 0.5f;


    private bool coroutineAllowed;
    private System.Random rng;

    // Start is called before the first frame update
    void Start()
    {
        rng = new();
        routeToGo = 0;
        tParam = 0f;
        coroutineAllowed = true;

        RouteLengths = new double[routes.Length];
        for (int i = 0; i < routes.Length; i++)
        {
            RouteLengths[i] = GetRouteLengthCm(routes[i]);
            Debug.Log(string.Format("Route {0} length: {1}", i, RouteLengths[i]));
        }

        transform.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (coroutineAllowed && debugTraj)
        {
            StartCoroutine(GoByTheRoute(routeToGo, null));
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

    public IEnumerator GoByTheRouteOnce(float speed_cms, float stroke_length_cm, StrokeType strokeType, float pStartAnim, float pEndAnim, float speedCmsTactile)
    {
        GoByTheRouteOnceRunning = true;
        bool oldDebugTraj = debugTraj;
        debugTraj = false;
        tParam = 0f;

        float oldApproachSpeed = ApproachSpeed;
        float oldStrokeSpeed = StrokeSpeed;
        float baseAnimationTime = 0.1f;

        // ApproachSpeed = speed_cms;
        ApproachSpeed = speedCmsTactile;
        StrokeSpeed = speed_cms;

        Vector3 p0 = routes[0].GetChild(0).position;
 
        // *
        // * random positioning for stroke lengths < 9cm
        // *
        // METHOD 1
        // float trajPosCoeff;
        // if (stroke_length_cm < 9f)
        // {
        //     // trajPosCoeff = TrajPosCoeff - rng.Next((int)(TrajPosCoeff * 2 * 100)) / (TrajPosCoeff * 100);
        //     trajPosCoeff = rng.Next((int)(TrajPosCoeff * 100)) / (TrajPosCoeff * 100);
        // }
        // else
        // {
        //     trajPosCoeff = TrajPosCoeff;
        // }
        // AdjustedPosOffset = PosOffset - (stroke_length_cm - 9f) * trajPosCoeff * transform.forward / 100;
        // METHOD 2
        float trajPosOffset = 0f;
        if (speed_cms != speedCmsTactile)
        {
            // curve fitted equation
            float freedom = 2.25f + 1.833f * stroke_length_cm - 0.074f * (float)Math.Pow((double)stroke_length_cm, 2);
            float maxTrajPosOffset = freedom - stroke_length_cm;
            maxTrajPosOffset = Math.Max(maxTrajPosOffset, 0.0f);
            trajPosOffset = rng.Next((int)(maxTrajPosOffset * 100)) / 100.0f;
        }
        Vector3 zeroPosOffset = PosOffset - (stroke_length_cm - 9f) * transform.forward / 100;
        AdjustedPosOffset = zeroPosOffset - trajPosOffset * transform.forward / 100;
        
        transform.position = p0 + AdjustedPosOffset;
        transform.gameObject.SetActive(true);

        // for (int i = 0; i < routes.Length; i++)
        // {
        //     yield return GoByTheRoute(i);
        // }

        // float animationSpeed = 1.0f / (3.5f * 5.0f);
        float animationSpeed = speedCmsTactile / (3.5f * 5.0f);
        // float animationSpeed = speedCmsTactile / 10.0f * 3.5f;
        Debug.Log("animationSpeed=" + animationSpeed);
        animator.SetFloat("Speed", animationSpeed);
        // float animationTime = baseAnimationTime / animationSpeed;
        float animationTime = baseAnimationTime / animationSpeed;

        yield return new WaitForSeconds(2);

        float approachTime = (float)RouteLengths[0] / ApproachSpeed;
        RouteAnimTrigger rat0 = new(animator, 0f, "Flex");
        float animStartDelay;
        if (strokeType == StrokeType.Vibrator)
        {
            animStartDelay = approachTime * pStartAnim;            
            // animStartDelay = approachTime - animationTime;            
            // animStartDelay = approachTime - animationTime * 3.0f;
        }
        else if (strokeType == StrokeType.Robot)
        {
            animStartDelay = approachTime * pStartAnim;            
            // animStartDelay = approachTime - animationTime;            
            // animStartDelay = approachTime - animationTime * 9.0f;
        }
        else
        {
            yield break;
        }
        Debug.Log("animationTime=" + animationTime);
        Debug.Log("approachTime=" + approachTime);
        Debug.Log("animStartDelay=" + animStartDelay);

        // Debug.Log(approachTime + " : " + animationTime + " : " + animStartDelay);
        StartCoroutine(rat0.TriggerAfterSeconds(animStartDelay));

        yield return GoByTheRoute(0, null);
        if (TurnGreen)
        {
            BrushMaterial.color = Color.green;
        }
        
        float forwardTime = stroke_length_cm / speed_cms;
        RouteAnimTrigger rat1 = new(animator, 0f, "Unflex");
        StartCoroutine(rat1.TriggerAfterSeconds(forwardTime * pEndAnim));
        // StartCoroutine(rat1.TriggerAfterSeconds(forwardTime - animationTime / 2.0f));
        yield return GoForward(speed_cms, stroke_length_cm);
        if (TurnGreen)
        {
            BrushMaterial.color = new Color32(0x01, 0x3A, 0x65, 0xFF);
        }
        Vector3 oldRoute2Position = routes[2].position;
        routes[2].position = transform.position - AdjustedPosOffset;
        // yield return GoByTheRoute(2,  new RouteAnimTrigger(animator, 0.3f, "Unflex"));
        yield return GoByTheRoute(2,  null);

        routes[2].position = oldRoute2Position;
        ApproachSpeed = oldApproachSpeed;
        StrokeSpeed = oldStrokeSpeed;

        routeToGo = 0;
        debugTraj = oldDebugTraj;

        transform.gameObject.SetActive(false);
        // transform.position = p0 + PosOffset;
        // yield return new WaitForSeconds(1);
        // transform.gameObject.SetActive(true);
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

    private IEnumerator GoByTheRoute(int routeNum, RouteAnimTrigger routeAnimTrigger)
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

        while (tParam < 1)
        {
            if (routeAnimTrigger != null)
            {
                if (tParam >= routeAnimTrigger.triggerTParam && !routeAnimTrigger.triggered)
                {
                    routeAnimTrigger.animator.SetTrigger(routeAnimTrigger.triggerName);
                    routeAnimTrigger.triggered = true;
                }
            }

            tParam += Time.deltaTime * speedModifier;

            objectPosition = Mathf.Pow(1 - tParam, 3) * p0 + 3 * Mathf.Pow(1 - tParam, 2) * tParam * p1 + 3 * (1 - tParam) * Mathf.Pow(tParam, 2) * p2 + Mathf.Pow(tParam, 3) * p3;
            objectPosition += AdjustedPosOffset;

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


