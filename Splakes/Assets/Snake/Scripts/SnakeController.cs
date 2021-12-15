using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Topology;
using TriangleNet;

public class SnakeController : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback
{
    #region Generation data

    //Generation parameters
    public int StartingLength;
    public float StartHeadSize;
    public float StartBodySize;
    #endregion

    #region Visual data
    public Material SnakeBodyMaterial;
    public Material LaserWallMaterial;
    public Color[] SnakeColors;
    private Color currentSnakeColor;
    public Color[] LaserWallColors;
    private Color currentLaserWallColor;


    public TextMeshProUGUI SnakeScoreText;
    public TextMeshProUGUI SnakeLengthText;
    #endregion

    #region Body data
    //Public body data for interactions
    [HideInInspector]
    public Transform Head;
    [HideInInspector]
    public List<Transform> Body;

    //Body data tracking
    [HideInInspector]
    public int CurrentLength;

    //Laser wall data
    private List<int> laserSegmentStartIndices;
    private List<int> laserSegmentEndIndices;
    private List<GameObject> laserMeshes;
    #endregion

    #region Movement data
    //Movement parameters
    public float Speed;
    public float RotationSpeed;
    public int TurnThreshold;

    //Update timing
    private float movementTargetUpdateRate;
    private float timeSinceLastTargetPosUpdate;

    //Movement storage
    private List<Vector3> bodyRotationAxes;
    private List<Vector3> lastBodyPositions; //Parallel with Body and targetBodyPositions
    private List<Vector3> targetBodyPositions; //Parallel with Body and lasyBodyPositions
    private List<Quaternion> lastBodyRotations;
    private List<Quaternion> targetBodyRotations;


    #endregion

    #region Camera data
    //Main Camera data
    public Transform Camera;
    #endregion

    #region Gameplay data
    //Score data
    public int Score;

    //Growing parameters
    public int PointsForNewSegment;

    //Grow control data
    private int scoreLastGrow;
    private float currentHeadSize;
    private float currentBodySize;

    public float SegmentDeathStartDelay = 0.25f;
    public float SegmentDeathLingerTime = 1f;

    [HideInInspector]
    public bool Dying = false;

    [HideInInspector]
    public SpawnSnakes SnakeSpawner;
    public GenerateFood FoodSpawner;
    #endregion

    #region Multiplayer Data
    //Owner data
    public Player Owner;
    #endregion

    private GameObject[] midpointSpheres;

    // Start is called before the first frame update
    void Start()
    {
        midpointSpheres = new GameObject[20];
        for (int i = 0; i < 20; ++i)
        {
            midpointSpheres[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            midpointSpheres[i].transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        }

        //Get the food spawner
        FoodSpawner = GameObject.Find("FoodGenerator").GetComponent<GenerateFood>();

        //Initialize laser wall lists
        laserMeshes = new List<GameObject>();
        laserSegmentStartIndices = new List<int>();
        laserSegmentEndIndices = new List<int>();

        //Randomly select snake color
        currentSnakeColor = SnakeColors[UnityEngine.Random.Range(0, SnakeColors.Length)];
        currentLaserWallColor = LaserWallColors[UnityEngine.Random.Range(0, LaserWallColors.Length)];

        //Instantiate the body objects and body control structures
        if (PhotonNetwork.LocalPlayer == Owner)
        {
            generateHead();
            generateBody();
            initializeGameplayData();
            initializeMovementData();
            initializeHud();
            initializeCameraData();
        }
        else
        {
            RequestSyncWithOwner();
        }
    }

    // Update is called once per frame
    void Update()
    {
        bool updateTargets = false;

        //We dont want to update any targets if we are dying
        if (!Dying)
        {
            //Update loop to run once the snake has moved the distance equivilent to the size of 1 body segment
            timeSinceLastTargetPosUpdate += Time.deltaTime;
            if (timeSinceLastTargetPosUpdate >= 1 / Speed) //If time to update target positions
            {
                timeSinceLastTargetPosUpdate -= movementTargetUpdateRate;

                updateTargets = true;
            }

            if (Owner == PhotonNetwork.LocalPlayer)
            {
                ownerUpdate(updateTargets);
            }
            else
            {
                nonOwnerUpdate();
            }
        }
        else
        {
            if (Owner == PhotonNetwork.LocalPlayer)
            {
                bool doneDying = true;

                if (Head != null)
                {
                    doneDying = false;
                }

                for (int i = 0; i < Body.Count; ++i)
                {
                    if (Body[i] != null)
                    {
                        doneDying = false;
                    }
                }

                if (doneDying)
                {
                    SnakeSpawner.RespawnSnake();
                }
            }
        }
    }



    private void ownerUpdate(bool updateTargets)
    {
        if (updateTargets)
        {
            updateBodyTargets();
            UpdateLaserWalls(); //Only owner finds new laser walls
            updateOtherClientsLaserMeshes(); //Send new laser wall data to other clients
        }

        moveHead();
        moveBody();
        generateLaserMeshes();
    }

    private void nonOwnerUpdate()
    {
        if (Body.Count > 0)
        {
            generateLaserMeshes();
        }
    }

    #region Multiplayer functions
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        Owner = info.Sender;

        //Add all snakes not instantiated by the local client to the SnakeSpawner list of snakes
        if (Owner != PhotonNetwork.LocalPlayer)
        {
            SnakeSpawner = GameObject.Find("SnakeSpawner").GetComponent<SpawnSnakes>();
            SnakeSpawner.AddSnake(this);
        }
    }


