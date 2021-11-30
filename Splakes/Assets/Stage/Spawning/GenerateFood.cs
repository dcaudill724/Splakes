using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateFood : MonoBehaviour
{
    public Transform StageBoundary;
    public Color[] FoodColors = new Color[0];
    public Vector2Int PointValueRange = new Vector2Int(2, 10);
    public Vector2 SizeRange = new Vector2(0.5f, 1.3f);
    public Vector2Int EmissionRateRange = new Vector2Int(2, 10);

    public int FoodObjectsInPlay;

    //Prefab
    public GameObject FoodPrefab;

    private GameObject foodContainer;

    // Start is called before the first frame update
    void Start()
    {

        foodContainer = new GameObject("Food Container");
        foodContainer.transform.position = StageBoundary.position;

        for (int i = 0; i < FoodObjectsInPlay; ++i)
        {
            SpawnNewFood();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnNewFood()
    {
        GameObject food = Instantiate(FoodPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        food.GetComponent<FoodController>().FoodGenerator = this;

        Vector3 unitPos = Random.insideUnitSphere;
        food.transform.position = unitPos * StageBoundary.localScale.x / 2;

        int tempPointValue = Random.Range(PointValueRange.x, PointValueRange.y + 1);
        food.GetComponent<FoodController>().PointValue = tempPointValue;

        float tempScale = Tools.Map(tempPointValue, PointValueRange.x, PointValueRange.y, SizeRange.x, SizeRange.y);
        food.transform.localScale = new Vector3(tempScale, tempScale, tempScale);

        float tempEmissionRate = Tools.Map(tempPointValue, PointValueRange.x, PointValueRange.y, EmissionRateRange.x, EmissionRateRange.y);
        food.GetComponent<FoodController>().ParticleEmissionRate = tempEmissionRate;

        food.transform.GetComponent<FoodController>().FoodColor = FoodColors[Random.Range(0, FoodColors.Length)];

        food.transform.SetParent(foodContainer.transform);
    }
}
