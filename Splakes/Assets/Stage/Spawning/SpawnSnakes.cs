using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using TMPro;
using Photon.Realtime;
using Photon.Pun;
using ExitGames.Client.Photon;



public class SpawnSnakes : MonoBehaviourPunCallbacks
{
    //Spawning parameters
    public Vector3 MinSpawnBounds;
    public Vector3 MaxSpawnBounds;

    //Prefabs
    public GameObject SnakePrefab;
    public GameObject HollowSnakePrefab;
    
    //Spawns the snake controlled by the current player
    public SnakeController SpawnSnake()
    {
        //Generate Random Spawn point
        float tempX = UnityEngine.Random.Range(MinSpawnBounds.x, MaxSpawnBounds.x);
        float tempY = UnityEngine.Random.Range(MinSpawnBounds.y, MaxSpawnBounds.y);
        float tempZ = UnityEngine.Random.Range(MinSpawnBounds.z, MaxSpawnBounds.z);
        Vector3 tempSpawn = new Vector3(tempX, tempY, tempZ);

        //Instantiate a snake at the random spawn point
        GameObject snake = PhotonNetwork.Instantiate("Snake", tempSpawn, Quaternion.identity); //Gets instantiated on the network
        snake.name = "Snake: " + PhotonNetwork.LocalPlayer.NickName;

        //Initialize local players snake data
        SnakeController sc = snake.GetComponent<SnakeController>();
        sc.Owner = PhotonNetwork.LocalPlayer;

        //Store the local player SnakeController        
        return sc;
    }
}