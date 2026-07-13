using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BloodSplatter : MonoBehaviour
{
    [SerializeField] private Transform origin;
    [SerializeField] private LayerMask surfaceMask = ~0;
    [SerializeField] private float minInterval = 1f;
    [SerializeField] private float maxInterval = 3f;
    [SerializeField] private float maxSpreadAngle = 8f;
    [SerializeField] private float rayDistance = 100f;
    [SerializeField] private float surfaceOffset = 0.01f;
    [SerializeField] private Material[] bloodMaterials;
    [SerializeField] private float startScale = 0.1f;
    [SerializeField] private float endScale = 0.3f;
    [SerializeField] private float lifetime = 2f;

    private static Mesh quadMesh;

    private readonly List<BloodDecal> decals = new List<BloodDecal>();
    private BodyEncounter bodyEncounter;

    private void OnEnable()
    {
        bodyEncounter = GetComponent<BodyEncounter>();
        if (bodyEncounter) bodyEncounter.BodyDropped += HandleBodyDropped;
        StartCoroutine(SplatterLoop());
    }

    private void OnDisable()
    {
        if (bodyEncounter) bodyEncounter.BodyDropped -= HandleBodyDropped;
    }

    private void HandleBodyDropped(BodyEncounter _)
    {
        StopAllCoroutines();

        foreach (var decal in decals)
            if (decal) decal.Pause();

        decals.Clear();
    }

    private IEnumerator SplatterLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            TrySpawn();
        }
    }

    private void TrySpawn()
    {
        if (bloodMaterials == null || bloodMaterials.Length == 0) return;

        var start = origin ? origin.position : transform.position;
        var tilt = Quaternion.AngleAxis(Random.Range(0f, maxSpreadAngle), Vector3.forward);
        var swing = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up);
        var direction = swing * tilt * Vector3.down;

        if (!Physics.Raycast(start, direction, out var hit, rayDistance, surfaceMask, QueryTriggerInteraction.Ignore))
            return;

        var material = bloodMaterials[Random.Range(0, bloodMaterials.Length)];
        SpawnDecal(hit.point + hit.normal * surfaceOffset, hit.normal, material);
    }

    private void SpawnDecal(Vector3 position, Vector3 normal, Material material)
    {
        var decal = new GameObject("Blood Decal");
        decal.transform.SetPositionAndRotation(position, DecalRotation(normal));

        decal.AddComponent<MeshFilter>().sharedMesh = GetQuadMesh();

        var meshRenderer = decal.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        var bloodDecal = decal.AddComponent<BloodDecal>();
        bloodDecal.Play(startScale, endScale, lifetime);
        decals.Add(bloodDecal);
    }

    private static Quaternion DecalRotation(Vector3 normal)
    {
        var reference = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
        return Quaternion.LookRotation(normal, reference) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
    }

    private static Mesh GetQuadMesh()
    {
        if (quadMesh) return quadMesh;

        quadMesh = new Mesh
        {
            name = "Blood Decal Quad",
            vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
            },
            uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
            },
            normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward },
            triangles = new[] { 0, 2, 1, 0, 3, 2 },
        };
        return quadMesh;
    }
}
