using System;
using System.Collections.Generic;
using UnityEngine;

public static class CollectionUtils
{
    #region Find Closest Index Functions
    public static int FindClosestIndex<T>(this IEnumerable<T> positions, T pivot, Func<T, T, float> distanceFunc)
    {
        if (positions is null) return -1;

        int closestIndex = -1;
        float minDist = float.MaxValue;
        int i = 0;
        foreach (var pos in positions)
        {
            var dist = distanceFunc(pos, pivot);
            if (dist < minDist)
            {
                minDist = dist;
                closestIndex = i;
            }
            i++;
        }
        return closestIndex;
    }
    
    public static int FindClosestIndex(this IEnumerable<int> positionArray, int pivot) =>
        positionArray.FindClosestIndex(pivot, (pos, pvt) => Math.Abs(pos - pvt));
    
    public static int FindClosestIndex(this IEnumerable<float> positionArray, float pivot) =>
        positionArray.FindClosestIndex(pivot, (pos, pvt) => Mathf.Abs(pos - pvt));
    
    public static int FindClosestIndex(this IEnumerable<Vector3> positionArray, Vector3 pivot) =>
        positionArray.FindClosestIndex(pivot, Vector3.Distance);
    
    public static int FindClosestIndex(this IEnumerable<Vector2> positionArray, Vector2 pivot) =>
        positionArray.FindClosestIndex(pivot, Vector2.Distance);
    #endregion
    
    public static bool TryGetSafeRange<T>(this List<T> list, int index, int count, out List<T> result)
    {
        if (list == null || index < 0 || index >= list.Count || count <= 0)
        {
            result = null;
            return false;
        }

        count = Math.Min(count, list.Count - index);
        result = list.GetRange(index, count);
        return true;
    }
}