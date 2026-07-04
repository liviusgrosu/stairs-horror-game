using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FurnaceReverbTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var footsteps = other.GetComponentInChildren<CharacterFootsteps>();
        if (footsteps) footsteps.SuppressReverb = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var footsteps = other.GetComponentInChildren<CharacterFootsteps>();
        if (footsteps) footsteps.SuppressReverb = false;
    }
}
