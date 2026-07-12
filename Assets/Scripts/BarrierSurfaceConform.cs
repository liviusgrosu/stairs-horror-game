using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[AddComponentMenu("VFX/Surface Conforming Magic Beam")]
public sealed class BarrierSurfaceConform : MonoBehaviour
{
    [Header("Cylinder")]
    [Min(0.01f)] public float radius = 1.5f;
    [Min(0.01f)] public float height = 8f;
    [Range(3, 256)] public int radialSegments = 96;
    [Range(1, 32)] public int heightSegments = 8;

    [Header("Surface Conform")]
    [Tooltip("The beam samples surfaces along its local down direction.")]
    [Min(0f)] public float castStartHeight = 3f;
    [Min(0.01f)] public float castDistance = 10f;
    [Min(0f)] public float surfaceOffset = 0.015f;
    public LayerMask surfaceMask = ~0;
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Live Preview")]
    public bool previewInEditMode = true;
    public bool updateAtRuntime = true;
    [Min(0.02f)] public float rebuildInterval = 0.1f;

    [Header("Animation Layer")]
    [Tooltip("Multiplies the material's Upward Speed. Negative values scroll downward.")]
    [Range(-3f, 3f)] public float scrollSpeedMultiplier = 1f;
    [Tooltip("Offsets this layer in the animation cycle so stacked beams do not move in sync.")]
    [Range(0f, 1f)] public float scrollPhase;

    private readonly RaycastHit[] _hits = new RaycastHit[16];
    private static readonly int LayerSpeedId = Shader.PropertyToID("_LayerSpeed");
    private static readonly int LayerPhaseId = Shader.PropertyToID("_LayerPhase");
    private Mesh _mesh;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private MaterialPropertyBlock _propertyBlock;
    private float _nextRuntimeRebuild;

#if UNITY_EDITOR
    private double _nextEditorRebuild;
#endif

    private void OnEnable()
    {
        CacheComponents();
        Build();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;
        }
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= EditorUpdate;
#endif
        ReleaseMesh();
    }

    private void OnDestroy()
    {
#if UNITY_EDITOR
        EditorApplication.update -= EditorUpdate;
#endif
        ReleaseMesh();
    }

    private void OnValidate()
    {
        radius = Mathf.Max(0.01f, radius);
        height = Mathf.Max(0.01f, height);
        radialSegments = Mathf.Clamp(radialSegments, 3, 256);
        heightSegments = Mathf.Clamp(heightSegments, 1, 32);
        castStartHeight = Mathf.Max(0f, castStartHeight);
        castDistance = Mathf.Max(0.01f, castDistance);
        rebuildInterval = Mathf.Max(0.02f, rebuildInterval);
        scrollSpeedMultiplier = Mathf.Clamp(scrollSpeedMultiplier, -3f, 3f);
        scrollPhase = Mathf.Repeat(scrollPhase, 1f);

        if (isActiveAndEnabled)
            Build();
    }

    private void Update()
    {
        if (!Application.isPlaying || !updateAtRuntime || Time.unscaledTime < _nextRuntimeRebuild)
            return;

        Build();
        _nextRuntimeRebuild = Time.unscaledTime + rebuildInterval;
    }

#if UNITY_EDITOR
    private void EditorUpdate()
    {
        if (this == null || Application.isPlaying || !isActiveAndEnabled)
        {
            EditorApplication.update -= EditorUpdate;
            return;
        }

        // Prefab assets do not need continuous updates. Prefab Stage and scene instances do.
        if (!gameObject.scene.IsValid())
            return;

        SceneView.RepaintAll();

        if (!previewInEditMode || EditorApplication.timeSinceStartup < _nextEditorRebuild)
            return;

        Build();
        _nextEditorRebuild = EditorApplication.timeSinceStartup + rebuildInterval;
    }
