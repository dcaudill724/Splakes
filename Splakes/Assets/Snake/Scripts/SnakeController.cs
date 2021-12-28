using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System;

public class SnakeController : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback
{
    #region Generation data
    public int StartingLength;
    #endregion

    #region Visual data
    public Material SnakeBodyBaseMaterial;
    private Material sharedSnakeBodyMaterial;

    public Color[] SnakeColors;
    private Color currentSnakeColor;
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

    //Laser wall
    private LaserWallController laserWallController;
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

    //Death data
    public float SegmentDeathStartDelay = 0.25f;
    public float SegmentDeathLingerTime = 1f;

    [HideInInspector]
    public bool Dying = false;

    //Bounds of snake
    private Vector3[] bounds;
    #endregion

    #region Multiplayer Data
    public Player Owner;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        //Randomly select snake color
        currentSnakeColor = SnakeColors[UnityEngine.Random.Range(0, SnakeColors.Length)];

        //Local player instantiates their own snake
        if (PhotonNetwork.LocalPlayer == Owner)
        {
            generateHead();
            generateBody();
            initializeGameplayData();
            initializeMovementData();
            initializeCameraData();

            laserWallController = GetComponent<LaserWallController>();
            laserWallController.Init(ref Body, true);
        }

        //Snakes owned by other players request the other player for synchronization
        else
        {
            RequestSyncWithOwner();
        }

        
    }

    // Update is called once per frame
    void Update()
    {
        //Only update movement when not dying
        if (!Dying)
        {
            if (Owner == PhotonNetwork.LocalPlayer)
            {
                ownerUpdate(checkTargetUpdate());
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
                if (checkDoneDying())
                {
                    EasyEventSystem.RaiseLocalEvent("snake finished dying");
                }
            }
        }
    }

    private bool checkTargetUpdate()
    {
        timeSinceLastTargetPosUpdate += Time.deltaTime;
        if (timeSinceLastTargetPosUpdate >= 1 / Speed) //If time to update target positions
        {
            timeSinceLastTargetPosUpdate -= movementTargetUpdateRate;

            return true;
        }

        return false;
    }

    private void ownerUpdate(bool updateTargets)
    {
        if (updateTargets)
        {
            updateBodyTargets();
            updateOtherClientsLaserMeshes(); //Send new laser wall data to other clients
        }
        moveHead();
        moveBody();
        updateBounds();
    }

    private void nonOwnerUpdate()
    {
        updateBounds();
    }
   
    private bool checkDoneDying()
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

        return doneDying;
    }
    
    #region Multiplayer functions
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        Owner = info.Sender;

        //Add all snakes not instantiated by the local client to the SnakeSpawner list of snakes
        if (Owner != PhotonNetwork.LocalPlayer)
        {
            EasyEventSystem.RaiseLocalEvent("unowned snake instantiated", this);
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

        laserWallController = GetComponent<LaserWallController>();
        laserWallController.Init(ref Body, false);
    }

    void syncSnakeColors(Vector3 bodyColor, Vector3 laserColor)
    {
        currentSnakeColor = new Color(bodyColor.x, bodyColor.y, bodyColor.z);

        sharedSnakeBodyMaterial = new Material(SnakeBodyBaseMaterial);
        sharedSnakeBodyMaterial.color = currentSnakeColor;

        Head.GetComponent<Renderer>().sharedMaterial = sharedSnakeBodyMaterial;
        for (int i = 0; i < Body.Count; ++i)
        {
            Body[i].GetComponent<Renderer>().sharedMaterial = sharedSnakeBodyMaterial;
        }

        //currentLaserWallColor = new Color(laserColor.x, laserColor.y, laserColor.z);
    }



    //Body data fetching
    private Vector3 getSnakeBodyColor()
    {
        return new Vector3(currentSnakeColor.r, currentSnakeColor.g, currentSnakeColor.b);
    }

    private Vector3 getSnakeLaserColor()
    {
        return Vector3.zero;//(currentLaserWallColor.r, currentLaserWallColor.g, currentLaserWallColor.b);
    }



    //Owner to client realtime synchronization functions
    private void updateOtherClientsLaserMeshes()
    {
        photonView.RPC("updateClientLaserMeshesFromOwner", RpcTarget.Others, laserWallController.GetIndicesForSync());
    }

    [PunRPC]
    private void updateClientLaserMeshesFromOwner(object laserMeshSyncList)
    {
        GetComponent<LaserWallController>().SetIndicesForSync(laserMeshSyncList);
    }
    #endregion

    #region Gameplay functions
    public void FeedSnake(int points)
    {
        Score += points;
        updateSize();
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
        newSegment.GetComponent<Renderer>().sharedMaterial = sharedSnakeBodyMaterial;
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
        newSegment.GetComponent<Renderer>().sharedMaterial = sharedSnakeBodyMaterial;
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



    public Vector3[] GetBounds()
    {
        return bounds;
    }

    

    private void updateBounds()
    {
        Vector3[] points = new Vector3[Body.Count + 1];
        points[0] = transform.position;
        for (int i = 0; i < Body.Count; ++i)
        {
            points[i + 1] = Body[i].position;
        }

        Vector3[] tempBounds = Tools.GetBounds(points);

        Vector3 tempMinBounds = tempBounds[0] + (tempBounds[0] - tempBounds[1]).normalized * 1.5f;
        Vector3 tempMaxBounds = tempBounds[1] + (tempBounds[1] - tempBounds[0]).normalized * 1.5f;

        bounds = new Vector3[] { tempMinBounds, tempMaxBounds };
    }

    public void CheckBBCollision(SnakeController otherSnake)
    {
        try
        {
            if (Tools.BoundingBoxCollision(bounds, otherSnake.GetBounds()))
            {
                otherSnake.CheckLaserWallCollision(this);
            }
        }
        catch { }
    }

    public void CheckLaserWallCollision(SnakeController otherSnake)
    {
        laserWallController.CheckCollision(otherSnake);
    }



   

    public void LocalStartDying()
    {
        EasyEventSystem.RaiseNetworkEvent(SnakeEvents.SnakeStartedDying);
        StartDying();
    }

    public void StartDying()
    {
        Dying = true;

        Head.GetComponent<SnakeBodyController>().Die(0, SegmentDeathLingerTime);

        for (int i = 0; i < Body.Count; ++i)
        {
            Body[i].GetComponent<SnakeBodyController>().Die(SegmentDeathStartDelay * (i + 1), SegmentDeathLingerTime);
        }

        laserWallController.IsActive = false;
    }

    public void LocalHurt(Transform bodySegmentHurt)
    {
        int hurtStartIndex = Body.IndexOf(bodySegmentHurt);

        EasyEventSystem.RaiseNetworkEvent(SnakeEvents.SnakeHurt, hurtStartIndex);
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
    }

    #endregion  

    #region Movement functions
    void moveHead()
    {
        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");

        Vector3 inputVector = new Vector3(-vertical, horizontal, 0);
        if (PhotonNetwork.LocalPlayer != PhotonNetwork.MasterClient)
        {
            inputVector = new Vector3(-1, 0, 0);
        }

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
    void generateHead()
    {
        Head = PhotonNetwork.Instantiate("SnakeHead", transform.position, Quaternion.identity).transform;
        Head.GetComponent<SnakeBodyController>().Init(Owner, this, true);
        Head.GetComponent<Renderer>().material.color = currentSnakeColor;
    }

    void generateBody()
    {
        CurrentLength = StartingLength;
        currentBodySize = 1;
        currentHeadSize = 1.5f;

        //Offset position control
        float segmentSpawnOffset = (1.5f / 2f) + (1f / 2f);

        Body = new List<Transform>();

        sharedSnakeBodyMaterial = new Material(SnakeBodyBaseMaterial);
        sharedSnakeBodyMaterial.color = currentSnakeColor;

        //Generate a new game object for each body segment as many times as the 
        for (int i = 0; i < StartingLength; ++i)
        {
            float tempSpawnOffset = segmentSpawnOffset + i;

            GameObject tempBodySeg = PhotonNetwork.Instantiate("SnakeBodySeg", transform.position + (-Vector3.forward * tempSpawnOffset), Quaternion.identity);
            tempBodySeg.GetComponent<Renderer>().sharedMaterial = sharedSnakeBodyMaterial;

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
        Destroy(laserWallController);
        Destroy(sharedSnakeBodyMaterial);
    }
}