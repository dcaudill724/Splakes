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
        if (!playerScoreListItems.ContainsKey(player))
        {
            AddExistingPlayer(player, score);
            return;
        }

        playerScoreListItems[player].transform.Find("PlayerScore").GetComponent<TextMeshProUGUI>().text = score.ToString();
    }

    public void RemovePlayer(Player player)
    {
        Destroy(playerScoreListItems[player]);
        playerScoreListItems.Remove(player);
    }
    
}
