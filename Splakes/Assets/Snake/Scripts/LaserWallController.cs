using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Topology;


public class LaserWallController : MonoBehaviour
{
    //Material data
    public Color[] LaserWallColors; //Array of availible laser wall colors
    public Color CurrentLaserWallColor;
    public Material LaserWallBaseMaterial; //Base laser wall material

    private Material sharedLaserWallMaterial;

    //Wall generation data
    private List<int> laserSegmentStartIndices;
    private List<int> laserSegmentEndIndices;

    //Mesh Data
    private GameObject laserMeshObj;
    private Mesh laserMesh;

    //Reference to parent snake data
    List<Transform> snakeBody;

    //Timing control
    public int UpdatesPerSecond = 3;
    private float secondsPerUpdate;
    private float timeSinceLastUpdate;

    //Control data
    bool findLaserMeshes;

    //Active control
    public bool IsActive;


    public void Init(ref List<Transform> snakeBody, bool findLaserMeshes)
    {
        this.snakeBody = snakeBody;
        this.findLaserMeshes = findLaserMeshes;

        IsActive = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        //Initialize laser wall lists
        laserSegmentStartIndices = new List<int>();
        laserSegmentEndIndices = new List<int>();


        sharedLaserWallMaterial = new Material(LaserWallBaseMaterial);

        CurrentLaserWallColor = LaserWallColors[UnityEngine.Random.Range(0, LaserWallColors.Length)];
        sharedLaserWallMaterial.SetColor("_AlbedoColor", CurrentLaserWallColor);

        laserMesh = new Mesh();
        laserMeshObj = new GameObject("Laser Wall");
        MeshFilter tempFilter = laserMeshObj.AddComponent<MeshFilter>();
        MeshRenderer tempRen = laserMeshObj.AddComponent<MeshRenderer>();
        tempFilter.sharedMesh = laserMesh;
        tempRen.sharedMaterial = sharedLaserWallMaterial;

        secondsPerUpdate = 1 / UpdatesPerSecond;
    }

    // Update is called once per frame
    void Update()
    {
        if (IsActive)
        {
            timeSinceLastUpdate += Time.deltaTime;
            if (timeSinceLastUpdate >= secondsPerUpdate && findLaserMeshes)
            {
                UpdateLaserWalls(); //Only update seldomly
                timeSinceLastUpdate -= secondsPerUpdate;
            }

            generateLaserMeshes(); //Always update the meshes
        }
    }



    private void OnDestroy()
    {
        Destroy(sharedLaserWallMaterial);
        Destroy(laserMesh);
    }

    private void UpdateLaserWalls()
    {
        laserSegmentStartIndices = new List<int>(); //Holds the Body index of laser segment start
        laserSegmentEndIndices = new List<int>(); //Holds the Body index of laser segment end

        List<Vector3> segmentPlaneNormals = new List<Vector3>();

        int startIndex = -1;
        int planeCount = 0;

        bool foundAllNaN = false;

        //Find all laser segments
        do
        {
            laserSegmentStartIndices.Add(startIndex); //Add the start

            object[] laserPlaneData = findLaserPlane(startIndex); //Get the plane end index and normal vector

            laserSegmentEndIndices.Add((int)laserPlaneData[0]); //Add the end
            segmentPlaneNormals.Add((Vector3)laserPlaneData[1]); //Add the normal of the plane found

            planeCount++; //Adds a plane
            startIndex++; //Increment start index

            //If they are all NaN we don't want lasers
            if ((bool)laserPlaneData[2])
            {
                foundAllNaN = true;
            }

        } while (laserSegmentEndIndices[laserSegmentEndIndices.Count - 1] != snakeBody.Count - 1);

        //Debug.Log("~~~~~~~~~~~~~~~~~");

        //Clean up laser Segments if more than 1
        if (planeCount > 1)
        {
            cleanDirtyEndIndexPlanes(ref planeCount, ref segmentPlaneNormals); //Removes segments with the same end indices
            //removeSmallPlanes(ref planeCount, ref segmentPlaneNormals);
            combineSimilarPlanes(ref planeCount, ref segmentPlaneNormals, 20f); //Combines planes that are very similar in orientation
        }
        else
        {
            if (foundAllNaN)
            {
                laserSegmentStartIndices = new List<int>();
                laserSegmentEndIndices = new List<int>();
                planeCount = 0;
            }
        }
    }


