using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneratePowerUps : MonoBehaviour
{
    public Vector3 MinPowerUpBounds;
    public Vector3 MaxPowerUpBounds;

    public int PowerUpsInPlay;

    //Prefab
    public GameObject PowerupPrefab;


    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < PowerUpsInPlay; ++i)
        {
            GameObject powerup = Instantiate(PowerupPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            float tempX = Random.Range(MinPowerUpBounds.x, MaxPowerUpBounds.x);
            float tempY = Random.Range(MinPowerUpBounds.y, MaxPowerUpBounds.y);
            float tempZ = Random.Range(MinPowerUpBounds.z, MaxPowerUpBounds.z);
            powerup.transform.position = new Vector3(tempX, tempY, tempZ);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
