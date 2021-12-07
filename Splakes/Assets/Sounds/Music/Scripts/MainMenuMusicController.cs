using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuMusicController : MonoBehaviour
{
    public AudioClip Music;


    // Start is called before the first frame update
    void Start()
    {
        var temp = GetComponent<AudioSource>();
        temp.clip = Music;
        temp.Play();
    }

    // Update is called once per frame
    void Update()
    {

    }
}

