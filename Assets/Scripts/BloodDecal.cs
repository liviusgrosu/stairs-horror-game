using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class BloodDecal : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock block;
    private float startScale;
    private float endScale;
    private float lifetime;
    private float elapsed;

    public void Play(float startScale, float endScale, float lifetime)
    {
        this.startScale = startScale;
        this.endScale = endScale;
        this.lifetime = Mathf.Max(0.0001f, lifetime);

        meshRenderer = GetComponent<MeshRenderer>();
        block = new MaterialPropertyBlock();
        Apply(0f);
    }

    public void Pause() => enabled = false;

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        Apply(elapsed / lifetime);
    }

    private void Apply(float t)
    {
        var scale = Mathf.Lerp(startScale, endScale, t);
        transform.localScale = new Vector3(scale, scale, scale);

        meshRenderer.GetPropertyBlock(block);
        block.SetColor(BaseColorId, new Color(1f, 1f, 1f, 1f - t));
        meshRenderer.SetPropertyBlock(block);
    }
}
