using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodController : MonoBehaviour
{
    public int PointValue;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {   
        if (collision.gameObject.tag == "SnakeHead")
        {
            collision.gameObject.GetComponent<SnakeHeadController>().FeedSnake(PointValue);
            getEaten();
        }
    }

    void getEaten()
    {
        Destroy(transform.gameObject);
    }
}
