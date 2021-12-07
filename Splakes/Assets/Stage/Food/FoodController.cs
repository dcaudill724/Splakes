using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class FoodController : MonoBehaviour
{
    public GenerateFood FoodGenerator;

    public int PointValue;
    public Color FoodColor;
    public float ParticleEmissionRate;

    public GameObject FoodBit;
    public float FoodEatenExplosionForce;

    // Start is called before the first frame update
    void Start()
    {
        transform.GetComponent<Renderer>().material.color = FoodColor;

        var main = transform.GetComponent<ParticleSystem>();
        main.emissionRate = ParticleEmissionRate;
       
        transform.GetComponent<ParticleSystemRenderer>().material.SetColor("_Color", FoodColor);

        transform.GetChild(0).GetComponent<Light>().color = FoodColor;

    }

    private void OnCollisionEnter(Collision collision)
    {   
        if (collision.gameObject.tag == "SnakeHead")
        {
            getEaten(collision.transform);
        }
    }

    void getEaten(Transform headTransform)
    {
        transform.parent.GetComponent<FoodSoundController>().PlayFoodEatenSoundEffect();

        for (int i = 0; i < PointValue; ++i)
        {
            Vector3 spawnPoint = Random.insideUnitSphere * transform.localScale.x / 2;

            GameObject foodBit = Instantiate(FoodBit);
            foodBit.transform.parent = null;
            foodBit.transform.position = transform.position + spawnPoint;
            foodBit.GetComponent<FoodBitController>().HeadTransform = headTransform;
            foodBit.GetComponent<FoodBitController>().ExplosionForce = FoodEatenExplosionForce;
            foodBit.GetComponent<FoodBitController>().ExplosionDirection = spawnPoint.normalized;
            foodBit.GetComponent<Renderer>().material.SetColor("_Color", FoodColor);
        }

        FoodGenerator.EatFood(this);
    }
}
