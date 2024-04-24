using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CongruencyToggles : MonoBehaviour
{
    [SerializeField]
    private Toggle Congruent;
    [SerializeField]
    private Toggle Incongruent;

    // Update is called once per frame
    void Update()
    {
        if (Congruent.isOn)
            Incongruent.isOn = false;
        if (Incongruent.isOn)
            Congruent.isOn = false;
    }
}
