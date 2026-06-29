using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class LowResSetupTool
{
    [MenuItem("Tools/Setup Low-Res Renderer")]
    public static void SetupLowResRenderer()
    {
        // ── 1. Find the Main Camera in the scene ─────────────────────────────
        Camera gameCamera = Camera.main;
        if (!gameCamera)
        {
            Debug.LogError("[LowResSetup] No camera tagged 'MainCamera' found in the scene.");
            return;
        }

        // ── 2. Strip UI from the game camera's culling mask ──────────────────
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0)
            gameCamera.cullingMask &= ~(1 << uiLayer);

        // ── 3. Create (or reuse) a UI Camera ─────────────────────────────────
        const string uiCamName = "UI Camera";
        Camera uiCamera = GameObject.Find(uiCamName)?.GetComponent<Camera>();
        if (!uiCamera)
        {
            var uiCamGo = new GameObject(uiCamName);
            uiCamera = uiCamGo.AddComponent<Camera>();
        }

        uiCamera.clearFlags    = CameraClearFlags.Depth;
        uiCamera.cullingMask   = uiLayer >= 0 ? (1 << uiLayer) : 0;
        uiCamera.depth         = gameCamera.depth + 1;
        uiCamera.orthographic  = true;
        uiCamera.targetTexture = null;

        // Position it on top of the game camera (doesn't matter for overlay, but keeps hierarchy tidy)
        uiCamera.transform.SetPositionAndRotation(gameCamera.transform.position, gameCamera.transform.rotation);

        // ── 4. Find or create a full-screen RawImage on the Canvas ───────────
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (!canvas)
        {
            Debug.LogError("[LowResSetup] No Canvas found in the scene.");
            return;
        }

        canvas.renderMode  = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = uiCamera;
        canvas.planeDistance = 1f;

        const string rawImageName = "Low Res Display";
        RawImage rawImage = canvas.transform.Find(rawImageName)?.GetComponent<RawImage>();
        if (!rawImage)
        {
            var go = new GameObject(rawImageName);
            go.transform.SetParent(canvas.transform, false);

            rawImage = go.AddComponent<RawImage>();

            // Stretch to fill the canvas
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.one;
            rt.offsetMin        = Vector2.zero;
            rt.offsetMax        = Vector2.zero;

            // Put it behind all other UI (sibling index 0)
            go.transform.SetSiblingIndex(0);
        }

        // ── 5. Create (or reuse) the LowResRenderer controller ───────────────
        const string controllerName = "Low Res Controller";
        var controllerGo = GameObject.Find(controllerName);
        if (!controllerGo)
            controllerGo = new GameObject(controllerName);

        var renderer = controllerGo.GetComponent<LowResRenderer>()
                    ?? controllerGo.AddComponent<LowResRenderer>();

        // Use SerializedObject so the fields actually save properly
        var so = new SerializedObject(renderer);
        so.FindProperty("_gameCamera").objectReferenceValue   = gameCamera;
        so.FindProperty("_displayTarget").objectReferenceValue = rawImage;
        so.ApplyModifiedProperties();

        // ── 6. Mark scene dirty so Unity knows to save ────────────────────────
        EditorUtility.SetDirty(canvas);
        EditorUtility.SetDirty(uiCamera.gameObject);
        EditorUtility.SetDirty(controllerGo);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[LowResSetup] Done! Save the scene and hit Play to see the effect.\n" +
                  $"  Game camera : {gameCamera.name} (CullingMask UI removed)\n" +
                  $"  UI camera   : {uiCamera.name}\n" +
                  $"  RawImage    : {rawImage.name} on Canvas '{canvas.name}'");

        Selection.activeGameObject = controllerGo;
    }
}
