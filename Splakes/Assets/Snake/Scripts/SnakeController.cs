using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class SnakeController : MonoBehaviour
{
    #region Generation data
    public Vector3 SpawnPoint;
    public int StartingLength;
    public float Speed;
    public float RotationSpeed;
    public GameObject SnakeHead;
    public float StartHeadSize;
    public float StartBodySize;
    #endregion

    #region Visual data
    public Material SnakeBodyMaterial;
    public Material LaserWallMaterial;
    public TextMeshProUGUI SnakeScoreText;
    public TextMeshProUGUI SnakeLengthText;
    #endregion

    #region Body data
    //Public body data for interactions
    [HideInInspector]
    public Transform Head;
    [HideInInspector]
    public Transform[] Body; //Bruh, List<Transform> causes huge frame drops when adding body segments in generateBody. Therefore I sadly use array. Also is parallel with BodyMovement
    [HideInInspector]
    public Transform DirectionLaser;
    [HideInInspector]
    public int CurrentLength;

    public GameObject BodySegmentPrefab;
    public int TurnThreshold;
    public Color[] SnakeColors;
    private Color currentSnakeColor;
    public Color[] LaserWallColors;
    private Color currentLaserWallColor;

    private List<int> laserSegmentStartIndeces;
    private List<int> laserSegmentEndIndeces;
    private List<GameObject> laserMeshes;
    #endregion

    #region Movement data
    private bool updateMovementData;
    private float movementDataUpdateTime;

    private float movementDistance;

    private Vector3[] bodyRotationAxes;
    private Vector3[] lastBodyPositions; //Parallel with Body and targetBodyPositions
    private Vector3[] targetBodyPositions; //Parallel with Body and lasyBodyPositions
    private Quaternion[] lastBodyRotations;
    private Quaternion[] targetBodyRotations;
    private float timeSinceLastTargetPosUpdate;
    #endregion

    #region Camera data
    public Transform Camera;
    #endregion

    #region Gameplay data
    public int Score;
    public int PointsForNewSegment;

    private int scoreLastGrow;
    private float currentHeadSize;
    private float currentBodySize;

    private bool dying = false;
    public float DyingAnimationLength = 1000;
    private float dyingAnimtionTimeLeft;

    Vector3 LastRotationAxis;
    #endregion


    // Start is called before the first frame update
    void Start()
    {
        laserMeshes = new List<GameObject>();
        laserSegmentStartIndeces = new List<int>();
        laserSegmentEndIndeces = new List<int>();

        currentSnakeColor = SnakeColors[Random.Range(0, SnakeColors.Length)];
        currentLaserWallColor = LaserWallColors[Random.Range(0, LaserWallColors.Length)];

        generateHead();
        initializePositionData();
        generateBody();
        initializeGameplayData();
        initializeMovementData();
        initializeCameraData();
        initializeHud();
    }

    // Update is called once per frame
    void Update()
    {
        if (!dying)
        {
            movementDataUpdateTime = currentBodySize / Speed; //Unit of time to measure when to update target body positions

            timeSinceLastTargetPosUpdate += Time.deltaTime;
            if (timeSinceLastTargetPosUpdate >= currentBodySize / Speed) //If time to update target positions
            {
                updateMovementData = true;
                timeSinceLastTargetPosUpdate -= movementDataUpdateTime; //Remove 1 unit of update time. I assume there will be a remainder to be accounted for

                UpdateLaserWalls();
            }

            moveHead();
            moveBody();
            DrawLaserWalls();
            updateMovementData = false;
        }
        else
        {
            updateDieAnimation();
        }
        

    }

    #region Multiplayer functions
    public Vector3[] GetBodyPositionData()
    {
        Vector3[] positionData = new Vector3[Body.Length + 1];
        positionData[0] = Head.transform.position;
        for (int i = 0; i < Body.Length; ++i)
        {
            positionData[i + 1] = Body[i].transform.position;
        }

        return positionData;
    }
    
    //Seperate different laser walls null
    public Vector3?[] GetLaserWallMeshVerticies()
    {
        List<Vector3?> laserWallVerticies = new List<Vector3?>();

        for (int i = 0; i < laserMeshes.Count; ++i)
        {
            laserWallVerticies.Add(null);

            Mesh temp = laserMeshes[i].GetComponent<MeshFilter>().mesh;
            for (int j = 0; j < temp.vertexCount; ++j)
            {
                laserWallVerticies.Add(temp.vertices[j]);
            }
        }

        return laserWallVerticies.ToArray();
    }

    public Vector3 GetSnakeBodyColor()
    {
        return new Vector3(currentSnakeColor.r, currentSnakeColor.g, currentSnakeColor.b);
    }

    public Vector3 GetSnakeLaserColor()
    {
        return new Vector3(currentLaserWallColor.r, currentLaserWallColor.g, currentLaserWallColor.b);
    }
    #endregion

    #region Gameplay functions
    public void FeedSnake(int points)
    {
        Score += points;
        SnakeScoreText.text = "Score: " + Score;
        updateSize(points);
        SnakeLengthText.text = "Length: " + CurrentLength;
    }

    void updateSize(int points)
    {
        //Add new segment if condition is met
        if (Score - scoreLastGrow >= PointsForNewSegment)
        {
            int newSegments = (Score - scoreLastGrow) / PointsForNewSegment;


            //Make new body arrays. Also look at how these badboys slope, looks very nice
            Transform[] newBody = new Transform[Body.Length + newSegments];
            Vector3[] newBodyRotationAxes = new Vector3[bodyRotationAxes.Length + newSegments];
            Vector3[] newLastBodyPositions = new Vector3[lastBodyPositions.Length + newSegments];
            Vector3[] newTargetBodyPositions = new Vector3[targetBodyPositions.Length + newSegments]; 
            Quaternion[] newLastBodyRotations = new Quaternion[lastBodyRotations.Length + newSegments];
            Quaternion[] newTargetBodyRotations = new Quaternion[targetBodyRotations.Length + newSegments];

            //Copy old arrays to new arrays
            for (int i = 0; i < CurrentLength; ++i)
            {
                newBody[i] = Body[i];
                newBodyRotationAxes[i] = bodyRotationAxes[i];
                newLastBodyPositions[i] = lastBodyPositions[i];
                newTargetBodyPositions[i] = targetBodyPositions[i];
                newLastBodyRotations[i] = lastBodyRotations[i];
                newTargetBodyRotations[i] = targetBodyRotations[i];
            }

            for (int i = 0; i < newSegments; ++i)
            {
                newBody[i + CurrentLength] = Instantiate(BodySegmentPrefab).transform;
                newBody[i + CurrentLength].SetParent(transform);
                newBody[i + CurrentLength].GetComponent<Renderer>().material.color = currentSnakeColor;
                newBody[i + CurrentLength].position = newBody[(i + CurrentLength) - 1].position + newBody[(i + CurrentLength) - 1].TransformDirection(-Vector3.forward) * currentBodySize;

                newBodyRotationAxes[i + CurrentLength] = bodyRotationAxes[(i + CurrentLength) - 1];
                newLastBodyPositions[i + CurrentLength] = lastBodyPositions[(i + CurrentLength) - 1];
                newTargetBodyPositions[i + CurrentLength] = targetBodyPositions[(i + CurrentLength) - 1];
                newLastBodyRotations[i + CurrentLength] = lastBodyRotations[(i + CurrentLength) - 1];
                newTargetBodyRotations[i + CurrentLength] = targetBodyRotations[(i + CurrentLength) - 1];
            }

            //Set arrays to new arrays
            Body = newBody;
            bodyRotationAxes = newBodyRotationAxes;
            lastBodyPositions = newLastBodyPositions;
            targetBodyPositions = newTargetBodyPositions;
            lastBodyRotations = newLastBodyRotations;
            targetBodyRotations = newTargetBodyRotations;

            CurrentLength += newSegments;

            //Update condition data
            scoreLastGrow = Score;
        }

        //Change head size
        Head.localScale = new Vector3(currentHeadSize, currentHeadSize, currentHeadSize);

        //Change body segment size
        for (int i = 0; i < Body.Length; ++i)
        {
            Body[i].localScale = new Vector3(currentBodySize, currentBodySize, currentBodySize);
        }
    }

    void UpdateLaserWalls()
    {
        laserSegmentStartIndeces = new List<int>(); //Holds the Body index of laser segment start
        laserSegmentEndIndeces = new List<int>(); //Holds the Body index of laser segment end

        int startIndex = -1; //start at head which is basically Body[-1]
        bool firstLaserSegmentValid = false;
        bool endOfFirstSegmentIsTail = false;
        int firstLaserSegEndIndex = findLaserSegment(startIndex, out firstLaserSegmentValid, out endOfFirstSegmentIsTail);

        if (firstLaserSegmentValid) //If a valid first segment is even found
        {
            laserSegmentStartIndeces.Add(startIndex); //Add the start index
            laserSegmentEndIndeces.Add(firstLaserSegEndIndex); //Add the end index

            if (!endOfFirstSegmentIsTail) //If we havent already reached the tail we will need to find more
            {
                bool endOfSegmentIsTail = false;
                bool currentSegmentIsValid = false;
               
                int currentStartIndex = startIndex + 1;
                int lastSegmentEndIndex = firstLaserSegEndIndex;

                bool searchingInLastSegment = true;
                int bestSegmentLength = 0;
                int bestSegmentStartIndex = 0;
                int bestSegmentEndIndex = 0;

                while (!endOfSegmentIsTail)
                {
                    int currentEndIndex = findLaserSegment(currentStartIndex, out currentSegmentIsValid, out endOfSegmentIsTail);

                    if (currentSegmentIsValid)
                    {
                        if (searchingInLastSegment)
                        {
                            if (currentStartIndex == lastSegmentEndIndex)
                            {
                                if (bestSegmentEndIndex > lastSegmentEndIndex)
                                {
                                    laserSegmentStartIndeces.Add(bestSegmentStartIndex); //Add the start index
                                    laserSegmentEndIndeces.Add(bestSegmentEndIndex); //Add the end index
                                    lastSegmentEndIndex = bestSegmentEndIndex;
                                    bestSegmentEndIndex = 0;
                                    bestSegmentLength = 0;
                                    bestSegmentStartIndex = 0;
                                }
                                
                                searchingInLastSegment = false;
                            }
                            else
                            {
                                int currentSegmentLength = Mathf.Abs(currentEndIndex - currentStartIndex);

                                if (currentSegmentLength > bestSegmentLength)
                                {
                                    bestSegmentLength = currentSegmentLength;
                                    bestSegmentStartIndex = currentStartIndex;
                                    bestSegmentEndIndex = currentEndIndex;
                                }
                            }
                        }
                        else
                        {
                            if (currentEndIndex > lastSegmentEndIndex)
                            {
                                laserSegmentStartIndeces.Add(currentStartIndex); //Add the start index
                                laserSegmentEndIndeces.Add(currentEndIndex); //Add the end index
                                lastSegmentEndIndex = currentEndIndex;
                                searchingInLastSegment = true;
                            }
                        }
                    }

                    currentStartIndex++;
                }

            }
        }




    }
    
    int findLaserSegment(int startIndex, out bool laserSegmentValid, out bool endOfSegmentIsTail)
    {
        laserSegmentValid = false;
        endOfSegmentIsTail = false;
        bool searchingForFirstSegment = true;
        while (searchingForFirstSegment) //Find the first laser segment by searching start to end
        {
            int laserSegEndIndex = findLaserSegEndIndex(startIndex); //Find laser segment from starting point

            if (Mathf.Abs(laserSegEndIndex - startIndex) < 3) //If too small to form a plane the segment is invalid
            {
                startIndex++;
            }
            else
            {
                laserSegmentValid = true; //Is a valid segment
                return laserSegEndIndex;
            }

            if (laserSegEndIndex == Body.Length - 1)
            {
                searchingForFirstSegment = false; //Done searching
                endOfSegmentIsTail = true; //The end of of the segment is the tail

            }
        }

        return -1;
    }

    void DrawLaserWalls()
    {
        /*Head.GetComponent<Renderer>().material.color = Color.grey;
        for (int i = 0; i < Body.Length; ++i)
        {
            //Debug.DrawRay(Body[i].position, transform.position - Body[i].position, Color.blue);
            //Body[i].GetComponent<Renderer>().material.color = Color.grey;
            for (int j = 0; j < Body.Length; ++j)
            {
                //Debug.DrawRay(Body[i].position, Body[j].position - Body[i].position, Color.green);
                
            }
        }*/

        generateLaserMeshses(laserSegmentStartIndeces, laserSegmentEndIndeces);

        /*for (int i = 0; i < laserSegmentStartIndeces.Count; ++i)
        {
            if (laserSegmentStartIndeces[i] == -1)
            {
                Head.GetComponent<Renderer>().material.color = Color.green;
            }
            else
            {
                Body[laserSegmentStartIndeces[i]].GetComponent<Renderer>().material.color = Color.green;
            }
            Body[laserSegmentEndIndeces[i]].GetComponent<Renderer>().material.color = Color.red;
        }
        */
    }

    int findLaserSegEndIndex(int segStartIndex)
    {
        //If we reached the tail of the snake, make that the final laser segment
        //All the setup is assuming there at least 3 points in the curve as you need at least 3 points to make a 2d plane

        //Initialization
        Transform segStart;
        if (segStartIndex == -1)
        {
            segStart = transform;
        }
        else
        {
            segStart = Body[segStartIndex];
        }

        int segIndex = segStartIndex + 1;
        bool foundPlane = false;
        int pointAtPlaneEndIndex = Body.Length - 1;
        float[] planeEquation = Tools.GetPlaneFrom3Points(segStart.position, Body[segIndex].position, Body[pointAtPlaneEndIndex].position);

        do
        {
            if (pointAtPlaneEndIndex - segStartIndex + 1 != 3)
            {
                //Check all points inbetween 3rd point of segment being tested, and the end point
                bool allPointsPassed = true;
                for (int i = segStartIndex + 2; i < pointAtPlaneEndIndex; ++i)
                {
                    Vector3 pointToTest = Body[i].position;
                    if (Tools.GetDistancePointToPlane(pointToTest, planeEquation) > 0.6)
                    {
                        allPointsPassed = false; //Point is not part of the plane
                        i = pointAtPlaneEndIndex;//To break the loop
                    }
                }

                if (allPointsPassed)
                {
                    foundPlane = true;
                }
            }
            else
            {
                foundPlane = true;
            }

            pointAtPlaneEndIndex--;
            planeEquation = Tools.GetPlaneFrom3Points(segStart.position, Body[segIndex].position, Body[pointAtPlaneEndIndex].position);
        } while (!foundPlane);
        pointAtPlaneEndIndex += 1;//Add 1 to account for the last decrement in the do-while loop

        //Debug.Log(pointAtPlaneEndIndex);

       for (int i = pointAtPlaneEndIndex; i >= segIndex; --i)
        {
            RaycastHit hit;
            if (Physics.Raycast(segStart.position, Body[i].position - segStart.position, out hit, 1000, LayerMask.GetMask("RaycastTargetLayer"))){
                
                if (System.Array.IndexOf(Body, hit.collider.transform.parent) == i)
                {
                    return i;
                }
            }
        }
        return segIndex;
    }

    void generateLaserMeshses(List<int> laserSegmentStartIndeces, List<int> laserSegmentEndIndeces)
    {
        //Only add or remove laser meshes when the number of meshes change
        int laserMeshesSegmentsDifference = laserMeshes.Count - laserSegmentStartIndeces.Count;
        if (laserMeshesSegmentsDifference > 0) //If we have more meshes than lasers
        {
            for (int i = 0; i < laserMeshesSegmentsDifference; ++i)
            {
                Destroy(laserMeshes[0]);
                laserMeshes.RemoveAt(0);
            }
        }

        else if (laserMeshesSegmentsDifference < 0) //if we have more lasers than meshes
        {
            for (int i = 0; i < -laserMeshesSegmentsDifference; ++i)
            {
                GameObject laserMesh = new GameObject("laser mesh");
                laserMesh.layer = 11;   
                laserMesh.AddComponent<MeshFilter>();
                laserMesh.AddComponent<MeshRenderer>();
                laserMesh.AddComponent<LaserWallController>();
                laserMesh.transform.SetParent(transform);

                laserMesh.GetComponent<MeshRenderer>().material = LaserWallMaterial;
                laserMesh.GetComponent<Renderer>().material.SetColor("_AlbedoColor", currentLaserWallColor);

                laserMeshes.Add(laserMesh);
            }

        }


        for (int i = 0; i < laserMeshes.Count; ++i)
        {
            Mesh mesh = new Mesh();
            laserMeshes[i].GetComponent<MeshFilter>().mesh = mesh;

            int pointCount = laserSegmentEndIndeces[i] - laserSegmentStartIndeces[i] + 1;
            Vector3[] verticies = new Vector3[pointCount + 1]; //Mesh verticies


            for (int j = 0; j < pointCount; ++j)
            {
                int index = laserSegmentStartIndeces[i] + j;
                if (index == -1)
                {
                    verticies[j] = transform.position;
                }
                else
                {
                    verticies[j] = Body[index].position;
                }

            }

            verticies[pointCount] = (verticies[pointCount - 1] + verticies[0]) / 2; //Middles of the line created by the two end points

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
            mesh.vertices = verticies;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
        }
    }

    public void Die()
    {
        dying = true;
        dyingAnimtionTimeLeft = DyingAnimationLength;
    }

    void updateDieAnimation()
    {
        float timePerBodySegment = DyingAnimationLength / Body.Length;

        int segmentDyingIndex = Body.Length - (int)(dyingAnimtionTimeLeft / timePerBodySegment);

        if (segmentDyingIndex == 0)
        {
            Head.GetComponent<Renderer>().material.color = Color.red;
        }
        else
        {
            try
            {
                Body[segmentDyingIndex - 1].GetComponent<Renderer>().material.color = Color.red;
            } catch
            {
                Debug.Log(segmentDyingIndex - 1);
            }
        }

        dyingAnimtionTimeLeft -= Time.deltaTime * 1000;

        if (dyingAnimtionTimeLeft <= 0)
        {
            Destroy(transform.gameObject);
        }
        
    }
    #endregion  

    #region Movement functions
    void moveHead()
    {
        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");

        //Generate rotation axis as vector based on input
        Vector3 inputVector = new Vector3(-vertical, horizontal, 0);
        Vector3 rotationAxis = inputVector.normalized;
        
        float rotationSpeed = RotationSpeed * inputVector.magnitude * Time.deltaTime;
        
        //Perform rotations
        Quaternion newRotation = Quaternion.AngleAxis(rotationSpeed, rotationAxis); //Rotate head

        //Update rotation data
        transform.rotation *= newRotation;

        //Move based on new rotation data
        Vector3 headMovement = transform.TransformDirection(Vector3.forward) * Speed * Time.deltaTime;
        movementDistance = headMovement.magnitude;
        transform.position += headMovement;
    }

    void moveBody()
    {


        //Update new positions when moved the length of 1 body segment
        if (updateMovementData) //If time to update target positions
        {

            //Move all body segments to the target position
            for (int i = 0; i < Body.Length; ++i)
            {
                Body[i].position = targetBodyPositions[i];
            }

            //Update the target positions and rotations for the next unit of targetUpdateTime
            targetBodyPositions[0] = Body[0].position + ((transform.position - Body[0].position).normalized * currentBodySize);
            targetBodyRotations[0] = transform.rotation;
            for (int i = 1; i < targetBodyPositions.Length; ++i)
            {
                targetBodyRotations[i] = Body[i - 1].rotation;
                targetBodyPositions[i] = Body[i - 1].position;
            }

            //Set last positions and rotations to newly aquired positions
            for (int i = 0; i < lastBodyPositions.Length; ++i)
            {
                lastBodyRotations[i] = Body[i].rotation;
                lastBodyPositions[i] = Body[i].position;
            }

            //Update rotation axes
            for (int i = 0; i < bodyRotationAxes.Length; ++i)
            {
                Quaternion baseRotation = targetBodyRotations[i] * Quaternion.Inverse(Body[i].rotation);
                Vector3 rotationAxis;
                baseRotation.ToAngleAxis(out float angle, out rotationAxis);
                if (angle != 0)
                {
                    bodyRotationAxes[i] = rotationAxis.normalized;
                }
            }
        }


        float timeMovementRatio = timeSinceLastTargetPosUpdate / movementDataUpdateTime; //How far to lerp

        //For all body segments, set position to proper vector lerped between position last update and target position based on percent
        for (int i = 0; i < Body.Length; ++i)
        {
            Body[i].rotation = Quaternion.Lerp(lastBodyRotations[i], targetBodyRotations[i], timeMovementRatio);
            Body[i].position = Vector3.Lerp(lastBodyPositions[i], targetBodyPositions[i], timeMovementRatio);
        }
    }

    #endregion

    #region Initialization funtions
    void initializeHud()
    {
        SnakeScoreText.text = "Score: 0";
        SnakeLengthText.text = "Length: " + CurrentLength;
    }

    void initializePositionData()
    {
        transform.position = SpawnPoint;
    }

    void generateHead()
    {
        Head = Instantiate(SnakeHead).transform;
        Head.SetParent(transform);
        Head.GetComponent<SnakeHeadController>().Init(StartHeadSize);
        Head.GetComponent<Renderer>().material.color = currentSnakeColor;
    }

    void generateBody()
    {
        CurrentLength = StartingLength;

        float segmentSpawnOffset = (StartHeadSize / 2) + (StartBodySize / 2);

        Body = new Transform[StartingLength];
        for (int i = 0; i < StartingLength; ++i)
        {
            GameObject tempBodySeg = Instantiate(BodySegmentPrefab);
            tempBodySeg.GetComponent<Renderer>().material = SnakeBodyMaterial;
            tempBodySeg.GetComponent<Renderer>().material.color = currentSnakeColor;
            float tempSpawnOffset = segmentSpawnOffset + (StartBodySize * i);
            tempBodySeg.transform.position = SpawnPoint + (-Vector3.forward * tempSpawnOffset);
            tempBodySeg.transform.SetParent(transform);
            
            Body[i] = tempBodySeg.transform; 
        }
    }

    void initializeMovementData()
    {

        updateMovementData = false;

        bodyRotationAxes = new Vector3[StartingLength];

        for (int i = 0; i < StartingLength; ++i)
        {
            bodyRotationAxes[i] = Vector3.up;
        }

        //Init positional data for movement
        targetBodyPositions = new Vector3[StartingLength];
        targetBodyPositions[0] = Body[0].position + ((transform.position - Body[0].position).normalized * currentBodySize);
        for (int i = 1; i < StartingLength; i++)
        {
            targetBodyPositions[i] = Body[i - 1].position;
        }

        lastBodyPositions = new Vector3[StartingLength];
        for (int i = 0; i < lastBodyPositions.Length; ++i)
        {
            lastBodyPositions[i] = Body[i].position;
        }

        //Init rotational data for movement
        targetBodyRotations = new Quaternion[StartingLength];
        targetBodyRotations[0] = transform.rotation;
        for (int i = 1; i < Body.Length; ++i)
        {
            targetBodyRotations[i] = Body[i - 1].rotation;
        }

        lastBodyRotations = new Quaternion[StartingLength];
        for (int i = 0; i < Body.Length; ++i)
        {
            lastBodyRotations[i] = Body[i].rotation;
        }
    }

    void initializeGameplayData()
    {
        currentHeadSize = StartHeadSize;
        currentBodySize = StartBodySize;
        scoreLastGrow = 0;
    }

    void initializeCameraData()
    {
        Camera = GameObject.Find("Main Camera").transform;
        Camera.GetComponent<CameraController>().SnakeObject = transform;
    }
    #endregion
}