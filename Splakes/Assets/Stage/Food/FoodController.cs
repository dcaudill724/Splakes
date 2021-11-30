using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodController : MonoBehaviour
{
    public GenerateFood FoodGenerator;

    public int PointValue;
    public Color FoodColor;
    public float ParticleEmissionRate;

    // Start is called before the first frame update
    void Start()
    {
        transform.GetComponent<Renderer>().material.color = FoodColor;

        var main = transform.GetComponent<ParticleSystem>();
        main.emissionRate = ParticleEmissionRate;
       

        transform.GetComponent<ParticleSystemRenderer>().material.SetColor("_Color", FoodColor);


        transform.GetChild(0).GetComponent<Light>().color = FoodColor;



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
        FoodGenerator.SpawnNewFood();
        Destroy(transform.gameObject);
    }
}
