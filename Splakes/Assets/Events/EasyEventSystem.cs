using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface EventReceiver
{
    void ReceiveEvent(string eventName, object content);
}

public static class EasyEventSystem
{
    private static List<EventReceiver> eventReceivers;

    static EasyEventSystem()
    {
        Debug.Log("Events system");
        eventReceivers = new List<EventReceiver>();
    }

    public static void AddReceiver(EventReceiver receiver)
    {
        eventReceivers.Add(receiver);
    }

    public static void RaiseEvent(string eventName, object content)
    {
        foreach (EventReceiver er in eventReceivers)
        {
            er.ReceiveEvent(eventName, content);
        }
    }
}
