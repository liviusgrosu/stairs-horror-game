using UnityEngine;

[System.Serializable]
public class EnemySpawnProfile
{
    public bool StartEngaged;
    public float RunSpeed = 6f;
    public float EngageDistanceScale = 1f;
    public bool InstantKill;
}
