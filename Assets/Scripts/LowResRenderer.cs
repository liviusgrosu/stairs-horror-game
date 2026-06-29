using UnityEngine;
using UnityEngine.UI;

public class LowResRenderer : MonoBehaviour
{
    [SerializeField] private Camera _gameCamera;
    [SerializeField] private RawImage _displayTarget;
    [Range(0.05f, 1f)]
    [SerializeField] private float _resolutionScale = 0.2f;

    private RenderTexture _rt;
    private int _lastWidth;
    private int _lastHeight;

    private void Start()
    {
        RebuildTexture();
    }

    private void Update()
    {
        if (Screen.width != _lastWidth || Screen.height != _lastHeight)
            RebuildTexture();
    }

    private void RebuildTexture()
    {
        _lastWidth = Screen.width;
        _lastHeight = Screen.height;

        int w = Mathf.Max(1, Mathf.RoundToInt(_lastWidth * _resolutionScale));
        int h = Mathf.Max(1, Mathf.RoundToInt(_lastHeight * _resolutionScale));

        if (_rt != null)
        {
            if (_gameCamera) _gameCamera.targetTexture = null;
            _rt.Release();
            Destroy(_rt);
        }

        _rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32)
        {
            filterMode = FilterMode.Point, // no bilinear blur keeps the blocky PS1 look
            antiAliasing = 1
        };
        _rt.Create();

        if (_gameCamera) _gameCamera.targetTexture = _rt;
        if (_displayTarget) _displayTarget.texture = _rt;
    }

    private void OnDestroy()
    {
        if (_gameCamera) _gameCamera.targetTexture = null;
        if (_rt) _rt.Release();
    }
}
