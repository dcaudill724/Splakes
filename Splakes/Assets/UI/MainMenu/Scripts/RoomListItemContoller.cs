using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;


public class RoomListItemContoller : MonoBehaviour, ISelectHandler
{
    public MultiplayerController MultiplayerController;

    public void OnSelect(BaseEventData eventData)
    {
        MultiplayerController.RoomToJoin = transform.Find("RoomNameText").GetComponent<TextMeshProUGUI>().text;
    }
}
