using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputSystemManager : MonoBehaviour
{
    public TextMeshProUGUI PleasantnessText;
    public Slider PleasantnessSlider;
    public TextMeshProUGUI IntensityText;
    public Slider IntensitySlider;
    public TextMeshProUGUI SubmitText;
    public RedisManager RedisManager;

    [SerializeField]
    private int CurrentSelected;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        string CurrentSelectedString = RedisManager.Get(RedisChannels.input_curr_selected);
        if (CurrentSelectedString == null)
        {
            return;
        }

        CurrentSelected = int.Parse(CurrentSelectedString);

        PleasantnessText.color = Color.black;
        IntensityText.color = Color.black;
        SubmitText.color = Color.black;

        switch (CurrentSelected)
        {
            case 0:
                PleasantnessText.color = Color.green;
                break;
            case 1:
                IntensityText.color = Color.green;
                break;
            case 2:
                SubmitText.color = Color.green;
                break;
            default:
                break;
        }

        try 
        {
            string PleasantnessString = RedisManager.Get(RedisChannels.pleasantness);
            PleasantnessSlider.value = float.Parse(PleasantnessString);
        }
        catch{}

        try 
        {
        string IntensityString = RedisManager.Get(RedisChannels.intensity);
            IntensitySlider.value = float.Parse(IntensityString);
        }
        catch {}
    }
}
