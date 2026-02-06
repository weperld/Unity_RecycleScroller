using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CollectionUtils
{
    #region Find Closest Index Functions
    public static int FindClosestIndex<T>(this IEnumerable<T> positions, T pivot, Func<T, T, float> distanceFunc)
    {
        if (positions is null) return -1;
        
        return positions
            .Select((pos, index) => new { index, dist = distanceFunc(pos, pivot) })
            .OrderBy(o => o.dist)
            .First().index;
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
    
    public static List<T> GetSafeRange<T>(this List<T> list, int index, int count)
    {
        try
        {
            // GetRange 호출, 유효하지 않은 경우 예외 발생
            return list.GetRange(index, count);
        }
        catch (ArgumentOutOfRangeException)
        {
            // 예외 발생 시 빈 리스트 반환
            return new List<T>();
        }
    }
}