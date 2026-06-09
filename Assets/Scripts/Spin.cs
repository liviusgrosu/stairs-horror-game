using UnityEngine;

public class Spin : MonoBehaviour
{
    public bool x;
    public bool y;
    public bool z;
    
    public float speed = 90f;

    private void Update()
    {
        var axis = new Vector3(
            x ? 1f : 0f, 
            y ? 1f : 0f, 
            z ? 1f : 0f);
        
        transform.Rotate(axis * (speed * Time.deltaTime));
    }
}
