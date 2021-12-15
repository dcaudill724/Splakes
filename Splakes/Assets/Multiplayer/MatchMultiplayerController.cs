using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class MatchMultiplayerController : MonoBehaviourPunCallbacks
{
    //Debug logging
    public DebugLog DebugLog;

    //Hud
    public ScoreListContentController ScoreListContent;

    //Level control objects
    public GenerateFood FoodGenerator;
    public SpawnSnakes SnakeSpawner;

    //Multiplayer request timing
    private float updateTime = 0.5f;
    private float timeSinceLastUpdate;

    void Start()
    {
        
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; ++i)
        {
            ScoreListContent.AddNewPlayer(PhotonNetwork.PlayerList[i]); //Add players in the game to the score list
        }
        
    }

    void Update()
    {
        timeSinceLastUpdate += Time.deltaTime;

        if (timeSinceLastUpdate >= updateTime)
        {

            //Put timed update requests in this region
            //################################
            RequestScores();
            //################################

            timeSinceLastUpdate -= updateTime;
        }
    }

   
    
    
    //Request the other players scores
    public void RequestScores()
    {
        photonView.RPC("GetScoreResponse", RpcTarget.All, SnakeSpawner.GetScore());
    }

    //Adds a new player to the score list
    [PunRPC]
    public void AddPlayerToScoreList(int score, PhotonMessageInfo info)
    {
        ScoreListContent.AddExistingPlayer(info.Sender, score);
    }

    //Update the senders score in the score list
    [PunRPC]
    public void GetScoreResponse(int score, PhotonMessageInfo info)
    {
        ScoreListContent.UpdatePlayer(info.Sender, score);
    }

    //Leave the game and return to the main menu
    [PunRPC]
    public void LeaveGame()
    {
        checkMasterClose();
        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene("MainMenu");
    }

    private void OnApplicationQuit()
    {
        checkMasterClose();
        PhotonNetwork.Disconnect();
    }

    //Only runs if the master client leaves the game
    void checkMasterClose()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsVisible = false; //If master leaves the room is no longer
            photonView.RPC("LeaveGame", RpcTarget.Others); //Make all other players leave the game
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);

        ScoreListContent.RemovePlayer(otherPlayer); //Remove the player from the score list
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);

        DebugLog.ShowMessage("Loading Stage data");
        

        ScoreListContent.AddNewPlayer(newPlayer); //Add new player to the score list
    }
    
}
