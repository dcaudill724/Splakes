using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnakeHeadController : MonoBehaviour
{
    private float laserWidth;
    private float scale;
    private Transform directionLaser;
    private int maxDirectionLaserDistance;

    // Start is called before the first frame update
    void Start()
    {
        transform.localScale = new Vector3(scale, scale, scale);

        directionLaser = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
        directionLaser.localScale = Vector3.Scale(directionLaser.localScale, new Vector3(laserWidth, 0, laserWidth));
        directionLaser.transform.rotation = Quaternion.Euler(90, 0, 0);
        directionLaser.localPosition = Vector3.zero;
        directionLaser.SetParent(transform.parent);
    }

    // Update is called once per frame
    void Update()
    {
        updateDirectionLaser();
    }

    public void FeedSnake(int points)
    {
        transform.parent.gameObject.GetComponent<SnakeController>().FeedSnake(points);
    }

    void updateDirectionLaser()
    {
        RaycastHit hit;
        float laserLength = maxDirectionLaserDistance;

        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, maxDirectionLaserDistance))
        {
            laserLength = hit.distance / 2;
        }

        directionLaser.localScale = new Vector3(laserWidth, laserLength, laserWidth);
        directionLaser.localPosition = Vector3.forward * laserLength;
    }

    public void Init(int maxDirectionLaserDistance, float laserWidth, float scale)
    {
        this.maxDirectionLaserDistance = maxDirectionLaserDistance;
        this.laserWidth = laserWidth;
        this.scale = scale;
    }
}
