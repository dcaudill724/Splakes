using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Realtime;
using Photon.Pun;
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon;

public class HudController : MonoBehaviour
{
    public ScoreListContentController ScoreListContent;
    public TextMeshProUGUI ScoreText;
    public TextMeshProUGUI LengthText;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdatePlayer(Player player, int score, bool isLocalPlayer = false, int length = 0)
    {
        ScoreListContent.UpdatePlayer(player, score);

        if (isLocalPlayer)
        {
            ScoreText.text = "Score: " + score;
            LengthText.text = "Length: " + length;
        }
    }

    public void RemovePlayer(Player player)
    {
        ScoreListContent.RemovePlayer(player); //Remove the player from the score list
    }
}
