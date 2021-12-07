using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GenerateFood : MonoBehaviourPun
{
    public Transform StageBoundary;
    public Color[] FoodColors = new Color[0];
    public Vector2Int PointValueRange = new Vector2Int(2, 10);
    public Vector2 SizeRange = new Vector2(0.5f, 1.3f);
    public Vector2Int EmissionRateRange = new Vector2Int(2, 10);
    public AudioClip FoodEatenSoundEffect;

    public GameObject FoodBit;
    public float FoodEatenExplosionForce;
    public int FoodObjectsInPlay;

    public Dictionary<int, Vector3> FoodPositionsByID;
    public Dictionary<int, int> FoodPointValuesByID;
    public Dictionary<int, FoodController> FoodObjectsByID;
    public Dictionary<FoodController, int> FoodIDsByObject;

    //Prefab
    public GameObject FoodPrefab;
    private GameObject foodContainer;

    void Start()
    {
        InstantiateFoodContainer();
    }

    public void SpawnAllFood()
    {
        for (int i = 0; i < FoodObjectsInPlay; ++i)
        {
            object[] foodData = generateNewFoodData();
            Vector3 pos = (Vector3)foodData[0];
            int pointValue = (int)foodData[1];
            int foodID = (int)foodData[2];

            FoodPositionsByID.Add(foodID, pos);
            FoodPointValuesByID.Add(foodID, pointValue);

            FoodController newFood = SpawnNewFood(pos, pointValue);
            FoodObjectsByID.Add(foodID, newFood);
            FoodIDsByObject.Add(newFood, foodID);
        }
    }

    private object[] generateNewFoodData()
    {
        Vector3 unitPos = Random.insideUnitSphere;
        Vector3 truePos = unitPos * StageBoundary.localScale.x / 2;

        int tempPointValue = Random.Range(PointValueRange.x, PointValueRange.y + 1);

        bool IDunique = false;
        int randFoodID = Random.Range(0, 99999);
        while (!IDunique)
        {
            if (!FoodObjectsByID.ContainsKey(randFoodID))
            {
                IDunique = true;
            }
            else
            {
                randFoodID = Random.Range(0, 99999);
            }
        }

        return new object[]{ truePos as object, tempPointValue as object, randFoodID as object };
    }

    public int[] GetFoodIDs()
    {
        int[] foodIDs = new int[FoodObjectsByID.Count];
        FoodObjectsByID.Keys.CopyTo(foodIDs, 0);

        return foodIDs;
    }

    public Vector3[] GetFoodPositions(int[] foodIDs)
    {
        Vector3[] foodPositions = new Vector3[foodIDs.Length];

        for (int i = 0; i < foodIDs.Length; ++i)
        {
            foodPositions[i] = FoodPositionsByID[foodIDs[i]];
        }

        return foodPositions;
    }

    public int[] GetFoodPointValues(int[] foodIDs)
    {
        int[] foodPointValues = new int[foodIDs.Length];

        for (int i = 0; i < foodIDs.Length; ++i)
        {
            foodPointValues[i] = FoodPointValuesByID[foodIDs[i]];
        }

        return foodPointValues;
    }

    public void LoadFood(int[] foodIDs, Vector3[] foodPositions, int[] foodPointValues)
    {
        for (int i = 0; i < foodIDs.Length; ++i)
        {
            FoodPositionsByID.Add(foodIDs[i], foodPositions[i]);
            FoodPointValuesByID.Add(foodIDs[i], foodPointValues[i]);

            FoodController newFood = SpawnNewFood(foodPositions[i], foodPointValues[i]);
            FoodObjectsByID.Add(foodIDs[i], newFood);
            FoodIDsByObject.Add(newFood, foodIDs[i]);
        }
    }

    private void InstantiateFoodContainer()
    {
        FoodPositionsByID = new Dictionary<int, Vector3>();
        FoodPointValuesByID = new Dictionary<int, int>();
        FoodObjectsByID = new Dictionary<int, FoodController>();
        FoodIDsByObject = new Dictionary<FoodController, int>();

        foodContainer = new GameObject("Food Container");
        foodContainer.transform.position = StageBoundary.position;
        foodContainer.AddComponent<AudioSource>();
        foodContainer.GetComponent<AudioSource>().volume = 0.05f;
        foodContainer.AddComponent<FoodSoundController>().FoodEatenSoundEffect = FoodEatenSoundEffect;
    }

    public FoodController SpawnNewFood(Vector3 foodPosition, int foodPointValue)
    {
        GameObject food = Instantiate(FoodPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        food.GetComponent<FoodController>().FoodGenerator = this;

        food.transform.position = foodPosition;

        
        food.GetComponent<FoodController>().PointValue = foodPointValue;

        float tempScale = Tools.Map(foodPointValue, PointValueRange.x, PointValueRange.y, SizeRange.x, SizeRange.y);
        food.transform.localScale = new Vector3(tempScale, tempScale, tempScale);

        float tempEmissionRate = Tools.Map(foodPointValue, PointValueRange.x, PointValueRange.y, EmissionRateRange.x, EmissionRateRange.y);
        food.GetComponent<FoodController>().ParticleEmissionRate = tempEmissionRate;

        food.transform.GetComponent<FoodController>().FoodColor = FoodColors[Random.Range(0, FoodColors.Length)];

        food.transform.SetParent(foodContainer.transform);

        food.GetComponent<FoodController>().FoodBit = FoodBit;
        food.GetComponent<FoodController>().FoodEatenExplosionForce = FoodEatenExplosionForce;

        return food.GetComponent<FoodController>();
    }

    public void EatFood(FoodController fc)
    {
        int foodID = FoodIDsByObject[fc];
        photonView.RPC("MasterFoodEaten", RpcTarget.MasterClient, foodID);
    }

    [PunRPC]
    public void MasterFoodEaten(int foodID)
    {
        object[] foodData = generateNewFoodData();
        Vector3 newFoodPos = (Vector3)foodData[0];
        int newFoodPointValue = (int)foodData[1];
        int newFoodID = (int)foodData[2];

        photonView.RPC("EveryoneFoodEaten", RpcTarget.All, foodID, newFoodID, newFoodPos, newFoodPointValue);
    }

    [PunRPC]
    public void EveryoneFoodEaten(int foodEatenID, int newFoodID, Vector3 newFoodPosition, int newFoodPointValue)
    {
        FoodController foodEaten = FoodObjectsByID[foodEatenID];
        FoodIDsByObject.Remove(foodEaten);

        Destroy(FoodObjectsByID[foodEatenID].gameObject);
        FoodObjectsByID.Remove(foodEatenID);
        FoodPointValuesByID.Remove(foodEatenID);
        FoodPositionsByID.Remove(foodEatenID);

        FoodPositionsByID.Add(newFoodID, newFoodPosition);
        FoodPointValuesByID.Add(newFoodID, newFoodPointValue);

        FoodController newFood = SpawnNewFood(newFoodPosition, newFoodPointValue);
        FoodObjectsByID.Add(newFoodID, newFood);
        FoodIDsByObject.Add(newFood, newFoodID);
    }
}
