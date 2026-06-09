using System;
using UnityEngine;

public static class NoiseEmitter
{
    public static event Action<Vector3, float, string> OnNoise;

    public static void Emit(Vector3 position, float radius, string surfaceTag)
    {
        OnNoise?.Invoke(position, radius, surfaceTag);
    }
}
