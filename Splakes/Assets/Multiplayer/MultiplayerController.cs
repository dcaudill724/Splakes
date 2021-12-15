using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.SceneManagement;

public class MultiplayerController : MonoBehaviourPunCallbacks
{
    public TextMeshProUGUI DebugLog;
    public RoomListContentController RoomListContent;

    [HideInInspector]
    public string RoomToJoin; //Is set when a room is selected from the list of rooms UI element
    

    string gameVersion = "1";

    #region Game Startup Connection
    void Awake()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.AutomaticallySyncScene = true; //So when a guest client joins the room it loads GameScene
        }
    }

    void Start()
    {
        //Connect to the Photon server
        if (!PhotonNetwork.IsConnected)
        {
            LogMessage("Connecting");
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.ConnectUsingSettings();
        }

    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();

        //Once connected to the Photon server, join the default Lobby
        PhotonNetwork.JoinLobby();
        LogMessage("Connected");
        
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        RoomListContent.LoadList(roomList);
    }
    #endregion

    public void SetNickname(TextMeshProUGUI nicknameInput)
    {
        PhotonNetwork.NickName = nicknameInput.text;
    }

    //Create a room based on the name of the room given from the room name input field
    public void CreateRoom(TextMeshProUGUI roomNameInput)
    {
        //Set Room options
        RoomOptions ro = new RoomOptions();
        ro.MaxPlayers = 16;

        //Create the room!
        PhotonNetwork.CreateRoom(roomNameInput.text, ro, TypedLobby.Default);
    }

    public override void OnCreatedRoom()
    {
        base.OnCreatedRoom();
        LogMessage("Created room succesfully");

        //When a player creates a room they will want to be spawned into the GameScene immediately
        SceneManager.LoadScene("GameScene");
        
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        LogMessage("Create room failed: " + returnCode + " : " + message);
    }

    
    //Join the room that has been selected
    public void JoinRoom()
    {
        if (!RoomToJoin.Equals(""))
        {
            PhotonNetwork.JoinRoom(RoomToJoin);
        }
        Debug.Log("Joining Room");
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        LogMessage("Joined room " + PhotonNetwork.CurrentRoom);
        Debug.Log("Joined room");
    }

    private void LogMessage(string message)
    {
        DebugLog.GetComponent<DebugLog>().ShowMessage(message);
    }
}
