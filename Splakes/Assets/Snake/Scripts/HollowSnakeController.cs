using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;

public class HollowSnakeController : MonoBehaviour
{
    public GameObject SnakeHeadPrefab;
    public GameObject SnakeBodyPrefab;
    public Material LaserWallMaterial;
    public GameObject Head;
    public List<GameObject> Body;
    public List<GameObject> LaserWalls;
    public Color CurrentSnakeColor;
    public Color CurrentLaserWallColor;

    public void InitHollowSnake(Vector3?[] snakePositionData)
    {
        Body = new List<GameObject>();
        LaserWalls = new List<GameObject>();

        Head = Instantiate(SnakeBodyPrefab);
        Head.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        Head.transform.position = (Vector3)snakePositionData[0];

        updateBody(snakePositionData);
    }

    public void UpdateHollowSnake(Vector3?[] snakePositionData, Vector3?[] laserWallMeshVerticies)
    {
        updateBody(snakePositionData);
        updateLaserWalls(laserWallMeshVerticies);
    }

    private void updateBody(Vector3?[] snakePositionData)
    {
        //If the snake got shorter
        if (snakePositionData.Length < Body.Count + 1)
        {
            Head.transform.position = (Vector3)snakePositionData[0];

            int endIndex = 1;
            for (int i = 1; i < snakePositionData.Length; ++i)
            {
                Body[i - 1].transform.position = (Vector3)snakePositionData[i];
                endIndex++;
            }

            for (int i = endIndex; i < snakePositionData.Length; ++i)
            {
                int endOfBodyListIndex = Body.Count - 1;
                Destroy(Body[endOfBodyListIndex]);
                Body.RemoveAt(endOfBodyListIndex);
            }
        }

        //If the snake grew longer
        else if (snakePositionData.Length > Body.Count + 1)
        {
            Head.transform.position = (Vector3)snakePositionData[0];

            for (int i = 1; i < Body.Count + 1; ++i)
            {
                Body[i - 1].transform.position = (Vector3)snakePositionData[i];
            }

            for (int i = Body.Count + 1; i < snakePositionData.Length; ++i)
            {
                addBodySegment((Vector3)snakePositionData[i]);
            }
        }

        //If the length of the snake stays the same
        else
        {
            Head.transform.position = (Vector3)snakePositionData[0];

            for (int i = 1; i < Body.Count + 1; ++i)
            {
                Body[i - 1].transform.position = (Vector3)snakePositionData[i];
            }
        }
    }

    private void updateLaserWalls(Vector3?[] laserWallMeshVerticies)
    {
        List<List<Vector3>> splitLaserWallVertices = new List<List<Vector3>>();

        int currentLaserWallIndex = -1;
        for (int i = 0; i < laserWallMeshVerticies.Length; ++i)
        {
            if (laserWallMeshVerticies[i] == null)
            {
                currentLaserWallIndex++;
                splitLaserWallVertices.Add(new List<Vector3>());
            }
            else
            {
                splitLaserWallVertices[currentLaserWallIndex].Add((Vector3)laserWallMeshVerticies[i]);
            }
        }

        
        int laserMeshesSegmentsDifference = LaserWalls.Count - splitLaserWallVertices.Count;
        if (laserMeshesSegmentsDifference > 0) //If we have more meshes than lasers
        {
            for (int i = 0; i < laserMeshesSegmentsDifference; ++i)
            {
                Destroy(LaserWalls[0]);
                LaserWalls.RemoveAt(0);
            }
        }
        else if (laserMeshesSegmentsDifference < 0) //if we have more lasers than meshes
        {
            for (int i = 0; i < -laserMeshesSegmentsDifference; ++i)
            {
                GameObject laserWall = new GameObject("laser wall");
                laserWall.layer = 11;
                laserWall.AddComponent<MeshFilter>();
                laserWall.AddComponent<MeshRenderer>();
                laserWall.AddComponent<LaserWallController>();
                laserWall.AddComponent<MeshCollider>();

                laserWall.GetComponent<MeshRenderer>().material = LaserWallMaterial;
                //laserWall.GetComponent<Renderer>().material.SetColor("_AlbedoColor", CurrentLaserWallColor);

                LaserWalls.Add(laserWall);
            }

        }


        
        for (int i = 0; i < LaserWalls.Count; ++i)
        {
            Mesh mesh = new Mesh();
            LaserWalls[i].GetComponent<MeshFilter>().mesh = mesh;
            LaserWalls[i].GetComponent<MeshCollider>().sharedMesh = mesh;

            Vector3[] vertices = splitLaserWallVertices[i].ToArray();

            int pointCount = vertices.Length - 1;
            int trianglesCount = (pointCount - 1) * 3;
            int[] triangles = new int[trianglesCount];
            for (int j = 0; j < pointCount - 1; ++j)
            {
                int frontIndex = j * 3;
                triangles[frontIndex] = j;
                triangles[frontIndex + 1] = pointCount;
                triangles[frontIndex + 2] = j + 1;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            LaserWalls[i].GetComponent<MeshCollider>().convex = true;
        }
        
    }

    private void addBodySegment(Vector3 position)
    {
        GameObject tempBodySegment = Instantiate(SnakeBodyPrefab);
        tempBodySegment.transform.position = position;
        Body.Add(tempBodySegment);
    }
}
