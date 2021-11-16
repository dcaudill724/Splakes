using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnakeController : MonoBehaviour
{

    #region Generation data
    public Vector3 SpawnPoint;
    public int StartingLength;
    public float Speed;
    public float RotationSpeed;
    public GameObject SnakeHead;
    public float LaserWidth;
    public float StartHeadSize;
    public float StartBodySize;
    #endregion

    #region Body data
    //Public body data for interactions
    [HideInInspector]
    public Transform Head;
    [HideInInspector]
    public Transform[] Body; //Bruh, List<Transform> causes huge frame drops when adding body segments in generateBody. Therefore I sadly use array. Also is parallel with BodyMovement
    [HideInInspector]
    public Transform DirectionLaser;

    public int MaxDirectionLaserDistance;
    public int TurnThreshold;
    #endregion

    #region Movement data
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
    public float GrowthMultipler;
    public float SpeedScoreMultipler;
    public int PointsForNewSegment;

    private int scoreLastGrow;
    private float currentHeadSize;
    private float currentBodySize;

    Vector3 LastRotationAxis;
    #endregion

    private LineRenderer lr; //Debugging
    private Transform testPoint;

    // Start is called before the first frame update
    void Start()
    {
        generateHead();
        initializePositionData();
        generateBody();
        initializeGameplayData();
        initializeMovementData();
        initializeCameraData();

        lr = GetComponent<LineRenderer>();
        lr.positionCount = 2;

        //testPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
    }

    // Update is called once per frame
    void Update()
    {
        moveHead();
        moveBody();
        DrawLaserWalls();

    }

    #region Gameplay functions
    public void FeedSnake(int points)
    {
        Score += points;
        float newSize = Score * GrowthMultipler;
        Speed = Speed + (Score * SpeedScoreMultipler);
        updateSize(newSize);
    }

    void updateSize(float newSize)
    {
        /*
        //Update sizes
        currentHeadSize = StartHeadSize * newSize;
        currentBodySize = StartBodySize * newSize;

        //Add new segment if condition is met
        if (Score - scoreLastGrow >= PointsForNewSegment)
        {
            int newSegments = (Score - scoreLastGrow) / PointsForNewSegment;
            Transform[] newBody = new Transform[Body.Length + newSegments]; //Init new body array
            Vector3[] newBodyMovement = new Vector3[bodyMovement.Length + newSegments]; //Init new movement array

            //Copy body to new body and movement to new movement
            for (int i = 0; i < Body.Length; ++i)
            {
                newBody[i] = Body[i];
                newBodyMovement[i] = bodyMovement[i];
            }

            //Add new segments and new movements
            for (int i = 0; i < newSegments; ++i) {
                newBodyMovement[bodyMovement.Length + i] = bodyMovement[bodyMovement.Length - 1];
                newBody[Body.Length + i] = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            }

            //Update body and movement
            bodyMovement = newBodyMovement;
            Body = newBody;

            //Update condition data
            scoreLastGrow = Score / PointsForNewSegment;
        }

        //Change head size
        Head.localScale = new Vector3(currentHeadSize, currentHeadSize, currentHeadSize);

        //Change body segment size
        for (int i = 0; i < Body.Length; ++i)
        {
            Body[i].localScale = new Vector3(currentBodySize, currentBodySize, currentBodySize);
        }

        //Change first body segment position since it relies on head position
        Body[0].transform.position = transform.position + (-bodyMovement[0] * ((currentHeadSize / 2) + currentBodySize / 2));

        //Change the rest of the segment positions
        for (int i = 1; i < Body.Length; ++i)
        {
            Body[i].transform.position = Body[i - 1].transform.position + (-bodyMovement[i] * currentBodySize);
        }

        */

    }

    void DrawLaserWalls()
    {
        //Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.right) * 10, Color.magenta);
        for (int i = 0; i < Body.Length; ++i)
        {
            Body[i].GetComponent<Renderer>().material.color = Color.grey;
            //Debug.DrawRay(Body[i].position, transform.position - Body[i].position, Color.green);
        }

        bool showOutput = false;
        if (Input.GetKeyUp("space"))
        {
            showOutput = true;
        }

        List<Transform> laserNodes = new List<Transform>();
        laserNodes.Add(transform);

        bool finishedFindingNodes = false;
        int segIndex = 0; //Start at first bodySegment

        while (!finishedFindingNodes)
        {
            //All the setup is assuming there at least 3 points in the curve as you need at least 3 points to make a 2d plane

            //Initialization
            float angleSum = 0; //Sum of all angles between body segments from previous laser node to current body segment
            int n = 3; //n = number of verticies in polygon being made. angleSum for concave shape is (n - 2)180

            Transform prevSegment = laserNodes[0]; //Starting with the first laser node which is the head
            bool laserSegEndFound = false;


            //Get intital rotation axis and angle
            float angle = Tools.GetAngleAtVertex(prevSegment.position, Body[segIndex].position, Body[segIndex + 1].position, out Vector3 R);
            bool rotationAxisFound = false;
            Debug.DrawRay(Body[segIndex].position, R * 10, Color.green);

            //If in a straightaway there are errors of a few degrees, so if the starting 3 nodes are in a straight line then a rotation axis cannot be determined because it could be R or -R
            if (180 - Mathf.Abs(angle) < TurnThreshold) //If the front of the snake isn't turning but it starts to turn eventually we have to make sure to be using the correct R and r
            {
                if (R.magnitude == 0) //if |R| is 0 then angle is 180
                {
                    R = transform.TransformDirection(LastRotationAxis); //Set it to up if ||R|| is 0
                    angle = 180f;
                }
            } 
            else
            {
                rotationAxisFound = true;
            }

            angleSum += angle;
            


            while (!laserSegEndFound)
            {
                int expectedAngle = (n - 2) * 180;//Sum of inside angles of any noncomplex polygon is (n - 2)180 degrees

                //If we reached the tail of the snake, make that the final laser segment
                if (segIndex == Body.Length - 1)
                {
                    laserNodes.Add(Body[segIndex]);
                    finishedFindingNodes = true;
                    laserSegEndFound = true;
                    continue;
                }

                //Check for current body segment makes polygon not concave
                if (n > 3)
                {
                    bool inStraightAway = false;

                    //Get rotation axis and angle for current node. By doing this it accounts for error accumulated if only R was used
                    angle = Tools.GetAngleAtVertex(prevSegment.position, Body[segIndex].position, Body[segIndex + 1].position, out Vector3 r);

                    if (180 - Mathf.Abs(angle) < TurnThreshold) //Will only be true if in a straight away
                    {
                        if (r.magnitude == 0) //if |r| is 0 then angle is 180
                        {
                            r = Body[segIndex].TransformDirection(Vector3.up); //Set it to up if ||R|| is 0
                            angle = 180f;
                        }
                        inStraightAway = true;
                    }

                    bool rRaligned = Vector3.Angle(R, r) < 90; //Check if r and R are aligned

                    if (!rotationAxisFound && !inStraightAway)
                    {
                        if (!rRaligned) //Keep r inline with R so that way all angles measured are inside angles
                        {
                            R = -R;
                            rRaligned = true;
                            angleSum = (360f * (n - 3f)) - angleSum;
                        }

                        rotationAxisFound = true;
                    }

                    if (!rRaligned)
                    {
                        r = -r;
                        angle = Tools.GetAngleAtVertex(prevSegment.position, Body[segIndex].position, Body[segIndex + 1].position, r);
                    }

                    //Get all of the angles for the body segments with real sides
                    if (angle < 0)
                    {
                        angle = 360 + angle;
                    }
                    angleSum += angle;

                    //Calulate the data for the nonexistent side
                    //Vertex at the start of the current segment
                    float tempAngle1 = Tools.GetAngleAtVertex(Body[segIndex + 1].position, laserNodes[laserNodes.Count - 1].position, Body[segIndex - (n - 3)].position, out Vector3 startr);
                    if (Vector3.Angle(startr, R) > 90)
                    {
                        startr = -startr;
                    }

                    //Vertex at the end of the current segment
                    float tempAngle2 = Tools.GetAngleAtVertex(Body[segIndex].position, Body[segIndex + 1].position, laserNodes[laserNodes.Count - 1].position, out Vector3 endr);
                    if (Vector3.Angle(endr, R) > 90)
                    {
                        endr = -endr;
                    }

                    float tempAngleSum = 0; //Store the angles between the first side and nonexistent side and last side and nonexistent side

                    if (!inStraightAway)
                    {
                        if (tempAngle1 < 0)
                        {
                            startr = -startr;
                            tempAngle1 = Tools.GetAngleAtVertex(Body[segIndex + 1].position, laserNodes[laserNodes.Count - 1].position, Body[segIndex - (n - 3)].position, startr);
                        }
                        if (tempAngle2 < 0)
                        {
                            endr = -endr;
                            tempAngle2 = Tools.GetAngleAtVertex(Body[segIndex].position, Body[segIndex + 1].position, laserNodes[laserNodes.Count - 1].position, endr);
                        }
                    }

                    tempAngleSum += tempAngle1;//Add angle of vertex between nonexistent side and first side
                    tempAngleSum += tempAngle2; //Add angle of vertex between nonexistent side and last side 

                    float actualAngleSum; //Holds the sum of the real angles and the nonexistent angles
                    if (inStraightAway)
                    {
                        actualAngleSum = expectedAngle; //Account for small but compounding errors
                    }
                    else
                    {
                        actualAngleSum = angleSum + tempAngleSum; //real angles + nonexistent angles = actualAngleSum
                    }
                    
                    bool isConcave = (actualAngleSum > expectedAngle - 1 && actualAngleSum < expectedAngle + 1); //Determine if concave or complex
                    if (isConcave)
                    {
                        float tempError = actualAngleSum - expectedAngle;
                        angleSum -= tempError;
                        
                    }

                    //Once the current seg does not make a concave polygon
                    if (!isConcave)
                    {
                        laserNodes.Add(Body[segIndex - 1]); //Go back 1 to the last body seg that made a concave shape
                        finishedFindingNodes = true;
                        laserSegEndFound = true;
                        continue;
                    }
                }
                
                prevSegment = Body[segIndex]; //Now that the loop has finished and is to move to the next node, current node becomes the previos node
                n++; //Add 1 to points in convex shape we are trying to make
                segIndex++;//Move from prev segment to current segment
            }
        }

        if (laserNodes.Count < 3)
        {
            lr.SetPosition(0, laserNodes[0].position);
            lr.SetPosition(1, laserNodes[1].position);
        }
        else if (laserNodes.Count == 3)
        {
            lr.SetPosition(0, laserNodes[0].position);
            lr.SetPosition(1, laserNodes[2].position);
        }
        else if (laserNodes.Count > 3)
        {
            lr.SetPosition(0, laserNodes[0].position);
            lr.SetPosition(1, laserNodes[2].position);
        }


        

        for (int i = 0; i < laserNodes.Count; ++i)
        {
            laserNodes[i].GetComponent<Renderer>().material.color = Color.green;
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
        if (rotationAxis.magnitude != 0)
        {
            LastRotationAxis = rotationAxis;
        }
        float rotationSpeed = RotationSpeed * inputVector.magnitude * Time.deltaTime;
        
        //Perform rotations
        Quaternion newRotation = Quaternion.AngleAxis(rotationSpeed, rotationAxis); //Rotate head

        //Update rotation data
        transform.rotation *= newRotation;

        //Move based on new rotation data
        Vector3 movement = transform.TransformDirection(Vector3.forward) * Speed * Time.deltaTime;
        transform.position += movement;
    }

    void moveBody()
    {
        float targetUpdateTime = currentBodySize / Speed; //Unit of time to measure when to update target body positions

        timeSinceLastTargetPosUpdate += Time.deltaTime;

        //Update new positions when moved the length of 1 body segment
        if (timeSinceLastTargetPosUpdate >= currentBodySize / Speed) //If time to update target positions
        {
            //Move all body segments to the target position
            for (int i = 0; i < Body.Length; ++i)
            {
                Body[i].position = targetBodyPositions[i];
            }

            timeSinceLastTargetPosUpdate -= targetUpdateTime; //Remove 1 unit of update time. I assume there will be a remainder to be accounted for

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
        }


        float timeMovementRatio = timeSinceLastTargetPosUpdate / targetUpdateTime; //How far to lerp

        //For all body segments, set position to proper vector lerped between position last update and target position based on percent
        for (int i = 0; i < Body.Length; ++i)
        {
            Body[i].rotation = Quaternion.Lerp(lastBodyRotations[i], targetBodyRotations[i], timeMovementRatio);
            Body[i].position = Vector3.Lerp(lastBodyPositions[i], targetBodyPositions[i], timeMovementRatio);
        }
    }

    #endregion

    #region Initialization funtions
    void initializePositionData()
    {
        transform.position = SpawnPoint;
    }

    void generateHead()
    {
        Head = Instantiate(SnakeHead).transform;
        Head.SetParent(transform);
        Head.GetComponent<SnakeHeadController>().Init(MaxDirectionLaserDistance, LaserWidth, StartHeadSize);
    }

    void generateBody()
    {
        float segmentSpawnOffset = (StartHeadSize / 2) + (StartBodySize / 2);

        Body = new Transform[StartingLength];
        for (int i = 0; i < StartingLength; ++i)
        {
            GameObject tempBodySeg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            float tempSpawnOffset = segmentSpawnOffset + (StartBodySize * i);
            tempBodySeg.transform.position = SpawnPoint + (-Vector3.forward * tempSpawnOffset);
            
            Body[i] = tempBodySeg.transform;        
        }
    }

    void initializeMovementData()
    {
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
