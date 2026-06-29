using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor.SceneManagement;

public static class LowResSetupTool
{
    [MenuItem("Tools/Low-Res Renderer/Revert Old Render-Texture Setup")]
    public static void RevertRenderTextureSetup()
    {
        bool changed = false;

        // Remove the UI Camera created by the old setup
        var uiCamGo = GameObject.Find("UI Camera");
        if (uiCamGo)
        {
            Undo.DestroyObjectImmediate(uiCamGo);
            Debug.Log("[LowResSetup] Removed 'UI Camera'.");
            changed = true;
        }

        // Remove the Low Res Controller
        var controllerGo = GameObject.Find("Low Res Controller");
        if (controllerGo)
        {
            Undo.DestroyObjectImmediate(controllerGo);
            Debug.Log("[LowResSetup] Removed 'Low Res Controller'.");
            changed = true;
        }

        // Remove the Low Res Display RawImage from the canvas
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas)
        {
            var display = canvas.transform.Find("Low Res Display");
            if (display)
            {
                Undo.DestroyObjectImmediate(display.gameObject);
                Debug.Log("[LowResSetup] Removed 'Low Res Display' RawImage.");
                changed = true;
            }

            // Revert canvas to Screen Space Overlay
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Undo.RecordObject(canvas, "Revert Canvas render mode");
                canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
                EditorUtility.SetDirty(canvas);
                Debug.Log("[LowResSetup] Canvas reverted to Screen Space Overlay.");
                changed = true;
            }
        }

        // Restore Main Camera culling mask to include UI
        var mainCam = Camera.main;
        if (mainCam)
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
            {
                Undo.RecordObject(mainCam, "Restore Main Camera culling mask");
                mainCam.cullingMask |= (1 << uiLayer);
                EditorUtility.SetDirty(mainCam);
                Debug.Log("[LowResSetup] Restored UI layer to Main Camera culling mask.");
                changed = true;
            }

            // Clear any render texture left on the camera
            if (mainCam.targetTexture != null)
            {
                Undo.RecordObject(mainCam, "Clear camera target texture");
                mainCam.targetTexture = null;
                EditorUtility.SetDirty(mainCam);
                Debug.Log("[LowResSetup] Cleared render texture from Main Camera.");
                changed = true;
            }
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[LowResSetup] Revert complete. Save the scene.");
        }
        else
        {
            Debug.Log("[LowResSetup] Nothing to revert.");
        }
    }
}