    public void CheckCollision(SnakeController snake)
    {
        if (!IsActive)
        {
            return;
        }

        Vector3[] verts = laserMesh.vertices;
        int[] tris = laserMesh.triangles;

        for (int i = 0; i < tris.Length / 3; i += 3)
        {
            //Points of the triangle
            Vector3 p1 = verts[tris[i]];
            Vector3 p2 = verts[tris[i + 1]];
            Vector3 p3 = verts[tris[i + 2]];

            //Equation of the plane of the triangle
            float[] planeEq = Tools.GetPlaneFrom3Points(p1, p2, p3);

            for (int j = -1; j < snake.Body.Count; ++j) {

                Transform currentBodySeg = null;
                Vector3 bodyPos = Vector3.zero;
                float sphereRadius = float.PositiveInfinity;
                
                

                if (j == -1)
                {
                    //For head
                    bodyPos = snake.transform.position;
                    currentBodySeg = snake.Head;
                    sphereRadius = 0.75f;
                }
                else
                {
                    //For body
                    bodyPos = snake.Body[j].position;
                    currentBodySeg = snake.Body[j];
                    sphereRadius = 0.5f;
                }


                //Signed distance from body to plane of triangle
                float dist = Tools.GetSignedDistancePointToPlane(bodyPos, planeEq);

                if (dist > sphereRadius)
                {
                    //No collision
                    break;
                }
                else
                {
                    Vector3 planeNormal = Tools.GetPlaneNormal(planeEq);

                    //Translate the body position to plane
                    Vector3 pointOnPlane = bodyPos - (planeNormal * dist);

                    if (Tools.PointInTriangle(pointOnPlane, p1, p2, p3))
                    {
                        //Collision
                        currentBodySeg.GetComponent<SnakeBodyController>().LaserWallCollision();
                        return;
                    }
                }

            }
            
        }
    }


    public void Kill(bool stayActive)
    {

        

        
    }


    #region Mesh Generation
    private void generateLaserMeshes()
    {
        List<Vector3> verts = new List<Vector3>();
        int vertStartIndex = 0;

        List<int> tris = new List<int>();

        List<Vector2> UVs = new List<Vector2>();

        for (int i = 0; i < laserSegmentEndIndices.Count; ++i)
        {
            int endIndex = laserSegmentEndIndices[i];
            int startIndex = laserSegmentStartIndices[i];

            int pointCount = endIndex - startIndex + 1;
            Vector3[] verticies = getVertices(startIndex, pointCount, ref verts);

            //Get mesh plane for 3D to 2D mapping
            float[] planeEq = Tools.GetPlaneFrom3Points(verticies[0], verticies[verticies.Length - 2], verticies[verticies.Length - 1]);
            Vector3 planeNormal = Tools.GetPlaneNormal(planeEq);

            //Map the points to a local 2D plane
            Vector2[] mappedPoints = Tools.Map3Dto2D(verticies, planeNormal);

            //Get uvs for texture/shader
            UVs.AddRange(getUVs(mappedPoints));

            IEnumerator<Triangle> triangleEnumerator = triangulate(mappedPoints, out bool validMesh);
            if (validMesh)
            {
                while (triangleEnumerator.MoveNext())
                {
                    //Get the current triangle
                    Triangle current = triangleEnumerator.Current;

                    //Vertex indices
                    int v2Index = current.vertices[2].id;
                    int v1Index = current.vertices[1].id;
                    int v0Index = current.vertices[0].id;

                    //Add the triangles to the the laser mesh triangles list
                    tris.Add(vertStartIndex + v2Index);
                    tris.Add(vertStartIndex + v1Index);
                    tris.Add(vertStartIndex + v0Index);
                }
            }

            vertStartIndex += pointCount;
        }

        laserMesh.Clear();

        laserMesh.vertices = verts.ToArray();
        laserMesh.triangles = tris.ToArray();
        laserMesh.SetUVs(0, UVs.ToArray());

        laserMesh.RecalculateNormals();
        laserMesh.RecalculateTangents();
        laserMesh.RecalculateBounds();
    }

