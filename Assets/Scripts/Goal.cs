using UnityEngine;
using System.Collections;

public class Goal : MonoBehaviour
{
    public AudioClip goalClip;

    void OnCollisionEnter2D(Collision2D coll)
    {
        if (coll.gameObject.tag == "Player")
        {
            var audioSource = GetComponent<AudioSource>();
            if (audioSource != null && goalClip != null)
            {
                audioSource.PlayOneShot(goalClip);
            }
            GameManager.instance.RestartLevel(0.5f);

            //finds the timer script component instance in the level scene
            var timer = FindObjectOfType<Timer>();
            GameManager.instance.SaveTime(timer.time);
        }
    }
}
