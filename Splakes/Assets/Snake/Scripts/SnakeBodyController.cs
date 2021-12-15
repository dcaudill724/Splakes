using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.Events;

public class SnakeBodyController : MonoBehaviour
{
    private Player owner;
    private SnakeController parent;

    //Type of segement indicator
    public bool IsHead;

    //Death indicators
    public bool Dying;

    //Death parameters
    private float deathTime;
    private float deathStartDelay;
    private float deathLingerTime;

    // Start is called before the first frame update
    void Start()
    {

    }

    void Update()
    {
        //Only run when dying
        if (Dying)
        {
            deathTime += Time.deltaTime; //Increment how long this has been dying

            if (deathTime >= deathStartDelay) //If this has been dying for longer than the start delay, start death animation
            {
                transform.GetComponent<Renderer>().material.color = Color.red;

                if (deathTime >= deathStartDelay + deathLingerTime) //Once the death time is greater than the delay + linger time, it is time to officially declare death of the body segment
                {
                    if (PhotonNetwork.LocalPlayer == owner)
                    {
                        PhotonNetwork.Destroy(gameObject);

                        object eventContent = new object[]
                        {
                            transform.position,
                            3
                        };

                        EasyEventSystem.RaiseEvent("BodySegDied", eventContent);
                    }
                }
            }
        }
    }


    public void Init(Player owner, SnakeController parent, bool isHead)
    {
        this.owner = owner;
        this.parent = parent;
        IsHead = isHead;
    }

    public void Move(Vector3 position, Quaternion rotation)
    {
        if (!Dying)
        {
            transform.position = position;
            transform.rotation = rotation;
        }
    }

    //Only the owner can feed, synchronization problems arise otherwise. Can only be called if IsHead is true
    public void FeedSnake(int points)
    {
        if (owner == PhotonNetwork.LocalPlayer && IsHead)
        {
            parent.FeedSnake(points);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.transform.name == "StageBoundary" || collision.transform.name == "laser mesh")
        {
            if (!Dying)
            {
                if (IsHead)
                {
                    snakeDie();
                }
                else
                {
                    snakeHurt();
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.name == "laser mesh")
        {
            if (!Dying)
            {
                if (IsHead)
                {
                    snakeDie();
                }
                else
                {
                    snakeHurt();
                }
            }
        }
    }

    //Calls Hurt() on the snake object. Only the owner of the snake can cause it do get hurt, for synchronization purposes
    private void snakeHurt()
    {
        if (owner == PhotonNetwork.LocalPlayer)
        {
            parent.Hurt(transform);
        }
    }

    //Calls Die() on the snake object. Only the owner of the snake can cause it do die, for synchronization purposes
    private void snakeDie()
    {
        if (owner == PhotonNetwork.LocalPlayer)
        {
            parent.Die(true);
        }
    }

    public void Die(float startDelay, float lingerTime)
    {
        deathStartDelay = startDelay;
        deathLingerTime = lingerTime;
        deathTime = 0;
        Dying = true;
    }
}