#endif

    [ContextMenu("Rebuild Beam Mesh")]
    public void Build()
    {
        CacheComponents();
        EnsureMesh();
        Physics.SyncTransforms();

        var columns = radialSegments + 1;
        var rows = heightSegments + 1;
        var vertices = new Vector3[columns * rows];
        var normals = new Vector3[columns * rows];
        var uvs = new Vector2[columns * rows];
        var triangles = new int[radialSegments * heightSegments * 6];
        var localUp = Vector3.up;
        var worldUp = transform.up.normalized;

        for (var column = 0; column < columns; column++)
        {
            var u = (float)column / radialSegments;
            var angle = u * Mathf.PI * 2f;
            var radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            var baseLocal = radial * radius;
            var sampleWorld = transform.TransformPoint(baseLocal);
            var rayOrigin = sampleWorld + worldUp * castStartHeight;

            if (TryFindSurface(rayOrigin, -worldUp, castStartHeight + castDistance, out var hit))
            {
                var offsetPoint = hit.point + hit.normal.normalized * surfaceOffset;
                baseLocal = transform.InverseTransformPoint(offsetPoint);
            }

            for (var row = 0; row < rows; row++)
            {
                var v = (float)row / heightSegments;
                var index = row * columns + column;
                vertices[index] = baseLocal + localUp * (height * v);
                normals[index] = radial;
                uvs[index] = new Vector2(u, v);
            }
        }

        var triangleIndex = 0;
        for (var row = 0; row < heightSegments; row++)
        {
            for (var column = 0; column < radialSegments; column++)
            {
                var lowerLeft = row * columns + column;
                var lowerRight = lowerLeft + 1;
                var upperLeft = lowerLeft + columns;
                var upperRight = upperLeft + 1;

                triangles[triangleIndex++] = lowerLeft;
                triangles[triangleIndex++] = upperLeft;
                triangles[triangleIndex++] = upperRight;
                triangles[triangleIndex++] = lowerLeft;
                triangles[triangleIndex++] = upperRight;
                triangles[triangleIndex++] = lowerRight;
            }
        }

        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.normals = normals;
        _mesh.uv = uvs;
        _mesh.triangles = triangles;
        _mesh.RecalculateBounds();
        _meshFilter.sharedMesh = _mesh;
        ApplyAnimationProperties();
        transform.hasChanged = false;
    }

    private bool TryFindSurface(Vector3 origin, Vector3 direction, float distance, out RaycastHit closest)
    {
        var hitCount = Physics.RaycastNonAlloc(origin, direction, _hits, distance, surfaceMask, triggerInteraction);
        var closestDistance = float.PositiveInfinity;
        var found = false;
        closest = default;

        for (var i = 0; i < hitCount; i++)
        {
            var hit = _hits[i];
            if (hit.collider == null || IsOwnCollider(hit.collider.transform) || hit.distance >= closestDistance)
                continue;

            closest = hit;
            closestDistance = hit.distance;
            found = true;
        }

        return found;
    }

    private bool IsOwnCollider(Transform hitTransform)
    {
        return hitTransform == transform || hitTransform.IsChildOf(transform) || transform.IsChildOf(hitTransform);
    }

    private void CacheComponents()
    {
        if (_meshFilter == null)
            _meshFilter = GetComponent<MeshFilter>();
        if (_meshRenderer == null)
            _meshRenderer = GetComponent<MeshRenderer>();
    }

    private void ApplyAnimationProperties()
    {
        if (_meshRenderer == null)
            return;

        if (_propertyBlock == null)
            _propertyBlock = new MaterialPropertyBlock();

        _meshRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetFloat(LayerSpeedId, scrollSpeedMultiplier);
        _propertyBlock.SetFloat(LayerPhaseId, scrollPhase);
        _meshRenderer.SetPropertyBlock(_propertyBlock);
    }

    private void EnsureMesh()
    {
        if (_mesh != null)
            return;

        _mesh = new Mesh
        {
            name = "Surface Conforming Magic Beam",
            hideFlags = HideFlags.HideAndDontSave
        };
        _mesh.MarkDynamic();
    }

    private void ReleaseMesh()
    {
        if (_mesh == null)
            return;

        if (_meshFilter != null && _meshFilter.sharedMesh == _mesh)
            _meshFilter.sharedMesh = null;

        if (Application.isPlaying)
            Destroy(_mesh);
        else
            DestroyImmediate(_mesh);

        _mesh = null;
    }
}
