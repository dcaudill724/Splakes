using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.SceneManagement;

public class MultiplayerController : MonoBehaviourPunCallbacks
{
    public bool IsReady = false;
    public TextMeshProUGUI DebugLog;

    [HideInInspector]
    public List<RoomInfo> RoomList;
    public string RoomToJoin;
    

    string gameVersion = "1";

    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    void Start()
    {
        LogMessage("Connecting");
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        PhotonNetwork.JoinLobby();
        LogMessage("Connected");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        RoomList = roomList;
        IsReady = true;
    }





    public void CreateRoom(TextMeshProUGUI roomNameInput)
    {
        RoomOptions ro = new RoomOptions();
        ro.MaxPlayers = 16;
        PhotonNetwork.CreateRoom(roomNameInput.text, ro, TypedLobby.Default);
    }

    public override void OnCreatedRoom()
    {
        base.OnCreatedRoom();
        LogMessage("Created room succesfully");
        SceneManager.UnloadScene("MainMenu");
        SceneManager.LoadScene("GameScene");
        
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        LogMessage("Create room failed: " + returnCode + " : " + message);
    }

   
    public void JoinRoom()
    {
        if (!RoomToJoin.Equals(""))
        {
            PhotonNetwork.JoinRoom(RoomToJoin);
        }
        
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        LogMessage("Joined room " + PhotonNetwork.CurrentRoom);
    }

    private void LogMessage(string message)
    {
        DebugLog.GetComponent<DebugLog>().ShowMessage(message);
    }
}
