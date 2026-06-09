using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class EnemyPathing : MonoBehaviour
{
    public List<Transform> Points = new();
    [SerializeField] private bool _isLooping = true;
    
    private void OnDrawGizmos()
    {
        if (Points == null || Points.Count < 2) return;

        Gizmos.color = Color.green;

        for (var i = 0; i < Points.Count; i++)
        {
            var current = Points[i];
            var next = (i == Points.Count - 1) ? Points[0] : Points[i + 1];

            if (i == Points.Count - 1 && !_isLooping)
            {
                return;
            }
            
            if (current != null && next != null)
            {
                Gizmos.DrawLine(current.position, next.position);
            }
        }
    }
}