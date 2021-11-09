using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnakeController : MonoBehaviour
{
    //Generation Data
    public Vector3 SpawnPoint;
    public int StartingLength;
    public float Speed;
    public float RotationSpeed;


    //Body data
    public Transform Head;
    public Transform[] Body; //Bruh, List<Transform> causes huge frame drops when adding body segments in generateBody. Therefore I sadly use array. Also is parallel with BodyMovement
    
    private float segmentSpawnOffset;
    private float bodyLength;


    //Movement data
    public Vector3 TrueForward;
    public Vector3 TrueUp;
    public Vector3 TrueRight;

    private Vector3 localForward;
    private Vector3 localUp;
    private Vector3 localRight;
    private Vector3[] bodyMovement; //Parallel with Body


    //Camera data
    public Transform Camera;


    // Start is called before the first frame update
    void Start()
    {
        TrueForward = new Vector3(-1, 0, 0);
        TrueUp = new Vector3(0, 1, 0);
        TrueRight = new Vector3(0, 0, 1);
        localForward = new Vector3(-1, 0, 0);
        localUp = new Vector3(0, 1, 0);
        localRight = new Vector3(0, 0, 1);


        generateHead();
        initializePositionData();
        generateBody();
        initializeMovementData();
        initializeCameraData();
    }

    // Update is called once per frame
    void Update()
    {
        moveHead();
        moveBody();
    }

    void moveHead()
    {
        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");

        //Rotation to add based on input in euler angles
        Vector3 rotationAxis = new Vector3(0, horizontal, -vertical).normalized;
        float rotationSpeed = RotationSpeed * Time.deltaTime;
        
        //Perform rotations
        Quaternion newRotation = Quaternion.AngleAxis(rotationSpeed, rotationAxis); //Rotate head

        //Update rotations
        transform.rotation *= newRotation;
        TrueForward = transform.TransformDirection(localForward);
        TrueUp = transform.TransformDirection(localUp);
        TrueRight = transform.TransformDirection(localRight);

        //Move based on new rotations
        Vector3 movement = TrueForward * Speed * Time.deltaTime;
        transform.position += movement;

        Debug.DrawRay(transform.position, TrueForward * 20, Color.green);
    }

    void moveBody()
    {

        //Update bodyMovement
        //Starting at the end of the body, fetch the movement vector from the segment before it. 
        for (int i = 0; i < bodyMovement.Length; ++i)
        {
            Vector3 currentPos = Body[i].transform.position;
            Vector3 nextPos = Vector3.zero; //Zero for now, will always be position of body segment before it

            //If first body segment fetch from head, otherwise fetch from segment in front of it
            if (i == 0)
            {
                nextPos = transform.position;
            }
            else
            {
                nextPos = Body[i - 1].transform.position;
            }

            Vector3 newBodyMovement = (nextPos - currentPos).normalized;

           bodyMovement[i] = newBodyMovement;
        }

        //Move the body segments
        for (int i = 0; i < Body.Length; ++i)
        {
            Body[i].transform.position += bodyMovement[i] * Speed * Time.deltaTime;
        }
    }

    void initializePositionData()
    {
        transform.position = SpawnPoint;
        segmentSpawnOffset += 1.25f; //For body segment generation
    }

    void generateHead()
    {
        Head = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        Head.localScale = Vector3.Scale(Head.transform.localScale, new Vector3(1.5f, 1.5f, 1.5f));
        Head.SetParent(transform); //Set empty snake object as parent for control purposes
    }

    void generateBody()
    {
        bodyLength = StartingLength;

        Body = new Transform[StartingLength];
        for (int i = 0; i < StartingLength; ++i)
        {
            GameObject tempBodySeg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tempBodySeg.transform.position = SpawnPoint + new Vector3(segmentSpawnOffset, 0, 0);
            segmentSpawnOffset += 1.0f;
            Body[i] = tempBodySeg.transform;        
        }
    }

    void initializeMovementData()
    {
        bodyMovement = new Vector3[StartingLength];
        for (int i = 0; i < StartingLength; i++)
        {
            bodyMovement[i] = TrueForward;
        }
    }

    void initializeCameraData()
    {
        Camera = GameObject.Find("Main Camera").transform;
        Camera.GetComponent<CameraController>().SnakeObject = transform;
    }
}
