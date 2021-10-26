using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateSnake : MonoBehaviour
{
    //Generation Data
    public Vector3 SpawnPoint;
    public int SnakeStartingLength = 5;

    //Body data
    public GameObject Head;
    public List<GameObject> Body;

    private float segmentSpawnOffset;

    // Start is called before the first frame update
    void Start()
    {
        //Generate head
        Head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Head.transform.position = SpawnPoint;
        Head.transform.localScale = Vector3.Scale(Head.transform.localScale, new Vector3(1.5f, 1.5f, 1.5f));
        segmentSpawnOffset += 1.25f;

        //Generate body
        Body = new List<GameObject>();
        for (int i = 0; i < SnakeStartingLength; ++i)
        {
            GameObject tempBodySeg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tempBodySeg.transform.position = SpawnPoint + new Vector3(segmentSpawnOffset, 0, 0);
            segmentSpawnOffset += 1.0f;
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
