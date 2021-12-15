using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using TMPro;
using Photon.Realtime;
using Photon.Pun;
using ExitGames.Client.Photon;

public enum SnakeEvents : byte
{
    NeedSync = 1,
    SnakeDied = 2,
    SnakeHurt = 3,
    SnakeRespawn = 4,
    SnakeSegmentDied = 5,
}

public class SpawnSnakes : MonoBehaviourPunCallbacks, IOnEventCallback
{
    //Spawning parameters
    public Vector3 MinSpawnBounds;
    public Vector3 MaxSpawnBounds;

    //Hud control parameters
    public TextMeshProUGUI SnakeScoreText;
    public TextMeshProUGUI SnakeLengthText;

    //Prefabs
    public GameObject SnakePrefab;
    public GameObject HollowSnakePrefab;

    //SnakeController control data
    private GameObject snakeObject;
    private SnakeController snake;
    private List<SnakeController> otherSnakes;

    void Start()
    {
        SpawnSnake(false);
    }

    void Update()
    {
        
    }


    //Spawns the snake controlled by the current player
    public void SpawnSnake(bool alreadySynced)
    {
        if (!alreadySynced)
        {
            //Initialize list of other players snakes
            otherSnakes = new List<SnakeController>();
        }

        //Generate Random Spawn point
        float tempX = UnityEngine.Random.Range(MinSpawnBounds.x, MaxSpawnBounds.x);
        float tempY = UnityEngine.Random.Range(MinSpawnBounds.y, MaxSpawnBounds.y);
        float tempZ = UnityEngine.Random.Range(MinSpawnBounds.z, MaxSpawnBounds.z);
        Vector3 tempSpawn = new Vector3(tempX, tempY, tempZ);

        //Instantiate a snake at the random spawn point
        GameObject snake = PhotonNetwork.Instantiate("Snake", tempSpawn, Quaternion.identity); //Gets instantiated on the network
        snake.name = "Snake: " + PhotonNetwork.LocalPlayer.NickName;


        //Store the local player snake object
        snakeObject = snake;

        //Initialize local players snake data
        SnakeController sc = snake.GetComponent<SnakeController>();
        sc.Owner = PhotonNetwork.LocalPlayer;
        sc.SnakeScoreText = SnakeScoreText; //Hud Control
        sc.SnakeLengthText = SnakeLengthText; //Hud Control
        sc.SnakeSpawner = this; //Needs this to respawn snake on death

        //Store the local player SnakeController        
        this.snake = sc;
    }

    public void RespawnSnake()
    {
        PhotonNetwork.Destroy(snakeObject);
        SpawnSnake(true);
    }


    //Sync all snakes instantiated by other players with their snakes
    void syncClientWithOtherOwners()
    {
        

        GameObject[] allSnakes = GameObject.FindGameObjectsWithTag("Snake");
        for (int i = 0; i < allSnakes.Length; ++i)
        {
            SnakeController sc = allSnakes[i].GetComponent<SnakeController>();

            if (sc != snake)
            {
                otherSnakes.Add(sc);
                sc.RequestSyncWithOwner();
            }
        }
    }

    public int GetScore()
    {
        return snake.Score;
    }


    //Event handler
    public void OnEvent(EventData photonEvent)
    {
        SnakeEvents snakeEvent;
        Enum.TryParse(Enum.GetName(typeof(SnakeEvents), photonEvent.Code), out snakeEvent);

        switch ((SnakeEvents)photonEvent.Code)
        {
            case SnakeEvents.SnakeDied: //Informs the client that the event sender has died
                for (int i = 0; i < otherSnakes.Count; ++i)
                {
                    if (otherSnakes[i].Owner == PhotonNetwork.CurrentRoom.GetPlayer(photonEvent.Sender) && !otherSnakes[i].Dying)
                    {
                        otherSnakes[i].Die(false);
                    }
                } 
                break;

            case SnakeEvents.SnakeHurt: //Informs the client that the event sender has been hurt and what body segment was hurt.
                Player sender = PhotonNetwork.CurrentRoom.GetPlayer(photonEvent.Sender);
   
                for (int i = 0; i < otherSnakes.Count; ++i)
                {
                    if (otherSnakes[i].Owner == sender)
                    {
                        otherSnakes[i].Hurt((int)photonEvent.CustomData);
                    }
                }
                
                break;
        }
    }


    //Public event raisers for other classes
    public void SnakeDied()
    {
        raiseSnakeEvent(SnakeEvents.SnakeDied);
    }

    public void SnakeHurt(int hurtStartIndex)
    {
        raiseSnakeEvent(SnakeEvents.SnakeHurt, hurtStartIndex);
    }


    //Add and remove snakes
    public void AddSnake(SnakeController sc)
    {
        int listIndex = -1;

        for (int i = 0; i < otherSnakes.Count; ++i)
        {
            if (otherSnakes[i].Owner == sc.Owner)
            {
                listIndex = i;
            }
        }

        //If the owner already had a snake then we replace it, otherwise we add it
        if (listIndex >= 0)
        {
            otherSnakes[listIndex] = sc;
        }
        else
        {
            otherSnakes.Add(sc);
        }
        
    }

    private void removeSnakeFromOthers(Player player)
    {
        SnakeController scToRemove = null;

        for (int i = 0; i < otherSnakes.Count; ++i)
        {
            if (otherSnakes[i].Owner == player)
            {
                scToRemove = otherSnakes[i];
            }
        }

        otherSnakes.Remove(scToRemove);
    }


    //Snake event raising functions
    private void raiseSnakeEvent(SnakeEvents snakeEvent)
    {
        RaiseEventOptions reo = new RaiseEventOptions();

        PhotonNetwork.RaiseEvent((byte)snakeEvent, null, reo, SendOptions.SendReliable);
    }

    private void raiseSnakeEvent(SnakeEvents snakeEvent, ReceiverGroup rg)
    {
        RaiseEventOptions reo = new RaiseEventOptions();
        reo.Receivers = rg;

        PhotonNetwork.RaiseEvent((byte)snakeEvent, null, reo, SendOptions.SendReliable);
    }

    private void raiseSnakeEvent(SnakeEvents snakeEvent, object eventContent)
    {
        RaiseEventOptions reo = new RaiseEventOptions();

        PhotonNetwork.RaiseEvent((byte)snakeEvent, eventContent, reo, SendOptions.SendReliable);
    }


    //Room callbacks
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);

        removeSnakeFromOthers(otherPlayer); //Remove the players snake from otherSnakes
    }
}