    //Synchronization functions
    public void RequestSyncWithOwner()
    {
        photonView.RPC("syncWithPlayer", Owner);
    }

    //Send snake data from the Owner client to the client requesting synchronization
    [PunRPC]
    private void syncWithPlayer(PhotonMessageInfo info)
    {
        object[] syncList = new object[Body.Count + 3];

        syncList[0] = getSnakeBodyColor();
        syncList[1] = getSnakeLaserColor();
        syncList[2] = Head.GetComponent<PhotonView>().ViewID;
        for (int i = 0; i < Body.Count; ++i)
        {
            syncList[i + 3] = Body[i].GetComponent<PhotonView>().ViewID;
        }


        photonView.RPC("syncOurselvesWithOwner", info.Sender, (object)syncList);
    }

    //Recieve Owner client snake infomation and sync local client belonging to the Owner with it.
    [PunRPC]
    private void syncOurselvesWithOwner(object syncList, PhotonMessageInfo info)
    {
        object[] syncListArray = (object[])syncList;

        if (info.Sender == Owner)
        {
            
            Head = PhotonView.Find((int)syncListArray[2]).transform;

            CurrentLength = 0;
            for (int i = 3; i < syncListArray.Length; ++i)
            {
                Body.Add(PhotonView.Find((int)syncListArray[i]).transform);
                CurrentLength += 1;
            }
            
            syncSnakeColors((Vector3)syncListArray[0], (Vector3)syncListArray[1]);
        }
    }

    void syncSnakeColors(Vector3 bodyColor, Vector3 laserColor)
    {
        currentSnakeColor = new Color(bodyColor.x, bodyColor.y, bodyColor.z);

        Head.GetComponent<Renderer>().material.color = currentSnakeColor;
        for (int i = 0; i < Body.Count; ++i)
        {
            Body[i].GetComponent<Renderer>().material.color = currentSnakeColor;
        }


        currentLaserWallColor = new Color(laserColor.x, laserColor.y, laserColor.z);
    }



    //Body data fetching
    private Vector3 getSnakeBodyColor()
    {
        return new Vector3(currentSnakeColor.r, currentSnakeColor.g, currentSnakeColor.b);
    }

    private Vector3 getSnakeLaserColor()
    {
        return new Vector3(currentLaserWallColor.r, currentLaserWallColor.g, currentLaserWallColor.b);
    }


    //Owner to client realtime synchronization functions
    private void updateOtherClientsLaserMeshes()
    {
        object laserMeshSyncList = new object[]
        {
            laserSegmentStartIndices.ToArray(),
            laserSegmentEndIndices.ToArray()
        };

        photonView.RPC("updateClientLaserMeshesFromOwner", RpcTarget.Others, laserMeshSyncList);
    }

