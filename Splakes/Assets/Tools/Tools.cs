using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Tools
{
    public static Vector3 RotateAroundPivot(Vector3 point, Vector3 pivot, Vector3 eulerAngles)
    {
        Vector3 direction = point - pivot;
        direction = Quaternion.Euler(eulerAngles) * direction;

        Vector3 newPoint = direction + pivot;
        return newPoint;
    }
}
