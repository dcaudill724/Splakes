using UnityEngine;

public class FoodController : MonoBehaviour
{
    //Identification
    public int ID;
    
    //Camera data for optimization of particle system
    public Transform Camera;
    public int ParticleRenderDistance;
    private bool isVisible;
    private bool isCloseEnough;

    //Food generator data
    public GenerateFood FoodGenerator;

    //Prefabs
    public GameObject FoodBitPrefab;

    //Means of spawning data
    public bool SpawnedFromSnake;
    public float ActiveDelay;
    private float timeInactive;
    private bool isActive = false;

    //Food object data
    public int PointValue;
    public Color FoodColor;
    public float ParticleEmissionRate;

    // Start is called before the first frame update
    void Start()
    {
        transform.GetComponent<Renderer>().material.color = FoodColor;

        ParticleSystem.EmissionModule em = GetComponent<ParticleSystem>().emission;
        em.rateOverTime = ParticleEmissionRate;
       
        transform.GetComponent<ParticleSystemRenderer>().material.SetColor("_Color", FoodColor);

        transform.GetChild(0).GetComponent<Light>().color = FoodColor;

    }

    void Update()
    {
        if (!isActive)
        {
            timeInactive += Time.deltaTime;

            if (timeInactive >= ActiveDelay)
            {
                isActive = true;
            }

        }

        

        if (isVisible)
        {
            if (Vector3.Distance(transform.position, Camera.position) < ParticleRenderDistance)
            {
                isCloseEnough = true;
            }
            else
            {
                isCloseEnough = false;
            }
        }

        if (isVisible && isCloseEnough)
        {
            ParticleSystem.EmissionModule em = GetComponent<ParticleSystem>().emission;
            em.enabled = true;
        }
        else
        {
            ParticleSystem.EmissionModule em = GetComponent<ParticleSystem>().emission;
            em.enabled = false;
        }
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isActive)
        {
            if (collision.gameObject.tag == "SnakeHead")
            {
                getEaten(collision.transform);
            }
        }
    }

    void getEaten(Transform headTransform)
    {
        transform.parent.GetComponent<FoodSoundController>().PlayFoodEatenSoundEffect();

        for (int i = 0; i < PointValue; ++i)
        {
            Vector3 spawnPoint = Random.insideUnitSphere * transform.localScale.x / 2;

            GameObject foodBit = Instantiate(FoodBitPrefab);
            foodBit.transform.parent = null;
            foodBit.transform.position = transform.position + spawnPoint;
            foodBit.GetComponent<FoodBitController>().HeadTransform = headTransform;
            foodBit.GetComponent<FoodBitController>().SpawnExplosionDirection = spawnPoint.normalized;
            foodBit.GetComponent<Renderer>().material.SetColor("_Color", FoodColor);
        }

        FoodGenerator.EatFood(ID);
    }

    private void OnBecameVisible()
    {
        isVisible = true;
    }

    private void OnBecameInvisible()
    {
        isVisible = false;
    }
}
