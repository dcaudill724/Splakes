using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Realtime;
using Photon.Pun;

public class SpawnSnakes : MonoBehaviour
{
    
    public Vector3 MinSpawnBounds;
    public Vector3 MaxSpawnBounds;
    public TextMeshProUGUI SnakeScoreText;
    public TextMeshProUGUI SnakeLengthText;
    public TextMeshProUGUI DebugText;

    private SnakeController snake;
    private Dictionary<Player, HollowSnakeController> hollowSnakes = new Dictionary<Player, HollowSnakeController>();

    //Snake Prefab
    public GameObject SnakePrefab;
    public GameObject HollowSnakePrefab;

    void Start()
    {

    }

    public void SpawnSnake(Player player)
    {
        float tempX = Random.Range(MinSpawnBounds.x, MaxSpawnBounds.x);
        float tempY = Random.Range(MinSpawnBounds.y, MaxSpawnBounds.y);
        float tempZ = Random.Range(MinSpawnBounds.z, MaxSpawnBounds.z);
        Vector3 tempSpawn = new Vector3(tempX, tempY, tempZ);

        GameObject snake = Instantiate(SnakePrefab, new Vector3(0, 0, 0), Quaternion.identity);
        snake.name = "Snake: " + player;
        snake.GetComponent<SnakeController>().SpawnPoint = tempSpawn;
        snake.GetComponent<SnakeController>().SnakeScoreText = SnakeScoreText;
        snake.GetComponent<SnakeController>().SnakeLengthText = SnakeLengthText;
        this.snake = snake.GetComponent<SnakeController>();
    }

    public object[] GetSnakeBodyPositionData()
    {
        Vector3[] positionData = snake.GetBodyPositionData();
        object[] objPositionData = new object[positionData.Length];

        for (int i = 0; i < positionData.Length; ++i)
        {
            objPositionData[i] = positionData[i] as object;
        }

        return objPositionData;
    }

    public object[] GetLaserWallMeshVerticies()
    {
        Vector3?[] vertexData = snake.GetLaserWallMeshVerticies();
        object[] objVertexData = new object[vertexData.Length];

        for (int i = 0; i < vertexData.Length; ++i)
        {
            objVertexData[i] = vertexData[i] as object;
        }

        return objVertexData;
    }

    public void GenerateHollowSnake(Vector3?[] snakePositionData, Vector3?[] laserWallMeshVerticies, Player player)
    {
        GameObject hollowSnake = Instantiate(HollowSnakePrefab, Vector3.zero, Quaternion.identity);
        hollowSnake.GetComponent<HollowSnakeController>().InitHollowSnake(snakePositionData);
        hollowSnakes.Add(player, hollowSnake.GetComponent<HollowSnakeController>());
    }

    public bool UpdateSnakeByPlayer(Player player, object[] objBodyPositions, object[] objLaserWallMeshVerticies)
    {
        Vector3?[] bodyPositions = new Vector3?[objBodyPositions.Length];
        Vector3?[] laserWallMeshVerticies = new Vector3?[objLaserWallMeshVerticies.Length];
        
        for (int i = 0; i < objBodyPositions.Length; ++i)
        {
            bodyPositions[i] = objBodyPositions[i] as Vector3?;
        }

        for (int i = 0; i < objLaserWallMeshVerticies.Length; ++i)
        {
            laserWallMeshVerticies[i] = objLaserWallMeshVerticies[i] as Vector3?;
        }


        if (hollowSnakes.ContainsKey(player))
        {
            hollowSnakes[player].UpdateHollowSnake(bodyPositions, laserWallMeshVerticies);
            return true;
        }
        else
        {
            GenerateHollowSnake(bodyPositions, laserWallMeshVerticies, player);
            return false;
        }
        
    }
}
