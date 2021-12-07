using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnakeHeadController : MonoBehaviour
{


    private float scale;
   

    // Start is called before the first frame update
    void Start()
    {
        transform.localScale = new Vector3(scale, scale, scale);
    }

    // Update is called once per frame
    void Update()
    {


    }

    public void FeedSnake(int points)
    {
        transform.parent.gameObject.GetComponent<SnakeController>().FeedSnake(points);
    }

    public void Init(float scale)
    {
        this.scale = scale;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.transform.name == "StageBoundary" || collision.transform.name == "laser wall") {
            transform.parent.gameObject.GetComponent<SnakeController>().Die();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.name == "laser wall")
        {
            transform.parent.gameObject.GetComponent<SnakeController>().Die();
        }
    }
}
