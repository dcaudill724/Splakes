using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateFood : MonoBehaviour
{
    public Vector3 MinFoodBounds;
    public Vector3 MaxFoodBounds;

    public int FoodObjectsInPlay;

    //Prefab
    public GameObject FoodPrefab;


    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < FoodObjectsInPlay; ++i)
        {
            GameObject food = Instantiate(FoodPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            float tempX = Random.Range(MinFoodBounds.x, MaxFoodBounds.x);
            float tempY = Random.Range(MinFoodBounds.y, MaxFoodBounds.y);
            float tempZ = Random.Range(MinFoodBounds.z, MaxFoodBounds.z);
            food.transform.position = new Vector3(tempX, tempY, tempZ);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
