using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform SnakeObject; //SnakeObject holds position data

    //Position data
    public Vector3 Offset;
    public Vector3 EulerRotation;

    private Quaternion quaternionRotation;

    // Start is called before the first frame update
    void Start()
    {
        quaternionRotation = Quaternion.Euler(EulerRotation);
    }

    // Update is called once per frame
    void Update()
    {
        //Rotate offset
        Vector3 newOffset = SnakeObject.rotation * Offset;

        //Rotate rotation
        Quaternion newRotation = SnakeObject.rotation * quaternionRotation;

        transform.position = SnakeObject.position + newOffset;
        transform.rotation = newRotation;

    }
}
