using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnSnakes : MonoBehaviour
{
    
    public Vector3 MinSpawnBounds;
    public Vector3 MaxSpawnBounds;
    public int EnemyCount = 0;

    //All snakes on map
    public List<GameObject> Snakes;

    //Snake Prefab
    public GameObject SnakePrefab;


    // Start is called before the first frame update
    void Start()
    {
        float tempX = Random.Range(MinSpawnBounds.x, MaxSpawnBounds.x);
        float tempY = Random.Range(MinSpawnBounds.y, MaxSpawnBounds.y);
        float tempZ = Random.Range(MinSpawnBounds.z, MaxSpawnBounds.z);
        Vector3 tempSpawn = new Vector3(tempX, tempY, tempZ);

        GameObject snake = Instantiate(SnakePrefab, new Vector3(0, 0, 0), Quaternion.identity);
        snake.name = "Main Snake";
        snake.GetComponent<SnakeController>().IsMainSnake = true;
        snake.GetComponent<SnakeController>().SpawnPoint = tempSpawn;
        Snakes.Add(snake);

        for (int i = 0; i < EnemyCount; ++i)
        {
            tempX = Random.Range(MinSpawnBounds.x, MaxSpawnBounds.x);
            tempY = Random.Range(MinSpawnBounds.y, MaxSpawnBounds.y);
            tempZ = Random.Range(MinSpawnBounds.z, MaxSpawnBounds.z);
            tempSpawn = new Vector3(tempX, tempY, tempZ);

            GameObject enemySnake = Instantiate(SnakePrefab, new Vector3(0, 0, 0), Quaternion.identity);
            enemySnake.name = "Enemy Snake: " + i;
            snake.GetComponent<SnakeController>().IsMainSnake = true;
            snake.GetComponent<SnakeController>().SpawnPoint = tempSpawn;
            Snakes.Add(enemySnake);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
