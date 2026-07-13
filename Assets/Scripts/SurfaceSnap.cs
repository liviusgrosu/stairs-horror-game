using UnityEngine;

public class SurfaceSnap : MonoBehaviour
{
    [SerializeField] private bool snapSelf = false;
    [SerializeField] private bool snapChildren = true;
    [SerializeField] private LayerMask surfaceMask = ~0;
    [SerializeField] private float castStartHeight = 2f;
    [SerializeField] private float castDistance = 20f;
    [SerializeField] private float surfaceOffset = 0.01f;
    [SerializeField] private bool alignToNormal = true;

    private void Start() => Snap();

    [ContextMenu("Snap To Surface")]
    public void Snap()
    {
        if (snapSelf)
            SnapTransform(transform);

        if (snapChildren)
            for (var i = 0; i < transform.childCount; i++)
                SnapTransform(transform.GetChild(i));
    }

    private void SnapTransform(Transform target)
    {
        var origin = target.position + Vector3.up * castStartHeight;
        if (!Physics.Raycast(origin, Vector3.down, out var hit, castStartHeight + castDistance, surfaceMask, QueryTriggerInteraction.Ignore))
            return;

        target.position = hit.point + hit.normal * surfaceOffset;

        if (alignToNormal)
            target.rotation = Quaternion.FromToRotation(target.up, hit.normal) * target.rotation;
    }
}
