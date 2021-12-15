using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodBitController : MonoBehaviour
{
    //Transform of head of snake that ate the food
    public Transform HeadTransform;

    //Spawning data
    public float SpawnExplosionForce;
    public Vector3 SpawnExplosionDirection;
   

    //Position / Velocity control data
    public float HeadGravity;
    public float SpawnExplosionForceDampening;

    private Vector3 velocity;
    private bool goingOut = true; //Signifies if the particles is still moving outward from initial spawn explosion or not

    // Start is called before the first frame update
    void Start()
    {
        velocity = SpawnExplosionDirection * SpawnExplosionForce;
    }

    // Update is called once per frame
    void Update()
    {
        //On collision with head transform
        if (Vector3.Distance(HeadTransform.position, transform.position) < 0.1f)
        {
            HeadTransform.GetComponent<SnakeBodyController>().FeedSnake(1);
            Destroy(gameObject);
        }


        if (goingOut)
        {
            //Dampen the velocity
            Vector3 velToAdd = -SpawnExplosionDirection * SpawnExplosionForceDampening;
            velocity += velToAdd;
            
            //Determine if the velocity has changed directions
            if (Vector3.Angle(velocity, SpawnExplosionDirection) > 5)
            {
                goingOut = false;
            }
        }
        else
        {
            //Get new direction of velocity
            velocity = (HeadTransform.position - transform.position);

            //Basic gravity calulation, force multiplier increases exponentially as the food bit is closer to the head transform
            float forceMultiplier = (1 / Mathf.Sqrt(velocity.magnitude + 1)) * HeadGravity;

            //Set magnitude (speed) of the velocity
            velocity = velocity.normalized * forceMultiplier;
        }

        //Move the position by its velocity
        transform.position = transform.position + velocity;
    }
}