    [PunRPC]
    private void updateClientLaserMeshesFromOwner(object laserMeshSyncList)
    {
        object[] objArray = (object[])laserMeshSyncList;

        laserSegmentStartIndices = new List<int>();
        laserSegmentStartIndices.AddRange((int[])objArray[0]);

        laserSegmentEndIndices = new List<int>();
        laserSegmentEndIndices.AddRange((int[])objArray[1]);
    }
    #endregion

    #region Gameplay functions
    public void FeedSnake(int points)
    {
        Score += points;
        updateSize();
        updateHUD();
    }

    private void updateHUD()
    {
        if (Owner == PhotonNetwork.LocalPlayer)
        {
            SnakeScoreText.text = "Score: " + Score;
            SnakeLengthText.text = "Length: " + CurrentLength;
        }
    }


    private void updateSize()
    {
        //Add new segment if condition is met
        if (Score - scoreLastGrow >= PointsForNewSegment)
        {
            growNewSegment();

            //Update condition data
            scoreLastGrow = Score;
        }
    }

    private void growNewSegment()
    {
        //Initialize new segment
        Transform newSegment = PhotonNetwork.Instantiate("SnakeBodySeg", Body[CurrentLength - 1].position + Body[CurrentLength - 1].TransformDirection(-Vector3.forward) * currentBodySize, Quaternion.identity).transform;
        newSegment.GetComponent<Renderer>().material.color = currentSnakeColor;
        newSegment.GetComponent<SnakeBodyController>().Init(Owner, this, true);

        //Resize all arrays and and new segment data
        Body.Add(newSegment);
        bodyRotationAxes.Add(bodyRotationAxes[CurrentLength - 1]);
        lastBodyPositions.Add(lastBodyPositions[CurrentLength - 1]);
        targetBodyPositions.Add(targetBodyPositions[CurrentLength - 1]);
        lastBodyRotations.Add(lastBodyRotations[CurrentLength - 1]);
        targetBodyRotations.Add(targetBodyRotations[CurrentLength - 1]);

        //Increase the current length by one
        CurrentLength += 1;

        photonView.RPC("addSegment", RpcTarget.Others, newSegment.GetComponent<PhotonView>().ViewID);
    }

    [PunRPC]
    private void addSegment(int newSegViewID)
    {
        Transform newSegment = PhotonView.Find(newSegViewID).transform;
        newSegment.GetComponent<Renderer>().material.color = currentSnakeColor;
        newSegment.GetComponent<SnakeBodyController>().Init(Owner, this, true);
        Body.Add(newSegment);
        CurrentLength += 1;
    }

    private void removeSegment(int segmentIndex)
    {
        Body.RemoveAt(segmentIndex);

        if (PhotonNetwork.LocalPlayer == Owner)
        {
            bodyRotationAxes.RemoveAt(segmentIndex);
            lastBodyPositions.RemoveAt(segmentIndex);
            targetBodyPositions.RemoveAt(segmentIndex);
            lastBodyRotations.RemoveAt(segmentIndex);
            targetBodyRotations.RemoveAt(segmentIndex);
        }

        CurrentLength -= 1;
        Score -= PointsForNewSegment;
    }