    private Vector3[] getVertices(int startIndex, int pointCount, ref List<Vector3> verts)
    {
        Vector3[] vertices = new Vector3[pointCount];

        for (int j = 0; j < pointCount; ++j)
        {
            int index = startIndex + j;
            if (index == -1)
            {
                vertices[j] = transform.position;
            }
            else
            {
                vertices[j] = snakeBody[index].position;
            }

            verts.Add(vertices[j]);
        }

        return vertices;
    }
    
    private IEnumerator<Triangle> triangulate(Vector2[] mappedPoints, out bool validMesh)
    {
        //Create polygon for triangulation
        Polygon poly = new Polygon();
        for (int j = 0; j < mappedPoints.Length; ++j)
        {
            poly.Add(new Vertex(mappedPoints[j].x, mappedPoints[j].y));
        }

        //Triangulate
        ConstraintOptions options = new ConstraintOptions();

        TriangleNet.Mesh tempMesh = null;
       
        try
        {
           
            tempMesh = (TriangleNet.Mesh)poly.Triangulate(options);
            validMesh = true;
            return tempMesh.triangles.GetEnumerator();
            
        }
        catch 
        {
            validMesh = false;
            return null;
        }
    }
    
    private Vector2[] getUVs(Vector2[] mappedPoints)
    {
        Vector2[] UVs = new Vector2[mappedPoints.Length];

        Vector2 lowBounds = new Vector2(-100, -100);
        Vector2 highBounds = new Vector2(100, 100);

        for (int i = 0; i < mappedPoints.Length; ++i)
        {
            float UVx = Tools.Map(mappedPoints[i].x, lowBounds.x, highBounds.x, 0, 1);
            float UVy = Tools.Map(mappedPoints[i].y, lowBounds.y, highBounds.y, 0, 1);
            UVs[i] = new Vector2(UVx, UVy);
        }

        return UVs;
    }
    #endregion

    #region Plane finding and cleaning
    private object[] findLaserPlane(int startIndex)
    {
        //If from start index to tail is less than 3, can't make a plane and will be removed from list of planes later
        if (snakeBody.Count - 1 - startIndex < 3)
        {
            return new object[] { snakeBody.Count - 1, Vector3.zero, false };
        }


        bool isAllNaN = true;

        //If there is potential for the plane to have 3 or more points then we are gucci
        List<Vector3> points = new List<Vector3>(); //Points one the plane

        //First point is always on the plane
        if (startIndex == -1)
        {
            points.Add(transform.position);
        }
        else
        {
            points.Add(snakeBody[startIndex].position);
        }

        //Add the next two points to get our 3 points needed to create a plane
        points.Add(snakeBody[startIndex + 1].position);
        points.Add(snakeBody[startIndex + 2].position);

        float[] planeEquation = Tools.GetPlaneFrom3Points(points[0], points[1], points[2]); //Obtain the plain equation with the three points
        Vector3 planeNormal = Tools.GetPlaneNormal(planeEquation); //Get the plane normal of the 3 points

