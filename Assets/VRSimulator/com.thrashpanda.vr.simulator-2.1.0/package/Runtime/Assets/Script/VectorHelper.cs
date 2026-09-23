using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Class: VectorHelper
 * Helper class for additional vector math.
 * 
 */
public static class VectorHelper
{
    /* Function: RotatePointAroundPivot
     * Rotates a given position around a point by the given eugler angles.
     * 
     * Parameters:
     * point - the vector position to rotate
     * pivot - the point to rotate around
     * angles - the rotation in euler angles
     * 
     */
    public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
    {

        Vector3 dir = point - pivot;
        dir = Quaternion.Euler(angles) * dir;
        point = dir + pivot;
        return point;
    }
}
