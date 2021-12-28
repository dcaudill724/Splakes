using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;
using ExitGames.Client.Photon;

public enum SnakeEvents : byte
{
    NeedSync = 1,
    SnakeStartedDying = 2,
    SnakeHurt = 3,
    SnakeRespawn = 4,
    SnakeSegmentDied = 5,
}

public interface EventReceiver
{
    void ReceiveEvent(string eventName, object content);
}

public static class EasyEventSystem
{
    private static List<EventReceiver> eventReceivers;

    static EasyEventSystem()
    {
        eventReceivers = new List<EventReceiver>();
    }

    public static void AddReceiver(EventReceiver receiver)
    {
        eventReceivers.Add(receiver);
    }

    public static void RaiseLocalEvent(string eventName, object content = null)
    {
        foreach (EventReceiver er in eventReceivers)
        {
            er.ReceiveEvent(eventName, content);
        }
    }

    public static void RaiseNetworkEvent(SnakeEvents snakeEvent, object eventContent = null, RaiseEventOptions reo = null)
    {
        if (reo == null) 
        {
            reo = new RaiseEventOptions();
        }

        PhotonNetwork.RaiseEvent((byte)snakeEvent, eventContent, reo, SendOptions.SendReliable);
    }
}
