using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InGameMusicController : MonoBehaviour
{
    public AudioClip[] Music;

    private float timeLastClipStarted;
    private float lengthOfSong;
    private int lastSongIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        //startNewSong();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void startNewSong()
    {
        int randIndex = Random.Range(0, Music.Length);
        while (randIndex == lastSongIndex)
        {
            randIndex = Random.Range(0, Music.Length);
        }

        var temp = GetComponent<AudioSource>();
        temp.clip = Music[randIndex];
        temp.Play();
        timeLastClipStarted = Time.realtimeSinceStartup;
        lastSongIndex = randIndex;
    }
}
