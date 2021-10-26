using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateFood : MonoBehaviour
{
    public Vector3 MinFoodBounds;
    public Vector3 MaxFoodBounds;
    public int FoodObjectsInPlay;


    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < FoodObjectsInPlay; ++i)
        {
            GameObject tempFood = Instantiate(Resources.Load("Food") as GameObject);
            float tempX = Random.Range(MinFoodBounds.x, MaxFoodBounds.x);
            float tempY = Random.Range(MinFoodBounds.y, MaxFoodBounds.y);
            float tempZ = Random.Range(MinFoodBounds.z, MaxFoodBounds.z);
            tempFood.transform.position = new Vector3(tempX, tempY, tempZ);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
