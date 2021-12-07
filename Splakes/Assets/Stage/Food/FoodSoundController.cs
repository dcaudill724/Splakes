using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodSoundController : MonoBehaviour
{
    public AudioClip FoodEatenSoundEffect;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlayFoodEatenSoundEffect()
    {
        GetComponent<AudioSource>().clip = FoodEatenSoundEffect;
        GetComponent<AudioSource>().Play();
    }
}
