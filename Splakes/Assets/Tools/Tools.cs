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

    #region Bounding Box Functions
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

    public static Vector2[] GetBounds(Vector2[] points)
    {
        Vector2 highBounds = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        Vector2 lowBounds = new Vector2(float.PositiveInfinity, float.PositiveInfinity);

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
            if (points[i].x > highBounds.x)
            {
                highBounds.x = points[i].x;
            }
            if (points[i].y > highBounds.y)
            {
                highBounds.y = points[i].y;
            }
        }

        return new Vector2[] { lowBounds, highBounds };
    }

    public static bool BoundingBoxCollision(Vector3[] bb1, Vector3[] bb2)
    {
        return ((bb1[0].x <= bb2[1].x && bb1[1].x >= bb2[0].x) &&
                (bb1[0].y <= bb2[1].y && bb1[1].y >= bb2[0].y) &&
                (bb1[0].z <= bb2[1].z && bb1[1].z >= bb2[0].z));
    }
    #endregion


    public static Vector3 RotateAroundPivot(Vector3 point, Vector3 pivot, Vector3 eulerAngles)
    {
        Vector3 direction = point - pivot;
        direction = Quaternion.Euler(eulerAngles) * direction;

        Vector3 newPoint = direction + pivot;
        return newPoint;
    }

    #region Plane functions
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

    public static float GetDistancePointToPlane(Vector3 point, float[] planeEquation) {
        float numerator = Mathf.Abs((planeEquation[0] * point.x) + (planeEquation[1] * point.y) + (planeEquation[2] * point.z) + planeEquation[3]);
        float denominator = Mathf.Sqrt(Mathf.Pow(planeEquation[0], 2) + Mathf.Pow(planeEquation[1], 2) + Mathf.Pow(planeEquation[2], 2));
        return numerator / denominator;
    }

    public static float GetSignedDistancePointToPlane(Vector3 point, float[] planeEquation)
    {
        float numerator = (planeEquation[0] * point.x) + (planeEquation[1] * point.y) + (planeEquation[2] * point.z) + planeEquation[3];
        float denominator = Mathf.Sqrt(Mathf.Pow(planeEquation[0], 2) + Mathf.Pow(planeEquation[1], 2) + Mathf.Pow(planeEquation[2], 2));
        return numerator / denominator;
    }

    public static Vector3 GetPlaneNormal(float[] planeEquation)
    {
        return new Vector3(planeEquation[0], planeEquation[1], planeEquation[2]).normalized;
    }

    public static Vector2[] Map3Dto2D(Vector3[] points, Vector3 normal)
    {
        //Assume points[0] is origin

        //Get two orthoginal vectors on the plane
        Vector3 vec1 = (points[1] - points[0]).normalized;
        Vector3 vec2 = Vector3.Cross(vec1, normal).normalized;

        Vector2[] newPoints = new Vector2[points.Length];
        for (int i = 0; i < points.Length; ++i) {
            newPoints[i] = new Vector2(Vector3.Dot(vec1, points[i] - points[0]), Vector3.Dot(vec2, points[i] - points[0]));
        }

        return newPoints;
    }
    #endregion

    #region Triangle functions
    public static bool PointInTriangle(Vector3 point, Vector3 tp1, Vector3 tp2, Vector3 tp3)
    {
        Vector3 v0 = tp3 - tp1;
        Vector3 v1 = tp2 - tp1;
        Vector3 v2 = point - tp1;

        float dot00 = Vector3.Dot(v0, v0);
        float dot01 = Vector3.Dot(v0, v1);
        float dot02 = Vector3.Dot(v0, v2);
        float dot11 = Vector3.Dot(v1, v1);
        float dot12 = Vector3.Dot(v1, v2);

        float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
        float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        return (u >= 0) && (v >= 0) && (u + v < 1);
    }
    #endregion




    public static float Map(float value, float r1start, float r1end, float r2start, float r2end)
    {
        return (value - r1start) / (r1end - r1start) * (r2end - r2start) + r2start;
    }



    public static bool LinesIntersect2D(Vector2 l1Start, Vector2 l1End, Vector2 l2Start, Vector2 l2End) 
    {
        Vector2 a = l1End - l1Start;
        Vector2 b = l2Start - l2End;
        Vector2 c = l1Start - l2Start;

        float alphaNumerator = b.y * c.x - b.x * c.y;
        float alphaDenominator = a.y * b.x - a.x * b.y;
        float betaNumerator = a.x * c.y - a.y * c.x;
        float betaDenominator = a.y * b.x - a.x * b.y;

        bool doIntersect = true;

        if (alphaDenominator == 0 || betaDenominator == 0)
        {
            doIntersect = false;
        }
        else
        {

            if (alphaDenominator > 0)
            {
                if (alphaNumerator < 0 || alphaNumerator > alphaDenominator)
                {
                    doIntersect = false;

                }
            }
            else if (alphaNumerator > 0 || alphaNumerator < alphaDenominator)
            {
                doIntersect = false;
            }

            if (doIntersect && betaDenominator > 0) {
                if (betaNumerator < 0 || betaNumerator > betaDenominator)
                {
                    doIntersect = false;
                }
            } else if (betaNumerator > 0 || betaNumerator < betaDenominator)
            {
                doIntersect = false;
            }
        }

        return doIntersect;
        /*float or1 = orientation(l1Start, l1End, l2Start);
        float or2 = orientation(l1Start, l1End, l2End);
        float or3 = orientation(l2Start, l2End, l1Start);
        float or4 = orientation(l2Start, l2End, l1End);

        if (or1 != or2 && or3 != or4) return true;

        return false;*/
    }



    public static float GetAngleAtVertex(Vector3 p1, Vector3 vertex, Vector3 p2, out Vector3 rAxis)
    {
        Vector3 s1 = p1 - vertex;
        Vector3 s2 = p2 - vertex;
        rAxis = Vector3.Cross(s1, s2).normalized;
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

    public static float pDistance(Vector2 p, Vector2 lstart, Vector2 lend) {

        float A = p.x - lstart.x;
        float B = p.y - lstart.y;
        float C = lend.x - lstart.x;
        float D = lend.y - lstart.y;

        float dot = A * C + B * D;
        float len_sq = C * C + D * D;
        float param = -1;
        if (len_sq != 0) //in case of 0 length line
            param = dot / len_sq;

        float xx, yy;

        if (param < 0)
        {
            xx = lstart.x;
            yy = lstart.y;
        }
        else if (param > 1)
        {
            xx = lend.x;
            yy = lend.y;
        }
        else
        {
            xx = lstart.x + param * C;
            yy = lstart.y + param * D;
        }

        float dx = p.x - xx;
        float dy = p.y - yy;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }
}
