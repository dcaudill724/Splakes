using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Tools
{
    public enum RegAxis 
    { 
        x,
        y,
        z
    }

    //Returns array length 2 where [0] is low bound and [1] is high bounds
    public static Vector3[] GetBounds(Vector3[] points)
    {
        Vector3 highBounds = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        Vector3 lowBounds = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);

        for (int i = 0; i < points.Length; ++i)
        {
            if (points[i].x < lowBounds.x)
            {
                lowBounds.x = points[i].x;
            }
            if (points[i].y < lowBounds.y)
            {
                lowBounds.y = points[i].y;
            }
            if (points[i].z < lowBounds.z)
            {
                lowBounds.z = points[i].z;
            }
            if (points[i].x > highBounds.x)
            {
                highBounds.x = points[i].x;
            }
            if (points[i].y > highBounds.y)
            {
                highBounds.y = points[i].y;
            }
            if (points[i].z > highBounds.z)
            {
                highBounds.z = points[i].z;
            }
        }

        return new Vector3[] { lowBounds, highBounds };
    }

    public static Vector3 RotateAroundPivot(Vector3 point, Vector3 pivot, Vector3 eulerAngles)
    {
        Vector3 direction = point - pivot;
        direction = Quaternion.Euler(eulerAngles) * direction;

        Vector3 newPoint = direction + pivot;
        return newPoint;
    }

    //Returns plane equation (ax + by + cz + d = 0) like [a, b, c, d]
    public static float[] GetPlaneFrom3Points (Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float a1 = p2.x - p1.x;
        float b1 = p2.y - p1.y;
        float c1 = p2.z - p1.z;
        float a2 = p3.x - p1.x;
        float b2 = p3.y - p1.y;
        float c2 = p3.z - p1.z;
        float a = b1 * c2 - b2 * c1;
        float b = a2 * c1 - a1 * c2;
        float c = a1 * b2 - b1 * a2;
        float d = (-a * p1.x - b * p1.y - c * p1.z);
        return new float[] { a, b, c, d };
    }

    public static float GetPointDistanceFromPlane(Vector3 point, Vector3 planeNormal, Vector3 pointOnPlane)
    {
        Vector3 n = planeNormal.normalized;
        Vector3 v = point - pointOnPlane;
        return Vector3.Dot(v, n);
    }

    public static float GetAngleAtVertex(Vector3 p1, Vector3 vertex, Vector3 p2, out Vector3 rAxis)
    {
        Vector3 s1 = p1 - vertex;
        Vector3 s2 = p2 - vertex;
        rAxis = Vector3.Cross(p1, p2).normalized;
        float angle = Vector3.SignedAngle(s1, s2, rAxis);
        if (angle < 0)
        {
            rAxis = -rAxis;
            angle = Vector3.SignedAngle(s1, s2, rAxis);
        }

        return angle;
    }

    public static float GetAngleAtVertex(Vector3 p1, Vector3 vertex, Vector3 p2, Vector3 rAxis)
    {
        Vector3 s1 = p1 - vertex;
        Vector3 s2 = p2 - vertex;
        return Vector3.SignedAngle(s1, s2, rAxis);
    }

    public static float LinePointDist(Vector3 x1y1z1, Vector3 x2y2z2, Vector3 point)
    {
        //Calulate horizontal distance
        float A = point.x - x1y1z1.x;
        float B = point.y - x1y1z1.y;
        float C = x2y2z2.x - x1y1z1.x;
        float D = x2y2z2.y - x1y1z1.y;

        float dot = A * C + B * D;
        float lenSq = C * C + D * D;
        float param = dot / lenSq;

        float xx = 0;
        float yy = 0;

        if(param < 0)
        {
            xx = x1y1z1.x;
            yy = x1y1z1.y;
        }
        else if (param > 1)
        {
            xx = x2y2z2.x;
            yy = x2y2z2.y;
        }
        else
        {
            xx = x1y1z1.x + param * C;
            yy = x1y1z1.y + param * D;
        }

        float dx = point.x - xx;
        float dy = point.y - yy;

        float horizontalDistSq = dx * dx + dy * dy;


        float A2 = point.x - x1y1z1.x;
        float B2 = point.z - x1y1z1.z;
        float C2 = x2y2z2.x - x1y1z1.x;
        float D2 = x2y2z2.z - x1y1z1.z;

        float dot2 = A2 * C2 + B2 * D2;
        float lenSq2 = C2 * C2 + D2 * D2;
        float param2 = dot2 / lenSq2;

        float xx2 = 0;
        float yy2 = 0;

        if (param2 < 0)
        {
            xx2 = x1y1z1.x;
            yy2 = x1y1z1.z;
        }
        else if (param2 > 1)
        {
            xx2 = x2y2z2.x;
            yy2 = x2y2z2.z;
        }
        else
        {
            xx2 = x1y1z1.x + param2 * C2;
            yy2 = x1y1z1.z + param2 * D2;
        }

        float dx2 = point.x - xx2;
        float dy2 = point.z - yy2;

        float verticalDistSq = dx2 * dx2 + dy2 * dy2;

        return Mathf.Sqrt(horizontalDistSq + verticalDistSq);
    }
}