    private void UpdateLaserWalls()
    {
        laserSegmentStartIndices = new List<int>(); //Holds the Body index of laser segment start
        laserSegmentEndIndices = new List<int>(); //Holds the Body index of laser segment end

        int startIndex = -1; //start at head which is basically Body[-1]
        bool firstLaserSegmentValid = false;
        bool endOfFirstSegmentIsTail = false;
        int firstLaserSegEndIndex = findLaserSegment(startIndex, out firstLaserSegmentValid, out endOfFirstSegmentIsTail);

        if (firstLaserSegmentValid) //If a valid first segment is even found
        {
            laserSegmentStartIndices.Add(startIndex); //Add the start index
            laserSegmentEndIndices.Add(firstLaserSegEndIndex); //Add the end index

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
                                    laserSegmentStartIndices.Add(bestSegmentStartIndex); //Add the start index
                                    laserSegmentEndIndices.Add(bestSegmentEndIndex); //Add the end index
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
                                laserSegmentStartIndices.Add(currentStartIndex); //Add the start index
                                laserSegmentEndIndices.Add(currentEndIndex); //Add the end index
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

            if (laserSegEndIndex == Body.Count - 1)
            {
                searchingForFirstSegment = false; //Done searching
                endOfSegmentIsTail = true; //The end of of the segment is the tail

            }
        }

        return -1;
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
        int pointAtPlaneEndIndex = Body.Count - 1;
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
                    if (Tools.GetDistancePointToPlane(pointToTest, planeEquation) > 0.3)
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


       for (int i = pointAtPlaneEndIndex; i >= segIndex; --i)
        {
            RaycastHit hit;
            if (Physics.Raycast(segStart.position, Body[i].position - segStart.position, out hit, 1000, LayerMask.GetMask("RaycastTargetLayer"))){
                
                
                if (Body.IndexOf(hit.collider.transform.parent) == i)
                {
                    return i;
                }
            }
        }
        return segIndex;
    }

