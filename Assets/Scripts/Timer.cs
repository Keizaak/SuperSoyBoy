using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    private Text timerText;

    public decimal time;

    void Awake()
    {
        timerText = GetComponent<Text>();
    }

    void Update()
    {
        time = System.Math.Round((decimal)Time.timeSinceLevelLoad, 2);
        timerText.text = time.ToString();
    }
}