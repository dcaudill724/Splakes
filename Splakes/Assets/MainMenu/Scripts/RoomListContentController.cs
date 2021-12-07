using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;

public class RoomListContentController : MonoBehaviour
{
    public GameObject RoomListItemPrefab;
    public MultiplayerController MultiplayerController;

    private bool listLoaded = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Update()
    {
        if (!listLoaded && MultiplayerController.IsReady)
        {
            loadList();
        }
    }

    void loadList()
    {
        List<RoomInfo> roomList = MultiplayerController.RoomList;

        foreach (RoomInfo r in roomList)
        {
            GameObject roomListItem = Instantiate(RoomListItemPrefab, transform);
            roomListItem.transform.Find("RoomNameText").GetComponent<TextMeshProUGUI>().text = r.Name;
            roomListItem.transform.Find("RoomPopText").GetComponent<TextMeshProUGUI>().text = r.PlayerCount + "/" + r.MaxPlayers;
            roomListItem.GetComponent<RoomListItemContoller>().MultiplayerController = MultiplayerController;
        }

        listLoaded = true;
    }
}