    void generateLaserMeshes()
    {
        //Only add or remove laser meshes when the number of meshes change
        int laserMeshesSegmentsDifference = laserMeshes.Count - laserSegmentStartIndices.Count;
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

                laserMesh.GetComponent<MeshRenderer>().material = LaserWallMaterial;
                laserMesh.GetComponent<Renderer>().material.SetColor("_AlbedoColor", currentLaserWallColor);

                if (Owner != PhotonNetwork.LocalPlayer)
                {
                    laserMesh.AddComponent<MeshCollider>();
                }

                laserMeshes.Add(laserMesh);
            }

        }


        for (int i = 0; i < laserMeshes.Count; ++i)
        {
            laserMeshes[i].SetActive(true);

            UnityEngine.Mesh mesh = new UnityEngine.Mesh();
            laserMeshes[i].GetComponent<MeshFilter>().mesh = mesh;

            int endIndex = laserSegmentEndIndices[i];
            int startIndex = laserSegmentStartIndices[i];


            int pointCount = endIndex - startIndex + 1;
            Vector3[] verticies = new Vector3[pointCount];// + 1]; //Mesh verticies


            for (int j = 0; j < pointCount; ++j)
            {
                int index = laserSegmentStartIndices[i] + j;
                if (index == -1)
                {
                    verticies[j] = transform.position;
                }
                else
                {
                    verticies[j] = Body[index].position;
                }
            }




            //Get mesh plane for 3D to 2D mapping
            float[] planeEq = Tools.GetPlaneFrom3Points(verticies[0], verticies[verticies.Length - 2], verticies[verticies.Length - 1]);
            Vector3 planeNormal = Tools.GetPlaneNormal(planeEq);

            //Put all the vertices on the same plane
            for (int j = 0; j < verticies.Length; ++j)
            {
                //verticies[j] += planeNormal * Tools.GetSignedDistancePointToPlane(verticies[j], planeEq);
            } 
            
            //Map the points to a local 2D plane
            Vector2[] mappedPoints = Tools.Map3Dto2D(verticies, planeNormal);

            //Create polygon for triangulation
            Polygon poly = new Polygon();
            for (int j = 0; j < mappedPoints.Length; ++j)
            {
                poly.Add(new Vertex(mappedPoints[j].x, mappedPoints[j].y));
            }

            //Triangulate
            ConstraintOptions options = new ConstraintOptions();
            TriangleNet.Mesh tempMesh = (TriangleNet.Mesh)poly.Triangulate(options);

            List<int> triangles = new List<int>();

            //Iterate through triangulated mesh triangles
            IEnumerator<Triangle> triangleEnumerator = tempMesh.triangles.GetEnumerator();

            while (triangleEnumerator.MoveNext())
            {
                //Get the current triangle
                Triangle current = triangleEnumerator.Current;

                //Vertex indices
                int v2Index = current.vertices[2].id;
                int v1Index = current.vertices[1].id;
                int v0Index = current.vertices[0].id;

                //Add the triangles to the the laser mesh triangles list
                triangles.Add(v2Index);
                triangles.Add(v1Index);
                triangles.Add(v0Index);
            }



            mesh.Clear();
            mesh.vertices = verticies;
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();

            if (Owner != PhotonNetwork.LocalPlayer)
            {
                try
                {
                    laserMeshes[i].GetComponent<MeshCollider>().sharedMesh = mesh;
                    laserMeshes[i].GetComponent<MeshCollider>().convex = true;
                }
                catch
                {
                    laserMeshes[i].GetComponent<MeshCollider>().convex = false;
                }
            }
        }
    }

    public void Die(bool throwDeathEvent)
    {
        //Is true only when called by a SnakeBodyController, otherwise when called by the snake spawner event handler, it is false
        if (throwDeathEvent)
        {
            SnakeSpawner.SnakeDied();
        }

        Dying = true;

        Head.GetComponent<SnakeBodyController>().Die(0, SegmentDeathLingerTime);

        for (int i = 0; i < Body.Count; ++i)
        {
            Body[i].GetComponent<SnakeBodyController>().Die(SegmentDeathStartDelay * (i + 1), SegmentDeathLingerTime);
        }

        for (int i = 0; i < laserMeshes.Count; ++i)
        {
            laserMeshes[i].SetActive(false);
        }
    }

    public void Hurt(Transform bodySegmentHurt)
    {
        int hurtStartIndex = Body.IndexOf(bodySegmentHurt);
        SnakeSpawner.SnakeHurt(hurtStartIndex);
        Hurt(hurtStartIndex);
    }

    public void Hurt(int hurtStartIndex)
    {
        float delayCount = 0;

        int segmentsRemoved = CurrentLength - hurtStartIndex;

        for (int i = 0; i < segmentsRemoved; ++i)
        {
            Body[hurtStartIndex].GetComponent<SnakeBodyController>().Die(SegmentDeathStartDelay * delayCount, SegmentDeathLingerTime);
            removeSegment(hurtStartIndex);
            delayCount++;
        }

        for (int i = 0; i < laserMeshes.Count; ++i)
        {
            laserMeshes[i].SetActive(false);
        }
    }

    #endregion  

    #region Movement functions
    void moveHead()
    {
        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");

        Vector3 inputVector = new Vector3(-vertical, horizontal, 0);

        //Determine the axis of rotation for the transfrom based on input
        Vector3 rotationAxis = inputVector.normalized;
        
        float rotationSpeed = RotationSpeed * inputVector.magnitude * Time.deltaTime;
        
        //Perform rotation
        Quaternion newRotation = Quaternion.AngleAxis(rotationSpeed, rotationAxis); //Rotate head

        //Update rotation data
        transform.rotation *= newRotation;
        Head.rotation *= newRotation;

        //Move based on new rotation data
        Vector3 headMovement = transform.TransformDirection(Vector3.forward) * Speed * Time.deltaTime;
        transform.position += headMovement;
        Head.position += headMovement;

    }

    private void updateBodyTargets()
    {
        //Move all body segments to the target position
        for (int i = 0; i < Body.Count; ++i)
        {
            Body[i].position = targetBodyPositions[i];
        }

        //Update the target positions and rotations for the next unit of targetUpdateTime
        targetBodyPositions[0] = Body[0].position + ((transform.position - Body[0].position).normalized * currentBodySize);
        targetBodyRotations[0] = transform.rotation;
        for (int i = 1; i < targetBodyPositions.Count; ++i)
        {
            targetBodyRotations[i] = Body[i - 1].rotation;
            targetBodyPositions[i] = Body[i - 1].position;
        }

        //Set last positions and rotations to newly aquired positions
        for (int i = 0; i < lastBodyPositions.Count; ++i)
        {
            lastBodyRotations[i] = Body[i].rotation;
            lastBodyPositions[i] = Body[i].position;
        }

        //Update rotation axes
        for (int i = 0; i < bodyRotationAxes.Count; ++i)
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

    void moveBody()
    {
        float timeMovementRatio = timeSinceLastTargetPosUpdate / movementTargetUpdateRate; //How far to lerp

        //For all body segments, set position to proper vector lerped between position last update and target position based on percent
        for (int i = 0; i < Body.Count; ++i)
        {
            Vector3 pos = Vector3.Lerp(lastBodyPositions[i], targetBodyPositions[i], timeMovementRatio);
            Quaternion rot = Quaternion.Lerp(lastBodyRotations[i], targetBodyRotations[i], timeMovementRatio);

            Body[i].GetComponent<SnakeBodyController>().Move(pos, rot);
        }
    }

    #endregion

    #region Initialization funtions
    void initializeHud()
    {
        //Only need the HUD for the local player score information
        if (Owner == PhotonNetwork.LocalPlayer)
        {
            SnakeScoreText.text = "Score: " + Score;
            SnakeLengthText.text = "Length: " + CurrentLength;
        }
    }

    void generateHead()
    {
        Head = PhotonNetwork.Instantiate("SnakeHead", transform.position, Quaternion.identity).transform;
        Head.GetComponent<SnakeBodyController>().Init(Owner, this, true);
        Head.GetComponent<Renderer>().material.color = currentSnakeColor;
    }

    void generateBody()
    {
        CurrentLength = StartingLength;

        //Offset position control
        float segmentSpawnOffset = (StartHeadSize / 2) + (StartBodySize / 2);

        Body = new List<Transform>();

        //Generate a new game object for each body segment as many times as the 
        for (int i = 0; i < StartingLength; ++i)
        {
            float tempSpawnOffset = segmentSpawnOffset + (StartBodySize * i);

            GameObject tempBodySeg = PhotonNetwork.Instantiate("SnakeBodySeg", transform.position + (-Vector3.forward * tempSpawnOffset), Quaternion.identity);
            tempBodySeg.GetComponent<Renderer>().material = SnakeBodyMaterial;
            tempBodySeg.GetComponent<Renderer>().material.color = currentSnakeColor;

            tempBodySeg.GetComponent<SnakeBodyController>().Init(Owner, this, false);
            
            Body.Add(tempBodySeg.transform);

            Score += PointsForNewSegment;
            scoreLastGrow = Score;
        }
    }

    void initializeMovementData()
    {
        //Update rate corresponds to the snake moving 1 body segment length in distance
        //Body size is always 1 for now
        movementTargetUpdateRate = 1 / Speed;

        //Initialize body position control structure
        targetBodyPositions = new List<Vector3>();
        targetBodyPositions.Add(Body[0].position + ((transform.position - Body[0].position).normalized * currentBodySize));
        for (int i = 1; i < StartingLength; i++)
        {
            targetBodyPositions.Add(Body[i - 1].position);
        }

        lastBodyPositions = new List<Vector3>();
        for (int i = 0; i < StartingLength; ++i)
        {
            lastBodyPositions.Add(Body[i].position);
        }

        //Initialize body rotation control structure
        bodyRotationAxes = new List<Vector3>();
        for (int i = 0; i < StartingLength; ++i)
        {
            bodyRotationAxes.Add(Vector3.up);
        }

        targetBodyRotations = new List<Quaternion>();
        targetBodyRotations.Add(transform.rotation);
        for (int i = 1; i < Body.Count; ++i)
        {
            targetBodyRotations.Add(Body[i - 1].rotation);
        }

        lastBodyRotations = new List<Quaternion>();
        for (int i = 0; i < Body.Count; ++i)
        {
            lastBodyRotations.Add(Body[i].rotation);
        }
    }

    void initializeGameplayData()
    {
        //Initialize size control structure
        currentHeadSize = StartHeadSize;
        currentBodySize = StartBodySize;
        scoreLastGrow = 0;
    }

    void initializeCameraData()
    {
        //Attach the main camera to this snake
        if (Owner == PhotonNetwork.LocalPlayer)
        {
            Camera = GameObject.Find("Main Camera").transform;
            Camera.GetComponent<CameraController>().SnakeObject = transform;
        }
    }
    #endregion

    void OnDestroy()
    {
        for (int i = 0; i < Body.Count; ++i)
        {
            try
            {
                Destroy(Body[i].gameObject);
            } catch { }
        }

        for (int i = 0; i < laserMeshes.Count; ++i)
        {
            Destroy(laserMeshes[i].gameObject);
        }
    }
}