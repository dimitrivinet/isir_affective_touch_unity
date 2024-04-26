using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class CalibrateAll : MonoBehaviour
{
    CalibrateHead head;
    CalibrateTable table;

    public void Calibrate()
    {
        IEnumerator CalibrateCoroutine() 
        {
            head.calibrateHead();
            yield return new WaitForEndOfFrame();
            table.calibrateTable();
        }
        
        StartCoroutine(CalibrateCoroutine());
    }

    // Start is called before the first frame update
    void Awake()
    {
        head = GetComponent<CalibrateHead>();
        table = GetComponent<CalibrateTable>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            Calibrate();
        }
    }
    }