        bool pointIsOnPlane = false;
        int endIndex = startIndex + 3;
        do
        {
            pointIsOnPlane = false; //Assume the point isn't on the plane

            Vector3 point = snakeBody[endIndex].position; //Get the point we are evaluating
            float distToPlane = Tools.GetDistancePointToPlane(point, planeEquation); //Evaluate the points distance to the plane

            if (distToPlane <= 0.2 || float.IsNaN(distToPlane)) //Check if it is arbitrarily close to the plane. If distance is NaN then points are colinear
            {
                if (!float.IsNaN(distToPlane))
                {
                    isAllNaN = false;
                }

                //If it is the point counts as being on the plane
                pointIsOnPlane = true;
                endIndex++;

                //Update the plane equation using the new point
                planeEquation = Tools.GetPlaneFrom3Points(points[0], points[1], point);
                planeNormal = Tools.GetPlaneNormal(planeEquation);
            }

        } while (pointIsOnPlane && endIndex <= snakeBody.Count - 1);

        endIndex--; //We need to delete the failed evaluated point

        return new object[] { endIndex, planeNormal, isAllNaN };
    }

    private void cleanDirtyEndIndexPlanes(ref int planeCount, ref List<Vector3> segmentPlaneNormals)
    {
        bool cleaning = true;
        int cleanIndex = 0; //Start at first segment

        while (cleaning) //We dont need to evaluate the last segment
        {
            int currentEndIndex = laserSegmentEndIndices[cleanIndex]; //End index of the current plane

            List<int> indicesToRemove = new List<int>(); //Planes we want to remove

            //Search all planes after the current plane
            for (int i = cleanIndex + 1; i < planeCount; ++i)
            {
                //If the plane we are checking has the same end index as the plane we are cleaning, then we need to remove it
                if (laserSegmentEndIndices[i] == currentEndIndex)
                {
                    indicesToRemove.Add(i);
                }
            }

            //Remove the planes we need to remove
            for (int i = 0; i < indicesToRemove.Count; ++i)
            {
                int removeIndex = indicesToRemove[i];

                laserSegmentStartIndices.RemoveAt(removeIndex);
                laserSegmentEndIndices.RemoveAt(removeIndex);
                segmentPlaneNormals.RemoveAt(removeIndex);

                //Need to move all remove indices back one
                for (int j = 0; j < indicesToRemove.Count; ++j)
                {
                    indicesToRemove[j]--;
                }

                planeCount--; //We remove a plane so we decrement the amount of planes
            }

            cleanIndex++; //Increment clean index

            //When we reach the last plane, we no longer need to clean
            if (cleanIndex + 1 == planeCount)
            {
                cleaning = false;
            }
        }
    }

    private void combineSimilarPlanes(ref int planeCount, ref List<Vector3> segmentPlaneNormals, float similarityThreshold)
    {
        bool combining = true;
        int combineIndex = 0;

        //Combine all subsequent planes if the are similar enough
        while (combining)
        {
            int index = combineIndex + 1;
            while (index < planeCount)
            {
                //Check the angle difference between the plane normals
                float checkAngle = Vector3.Angle(segmentPlaneNormals[combineIndex], segmentPlaneNormals[index]);

                //If the angle difference small enough we can combine this plane with the current plane
                //If the plane normals are 180 degrees seperated then they still have the same orientation, so we must check the inverse as well
                if (checkAngle <= similarityThreshold || 180 - checkAngle <= similarityThreshold)
                {
                    laserSegmentStartIndices.RemoveAt(index); //Remove this segments start index

                    laserSegmentEndIndices[combineIndex] = laserSegmentEndIndices[index]; //Make the current segments end this segments end, this is the combination
                    laserSegmentEndIndices.RemoveAt(index); //Remove this segements end index

                    //segmentPlaneNormals[combineIndex] = segmentPlaneNormals[index]; //Make the current segments normal this segments normal, this is for further evaluation
                    segmentPlaneNormals.RemoveAt(index); //Remove this segments plane normal for the list as this plane does not exist anymore

                    planeCount--; //We removed a plane by combining it, so decrement the amount of planes*/
                }
                else
                {
                    //Once we dont find one we can break
                    break;
                }
            }


            combineIndex++; //Move to the next plane to combine
            //Combine all up to the end
            if (combineIndex + 1 >= planeCount)
            {
                combining = false;
            }

        }

    }

