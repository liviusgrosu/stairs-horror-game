using System.Collections.Generic;
using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    private static readonly List<RespawnPoint> Points = new();

    private void OnEnable()
    {
        if (!Points.Contains(this))
        {
            Points.Add(this);
        }
    }

    private void OnDisable()
    {
        Points.Remove(this);
    }

    public static RespawnPoint GetRandom()
    {
        if (Points.Count == 0) return null;
        return Points[Random.Range(0, Points.Count)];
    }
}
