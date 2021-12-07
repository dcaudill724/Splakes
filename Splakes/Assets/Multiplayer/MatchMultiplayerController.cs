using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;
using TMPro;
using UnityEditor;

public class MatchMultiplayerController : MonoBehaviourPun
{
    public DebugLog DebugLog;
    public GenerateFood FoodGenerator;
    public SpawnSnakes SnakeSpawner;
    public int UpdatesPerSecond = 60;

    private float timeSinceLastUpdate = 0;

    // Start is called before the first frame update
    void Start()
    {
        PhotonNetwork.SendRate = UpdatesPerSecond;

        if (PhotonNetwork.IsMasterClient)
        {
            generateStage();
        }
        else
        {
            getStageData();
        }

        SpawnSnake(PhotonNetwork.LocalPlayer);
    }


    private void Update()
    {
        timeSinceLastUpdate += Time.deltaTime;

        if (timeSinceLastUpdate > 1 / UpdatesPerSecond)
        {
            requestAllSnakeData();
            timeSinceLastUpdate -= 1 / UpdatesPerSecond;
        }
    }

    #region Master Functions

    #region Stage Functions
    void generateStage()
    {
        DebugLog.ShowMessage("Generating Stage");
        FoodGenerator.SpawnAllFood();
    }

    [PunRPC]
    public void RequestStageData(PhotonMessageInfo info)
    {
        DebugLog.ShowMessage("Stage data request received from: " + info.Sender);
        respondWithStageData(info.Sender);

    }

    private void respondWithStageData(Player requestSender)
    {
        DebugLog.ShowMessage("Responding with stage data");
        int[] foodIDs = FoodGenerator.GetFoodIDs();

        photonView.RPC("GetStageDataResponse", requestSender, foodIDs, FoodGenerator.GetFoodPositions(foodIDs), FoodGenerator.GetFoodPointValues(foodIDs));
    }

    #endregion
 
    #endregion

    #region Guest Functions

    #region Stage Functions
    private void getStageData()
    {
        DebugLog.ShowMessage("Requesting stage data");
        photonView.RPC("RequestStageData", RpcTarget.MasterClient);
    }

    [PunRPC]
    public void GetStageDataResponse(int[] foodIDs, Vector3[] foodPositions, int[] foodPointValues)
    {
        DebugLog.ShowMessage("Stage data received");
        loadStageData(foodIDs, foodPositions, foodPointValues);
    }

    private void loadStageData(int[] foodIDs, Vector3[] foodPositions, int[] foodPointValues)
    {
        DebugLog.ShowMessage("Loading Stage data");
        FoodGenerator.LoadFood(foodIDs, foodPositions, foodPointValues);
        DebugLog.ShowMessage("Stage data loaded");
    }
    #endregion

    #endregion

    #region Everyone Functions

    private void SpawnSnake(Player player)
    {
        SnakeSpawner.SpawnSnake(player);
    }

    private void requestAllSnakeData()
    {
        photonView.RPC("RequestSnakeData", RpcTarget.Others);
    }

    [PunRPC]
    public void RequestSnakeData(PhotonMessageInfo info)
    {
        respondWithSnakeData(info.Sender);
    }

    private void respondWithSnakeData(Player requestSender)
    {
        photonView.RPC("GetSnakeDataResponse", requestSender, SnakeSpawner.GetSnakeBodyPositionData(), SnakeSpawner.GetLaserWallMeshVerticies());
    }

    [PunRPC]
    public void GetSnakeDataResponse(object[] snakeBodyPoisitions, object[] laserWallMeshVerticies, PhotonMessageInfo info)
    {
        SnakeSpawner.UpdateSnakeByPlayer(info.Sender, snakeBodyPoisitions, laserWallMeshVerticies);
    }
    #endregion
}
