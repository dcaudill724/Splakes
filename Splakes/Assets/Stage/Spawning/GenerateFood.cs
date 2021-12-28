using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GenerateFood : MonoBehaviourPun, EventReceiver
{
    //Rendering optimization data
    public Transform Camera;
    public int ParticleRenderDistance;

    //Generation data
    public int FoodObjectsInPlay;

    //Stage boundary data
    public Transform StageBoundary;
    private GameObject foodContainer;

    //Audio data
    public AudioClip FoodEatenSoundEffect;

    //Object storage
    private List<GameObject> foodList;

    //Visual data
    public Color[] FoodColors = new Color[0];
    public Vector2Int PointValueRange = new Vector2Int(2, 10);

    public Vector2 SizeRange = new Vector2(0.5f, 1.3f);
    public Vector2Int EmissionRateRange = new Vector2Int(2, 10);

    //Food eaten data
    public float FoodEatenExplosionForce;

    //Prefabs
    public GameObject FoodPrefab;
    public GameObject FoodBitPrefab;

    private Material[] foodColorMaterials;
    private Material[] foodbitColorMaterials;
    

    void Start()
    {
        Debug.Log("made it here");

        //Add to easy event system
        EasyEventSystem.AddReceiver(this);

        //Storage initialization
        foodList = new List<GameObject>();

        foodColorMaterials = new Material[FoodColors.Length];
        foodbitColorMaterials = new Material[FoodColors.Length];

        for (int i = 0; i < FoodColors.Length; ++i)
        {
            foodColorMaterials[i] = new Material(FoodPrefab.GetComponent<Renderer>().sharedMaterial);
            foodColorMaterials[i].color = FoodColors[i];

            foodbitColorMaterials[i] = new Material(FoodBitPrefab.GetComponent<Renderer>().sharedMaterial);
            foodbitColorMaterials[i].color = FoodColors[i];
        }

        //Initialize a parent object to all food objects for cleanliness and audio functions
        foodContainer = new GameObject("Food Container");
        foodContainer.transform.position = StageBoundary.position;
        foodContainer.AddComponent<AudioSource>();
        foodContainer.GetComponent<AudioSource>().volume = 0.05f;
        foodContainer.AddComponent<FoodSoundController>().FoodEatenSoundEffect = FoodEatenSoundEffect;

        if (PhotonNetwork.IsMasterClient)
        {
            generateStageFood(); //Only the Master Client generates the stage
        }
        else
        {
            requestMasterFoodData(); //Other clients request food data from the master client
        }
    }

    public void ReceiveEvent(string eventName, object content)
    {
        switch (eventName)
        {
            case "body segment died":
                object[] conArray = (object[])content;

                if (PhotonNetwork.LocalPlayer == PhotonNetwork.MasterClient)
                {
                    addNewFood(generateFoodDataArray(generateUniqueID(), (Vector3)conArray[0], (int)conArray[1]), true);
                }
                else
                {
                    photonView.RPC("addNewFood", RpcTarget.MasterClient, generateFoodDataArray(generateUniqueID(), (Vector3)conArray[0], (int)conArray[1]), true);
                }
                break;
        }
    }



    private object[] generateFoodDataArray(int ID, Vector3 pos, int pointVal)
    {
        return new object[] { pos as object, pointVal as object, ID as object };
    }

    private GameObject spawnNewFood(int ID, Vector3 foodPosition, int foodPointValue, bool spawnedFromSnake)
    {
        //Instantiate new food object
        GameObject food = Instantiate(FoodPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        FoodController fc = food.GetComponent<FoodController>();
        food.transform.position = foodPosition;

        //Set food object data
        fc.FoodGenerator = this;
        fc.PointValue = foodPointValue;
        fc.SpawnedFromSnake = spawnedFromSnake;


        //Set size based on point value
        float tempScale = Tools.Map(foodPointValue, PointValueRange.x, PointValueRange.y, SizeRange.x, SizeRange.y);
        food.transform.localScale = new Vector3(tempScale, tempScale, tempScale);

        //Set emission rate based on point value
        float tempEmissionRate = Tools.Map(foodPointValue, PointValueRange.x, PointValueRange.y, EmissionRateRange.x, EmissionRateRange.y);
        fc.ParticleEmissionRate = tempEmissionRate;

        //Set random color
        int randColorIndex = Random.Range(0, FoodColors.Length);
        fc.SharedFoodBitMaterial = foodbitColorMaterials[randColorIndex];
        food.GetComponent<Renderer>().sharedMaterial = foodColorMaterials[randColorIndex];

        //Add food object to the container
        food.transform.SetParent(foodContainer.transform);

        //Give food object 
        fc.FoodBitPrefab = FoodBitPrefab;

        //Set render optimization data
        fc.Camera = Camera;
        fc.ParticleRenderDistance = ParticleRenderDistance;

        //Set ID
        fc.ID = ID;

        return food;
    }

    //Randomly generate new food object functions
    private void generateStageFood()
    {
        for (int i = 0; i < FoodObjectsInPlay; ++i)
        {
            addNewFood(generateRandomFoodData());
        }
    }

    private int generateUniqueID()
    {
        bool isUnique = false;
        int randFoodID = Random.Range(0, 99999);

        while (!isUnique)
        {
            bool containsID = false;

            foreach (GameObject food in foodList)
            {
                if (food.GetComponent<FoodController>().ID == randFoodID)
                {
                    containsID = true;
                    break;
                }
            }

            if (!containsID)
            {
                isUnique = true;
            }
        }

        return randFoodID;
    }

    private object[] generateRandomFoodData()
    {
        Vector3 unitPos = Random.insideUnitSphere;
        Vector3 truePos = unitPos * StageBoundary.localScale.x / 2;

        int tempPointValue = Random.Range(PointValueRange.x, PointValueRange.y + 1);

        int ID = generateUniqueID();

        return generateFoodDataArray(ID, truePos, tempPointValue);
    }


    //Synchronized food generation/destruction functions
    [PunRPC]
    private void addNewFood(object[] newFoodData, bool spawnedFromSnake = false)
    {
        //Parse food data
        Vector3 pos = (Vector3)newFoodData[0];
        int pointValue = (int)newFoodData[1];
        int foodID = (int)newFoodData[2];

        //Spawn the food
        GameObject newFood = spawnNewFood(foodID, pos, pointValue, spawnedFromSnake);

        foodList.Add(newFood);

        //Only if called by the master, make other clients add the food as well
        if (PhotonNetwork.LocalPlayer == PhotonNetwork.MasterClient)
        {
            photonView.RPC("addNewFood", RpcTarget.Others, newFoodData, spawnedFromSnake);
        }

    }

    [PunRPC]
    private void destroyFood(int ID)
    {
        GameObject foodToRemove = getFoodByID(ID).gameObject;

        foodList.Remove(foodToRemove);
        Destroy(foodToRemove);
    }


    //Food data initialization synchronization functions
    private void requestMasterFoodData()
    {
        photonView.RPC("respondToClientWithFoodData", RpcTarget.MasterClient);
    }

    [PunRPC]
    private void respondToClientWithFoodData(PhotonMessageInfo info)
    {
        int[] foodIDs = getFoodIDs(); //Get all the food ID's
        photonView.RPC("loadFood", info.Sender, foodIDs, getFoodPositions(foodIDs), getFoodPointValues(foodIDs), getFoodSpawnedFromSnakes(foodIDs)); //Send food ID's and corresponding food data

    }

    [PunRPC]
    private void loadFood(int[] foodIDs, Vector3[] foodPositions, int[] foodPointValues, bool[] spawnedFromSnakes)
    {
        for (int i = 0; i < foodIDs.Length; ++i)
        {
            addNewFood(generateFoodDataArray(foodIDs[i], foodPositions[i], foodPointValues[i]), spawnedFromSnakes[i]);
        }
    }


    //Fetch food data functions
    private int[] getFoodIDs()
    {
        int[] foodIDs = new int[foodList.Count];

        for (int i = 0; i < foodList.Count; ++i)
        {
            foodIDs[i] = foodList[i].GetComponent<FoodController>().ID;
        }

        return foodIDs;
    }

    private Vector3[] getFoodPositions(int[] foodIDs)
    {
        Vector3[] foodPositions = new Vector3[foodIDs.Length];

        for (int i = 0; i < foodIDs.Length; ++i)
        {
            foodPositions[i] = getFoodByID(foodIDs[i]).transform.position;
        }

        return foodPositions;
    }

    private int[] getFoodPointValues(int[] foodIDs)
    {

        
        int[] foodPointValues = new int[foodIDs.Length];


        for (int i = 0; i < foodIDs.Length; ++i)
        {
            foodPointValues[i] = getFoodByID(foodIDs[i]).PointValue;
        }

        return foodPointValues;
    }

    private bool[] getFoodSpawnedFromSnakes(int[] foodIDs)
    {
        bool[] spawnedFromSnakes = new bool[foodIDs.Length];

        for (int i = 0; i < foodIDs.Length; ++i)
        {
            spawnedFromSnakes[i] = getFoodByID(foodIDs[i]).SpawnedFromSnake;
        }

        return spawnedFromSnakes;
    }


    //Food eating functions
    public void EatFood(int ID)
    {
        photonView.RPC("InformMasterFoodEaten", RpcTarget.MasterClient, ID);
    }

    [PunRPC]
    private void InformMasterFoodEaten(int ID)
    {
        //Spawn a new food if the food was spawned naturally, not from a snake dying
        if (!getFoodByID(ID).SpawnedFromSnake)
        {
            addNewFood(generateRandomFoodData());
        }

        //Destroy the food for all clients
        photonView.RPC("destroyFood", RpcTarget.All, ID);
    }

    private FoodController getFoodByID(int ID)
    {
        foreach (GameObject food in foodList)
        {
            if (food.GetComponent<FoodController>().ID == ID)
            {
                return food.GetComponent<FoodController>();
            }
        }

        return null;
    }

    void OnDestroy()
    {
        for (int i = 0; i < FoodColors.Length; ++i)
        {
            Destroy(foodColorMaterials[i]);
            Destroy(foodbitColorMaterials[i]);
        }
    }
}
