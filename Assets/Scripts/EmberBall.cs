using UnityEngine;

// A collectible ember ball. Picked up via the player's Pickup raycast, then
// consumed when lighting a Furnace.
public class EmberBall : MonoBehaviour
{
    public void Collect()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.AddEmberBall();
        }

        Destroy(gameObject);
    }
}
