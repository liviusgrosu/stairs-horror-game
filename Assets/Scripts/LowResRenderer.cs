using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LowResRenderer : MonoBehaviour
{
    [Range(0.05f, 1f)]
    [SerializeField] private float _resolutionScale = 0.2f;

    private UniversalRenderPipelineAsset _urpAsset;
    private float _originalScale;
    private UpscalingFilterSelection _originalFilter;

    private void Awake()
    {
        _urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (_urpAsset == null)
        { 
            Debug.LogWarning("[LowResRenderer] Project is not using URP.");
            return;
        }

        _originalScale  = _urpAsset.renderScale;
        _originalFilter = _urpAsset.upscalingFilter;

        _urpAsset.renderScale     = _resolutionScale;
        _urpAsset.upscalingFilter = UpscalingFilterSelection.Point;
    }

    private void OnDestroy()
    {
        if (_urpAsset == null) return;

        _urpAsset.renderScale     = _originalScale;
        _urpAsset.upscalingFilter = _originalFilter;
    }
}
