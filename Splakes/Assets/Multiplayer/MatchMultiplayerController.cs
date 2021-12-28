using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon;
using System;

public class MatchMultiplayerController : MonoBehaviourPunCallbacks, IOnEventCallback, EventReceiver
{
    //Debug logging
    public DebugLog DebugLog;

    //Hud
    public HudController HUD;
    
    //Snake sync control objects
    private SnakeController snake;
    private Dictionary<Player, SnakeController> otherSnakes;

    //Spawning objects
    public GenerateFood FoodGenerator;
    public SpawnSnakes SnakeSpawner;

    //Multiplayer request timing
    private float updateTime = 0.5f;
    private float timeSinceLastUpdate;

    void Start()
    {
        EasyEventSystem.AddReceiver(this);

        initSnakeSync();
    }

    void Update()
    {
        timeSinceLastUpdate += Time.deltaTime;

        if (timeSinceLastUpdate >= updateTime)
        {

            //Put timed update requests in this region
            //################################

            updateScore();

            //################################

            timeSinceLastUpdate -= updateTime;
        }

        //Check collision for all other snakes with our snake
        foreach (KeyValuePair<Player, SnakeController> entry in otherSnakes)
        {
            snake.CheckBBCollision(entry.Value);
        }
    }

    #region Snakes
    //Initializes snake sync control objects
    private void initSnakeSync()
    {
        snake = SnakeSpawner.SpawnSnake(); //Spawns the snake controlled by the local player
        otherSnakes = new Dictionary<Player, SnakeController>(); //Initializes the list of snakes controlled by other players
        syncOtherSnakesWithOwners(); //Sync snakes owned by other players with data from the owners
    }

    //Sync all snakes instantiated by other players with their local data
    private void syncOtherSnakesWithOwners()
    {
        GameObject[] allSnakes = GameObject.FindGameObjectsWithTag("Snake");

        for (int i = 0; i < allSnakes.Length; ++i)
        {
            SnakeController sc = allSnakes[i].GetComponent<SnakeController>();

            if (sc != snake)
            {
                otherSnakes.Add(sc.Owner, sc);
                sc.RequestSyncWithOwner();
            }
        }
    }



    //Spawns a new snake for local player if they die
    private void respawnSnake()
    {
        PhotonNetwork.Destroy(snake.gameObject);
        snake = SnakeSpawner.SpawnSnake();
    }



    //Add and remove snakes
    private void addSnake(SnakeController sc)
    {
        //Update the snake controller if the owner already has an element in the list, otherwise add the snake controller with the new owner
        if (otherSnakes.ContainsKey(sc.Owner))
        {
            otherSnakes[sc.Owner] = sc;
        }
        else
        {
            otherSnakes.Add(sc.Owner, sc);
        }
    }

    private void removeSnakeFromOthers(Player player)
    {
        otherSnakes.Remove(player);
    }
    #endregion

    #region HUD
    //Request the other players scores
    private void updateScore()
    {
        photonView.RPC("getScoreResponse", RpcTarget.All, snake.Score);
    }

    //Update the senders score in the score list
    [PunRPC]
    private void getScoreResponse(int score, PhotonMessageInfo info)
    {
        if (info.Sender == PhotonNetwork.LocalPlayer)
        {
            HUD.UpdatePlayer(info.Sender, score, true, snake.CurrentLength);
        }
        else
        {
            HUD.UpdatePlayer(info.Sender, score);
        }
    }

    private void removePlayerFromScoreList(Player player)
    {
        HUD.RemovePlayer(player);
    }
    #endregion

    #region Disconnection
    //Leave the game and return to the main menu
    [PunRPC]
    public void LeaveGame(bool disconnect = false)
    {
        //When the Master leaves, the room closes and all other players are kicked from the game
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsVisible = false;
            photonView.RPC("LeaveGame", RpcTarget.Others, false); 
        }

        if (disconnect)
        {
            PhotonNetwork.Disconnect();
        }
        else
        {
            PhotonNetwork.LeaveRoom();
        }
        
        SceneManager.LoadScene("MainMenu");
    }
    #endregion

    #region Built-in event methods
    private void OnApplicationQuit()
    {
        Debug.Log("Quit");
        LeaveGame(true);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);

        removePlayerFromScoreList(otherPlayer);
        removeSnakeFromOthers(otherPlayer); //Remove the players snake from otherSnakes
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
    }
    #endregion

    #region Event handlers

    //Network event handler
    public void OnEvent(EventData photonEvent)
    {
        SnakeEvents snakeEvent;
        Enum.TryParse(Enum.GetName(typeof(SnakeEvents), photonEvent.Code), out snakeEvent);

        Player sender = PhotonNetwork.CurrentRoom.GetPlayer(photonEvent.Sender);

        switch ((SnakeEvents)photonEvent.Code)
        {
            case SnakeEvents.SnakeStartedDying: //Informs the client that the event sender has died
                otherSnakes[sender].StartDying();
                break;

            case SnakeEvents.SnakeHurt: //Informs the client that the event sender has been hurt and what body segment was hurt.
                otherSnakes[sender].Hurt((int)photonEvent.CustomData);
                break;
        }
    }

    //Local Event handler
    public void ReceiveEvent(string eventName, object content)
    {
        switch (eventName)
        {
            case "unowned snake instantiated":
                addSnake((SnakeController)content);
                break;

            case "snake finished dying":
                respawnSnake();
                break;
        }
    }

    #endregion
}