    private void removeSmallPlanes(ref int planeCount, ref List<Vector3> segmentPlaneNormals)
    {
        /*
        //List of which planes are small
        List<int> smallPlaneIndices = new List<int>();

        bool firstIsSmall = false;
        bool lastIsSmall = false;

        for (int i = 0; i < planeCount; ++i)
        {
            int pointCount = laserSegmentEndIndices[i] - laserSegmentStartIndices[i] + 1;

            //Segments of size 3 are considered small planes
            if (pointCount == 3)
            {
                smallPlaneIndices.Add(i);

                if (i == 0)
                {
                    firstIsSmall = true;

                }

                if (i == planeCount - 1)
                {
                    lastIsSmall = true;
                }
            }
        }





        /*
        //Get the small planes that are sequential
        List<int> sequentialSmallPlanes = new List<int>();
        sequentialSmallPlanes.Add(smallPlaneIndices[0]); //First one is sequential with itself basically
        smallPlaneIndices.RemoveAt(0); //remove it from the other list

        //Adds all sequential indices to the list
        int maxCheckCount = smallPlaneIndices.Count;
        for (int i = 0; i < maxCheckCount; ++i)
        {
            if (smallPlaneIndices[0] - sequentialSmallPlanes[sequentialSmallPlanes.Count - 1] == 1)
            {
                sequentialSmallPlanes.Add(smallPlaneIndices[0]);
                smallPlaneIndices.RemoveAt(0);
            }
            else
            {
                break;
            }
        }
        //Done getting sequential small planes

        //Adjust planes behind and infront of the small planes
        //Sets the end index of the segment before the first small plane to the end of the last first small plane
        if (!firstIsSmall)
        {
            laserSegmentEndIndices[sequentialSmallPlanes[0] - 1] = laserSegmentEndIndices[sequentialSmallPlanes[0]];
        }

        //Sets the start index of the plane after the sequential small planes to the start of the last sequential small plane
        if (!lastIsSmall)
        {
            laserSegmentStartIndices[sequentialSmallPlanes[sequentialSmallPlanes.Count - 1] + 1] = laserSegmentStartIndices[sequentialSmallPlanes[sequentialSmallPlanes.Count - 1]];
        }


        //Remove the small planes
        for (int i = 0; i < sequentialSmallPlanes.Count; ++i)
        {
            if (i == 0 && firstIsSmall)
            {
                continue;
            }

            if (i == smallPlaneIndices.Count - 1 && lastIsSmall)
            {
                continue;
            }

                    
            laserSegmentEndIndices.RemoveAt(sequentialSmallPlanes[i]);
            laserSegmentStartIndices.RemoveAt(sequentialSmallPlanes[i]);
            segmentPlaneNormals.RemoveAt(sequentialSmallPlanes[i]);
            planeCount--;

            //Decrement all the indices since we are removing a plane
            for (int j = 0; j < sequentialSmallPlanes.Count; ++j)
            {
                smallPlaneIndices[j]--;
            }
                    
        }*/


    }
    #endregion

    #region Synchronization
    public object GetIndicesForSync()
    {
        object laserMeshSyncList = new object[]
        {
            laserSegmentStartIndices.ToArray(),
            laserSegmentEndIndices.ToArray()
        };

        return laserMeshSyncList;
    }

    public void SetIndicesForSync(object laserMeshSyncListObject)
    {
        object[] laserMeshSyncList = (object[])laserMeshSyncListObject;

        laserSegmentStartIndices = new List<int>();
        laserSegmentEndIndices = new List<int>();

        laserSegmentStartIndices.AddRange((int[])laserMeshSyncList[0]);
        laserSegmentEndIndices.AddRange((int[])laserMeshSyncList[1]);
    }
    #endregion
}
