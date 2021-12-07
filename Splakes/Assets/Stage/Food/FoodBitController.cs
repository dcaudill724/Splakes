using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodBitController : MonoBehaviour
{
    public Transform HeadTransform;
    public float ExplosionForce;
    public Vector3 ExplosionDirection;

    private Vector3 direction;
    private bool goingOut = true;

    // Start is called before the first frame update
    void Start()
    {
        direction = ExplosionDirection * ExplosionForce;
    }

    // Update is called once per frame
    void Update()
    {
        if (Vector3.Distance(HeadTransform.position, transform.position) < 0.1f)
        {
            HeadTransform.GetComponent<SnakeHeadController>().FeedSnake(1);
            Destroy(gameObject);
        }

        if (goingOut)
        {
            Vector3 dirToAdd = -ExplosionDirection * 0.005f;
            direction += dirToAdd;
            
            if (Vector3.Angle(direction, ExplosionDirection) > 5)
            {
                goingOut = false;
            }
        }
        else
        {
            direction = (HeadTransform.position - transform.position);
            float distanceMultiplier = (1 / Mathf.Sqrt(direction.magnitude + 1)) * 0.4f;
            direction = direction.normalized * distanceMultiplier;
        }


        transform.position = transform.position + direction;
    }
}
