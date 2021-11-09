using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnSnakes : MonoBehaviour
{
    public Vector3 MinSpawnBounds;
    public Vector3 MaxSpawnBounds;
    public List<GameObject> Snakes;
    public GameObject Camera;

    // Start is called before the first frame update
    void Start()
    {
        float tempX = Random.Range(MinSpawnBounds.x, MaxSpawnBounds.x);
        float tempY = Random.Range(MinSpawnBounds.y, MaxSpawnBounds.y);
        float tempZ = Random.Range(MinSpawnBounds.z, MaxSpawnBounds.z);
        Vector3 tempSpawn = Vector3.zero;//new Vector3(tempX, tempY, tempZ);

        GameObject snake = Instantiate(Resources.Load("Snake") as GameObject);
        snake.GetComponent<SnakeController>().SpawnPoint = tempSpawn;
        Snakes.Add(snake);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
