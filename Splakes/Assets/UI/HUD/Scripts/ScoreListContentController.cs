using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using TMPro;

public class ScoreListContentController : MonoBehaviour
{
    //List item prefab
    public GameObject ScoreListItem;

    //Keep track of players and scores
    private Dictionary<Player, GameObject> playerScoreListItems;

    void Start()
    {
        playerScoreListItems = new Dictionary<Player, GameObject>();
    }

    public void AddNewPlayer(Player player)
    {
        GameObject tempScoreListItem = Instantiate(ScoreListItem, transform);
        tempScoreListItem.transform.Find("PlayerName").GetComponent<TextMeshProUGUI>().text = player.NickName;
        tempScoreListItem.transform.Find("PlayerScore").GetComponent<TextMeshProUGUI>().text = "0";

        playerScoreListItems.Add(player, tempScoreListItem);
    }

    public void AddExistingPlayer(Player player, int score)
    {
        GameObject tempScoreListItem = Instantiate(ScoreListItem, transform);
        tempScoreListItem.transform.Find("PlayerName").GetComponent<TextMeshProUGUI>().text = player.NickName;
        tempScoreListItem.transform.Find("PlayerScore").GetComponent<TextMeshProUGUI>().text = score.ToString();

        playerScoreListItems.Add(player, tempScoreListItem);
    }

    public void UpdatePlayer(Player player, int score)
    {
        //If player exists already, update it, otherwise add the new player
        if (playerScoreListItems.ContainsKey(player))
        {
            playerScoreListItems[player].transform.Find("PlayerScore").GetComponent<TextMeshProUGUI>().text = score.ToString();
        }
        else
        {
            AddExistingPlayer(player, score);
        }
    }

    public void RemovePlayer(Player player)
    {
        Destroy(playerScoreListItems[player]);
        playerScoreListItems.Remove(player);
    }
    
}
