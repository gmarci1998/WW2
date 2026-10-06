#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Creates "Fog_Back": a copy of the existing "Fog" placed as a child of the background image
// (GameManager.backgroundSprite, i.e. Mountains), so it pans together with the background and
// the cover sprites. It sits on the Background sorting layer above the background image but
// below everything on MiddleUI (covers, enemies, front Fog), and below Snow_Far (order 100).
// It gets its own material asset (Assets/Shader/Fog_Back.mat) so it can be tuned separately
// from the front Fog. If a Fog_Back already exists, it asks before replacing it.
public static class BackFogEditorTools
{
    private const string ObjectName = "Fog_Back";
    private const string MaterialPath = "Assets/Shader/Fog_Back.mat";
    private const string SortingLayer = "Background";
    private const int SortingOrder = 50;

    [MenuItem("Tools/WW2/Create Background Fog")]
    private static void CreateBackgroundFog()
    {
        var gameManager = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        if (gameManager == null)
        {
            Debug.LogError("BackFogEditorTools: open the GameScene first (no GameManager found).");
            return;
        }

        var so = new SerializedObject(gameManager);
        var background = so.FindProperty("backgroundSprite").objectReferenceValue as GameObject;
        var backgroundRenderer = background != null ? background.GetComponent<SpriteRenderer>() : null;
        var fog = so.FindProperty("fogRenderer").objectReferenceValue as SpriteRenderer;
        if (fog == null)
        {
            GameObject fogObject = GameObject.Find("Fog");
            fog = fogObject != null ? fogObject.GetComponent<SpriteRenderer>() : null;
        }

        if (backgroundRenderer == null || fog == null || fog.sprite == null)
        {
            Debug.LogError("BackFogEditorTools: could not find the background (GameManager.backgroundSprite) or the \"Fog\" sprite.", gameManager);
            return;
        }

        SerializedProperty backFogProp = so.FindProperty("backFogRenderer");
        var existing = backFogProp.objectReferenceValue as SpriteRenderer;
        if (existing == null)
        {
            Transform found = background.transform.Find(ObjectName);
            existing = found != null ? found.GetComponent<SpriteRenderer>() : null;
        }

        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "Create Background Fog",
                "The scene already has a " + ObjectName + " object. Replace it with a fresh copy of the front Fog? (The Fog_Back material is kept.)",
                "Replace",
                "Cancel");
            if (!replace)
            {
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        Material material = GetOrCreateMaterial(fog.sharedMaterial);

        GameObject backFog = Object.Instantiate(fog.gameObject, background.transform, true);
        backFog.name = ObjectName;

        // Cover the whole background image horizontally (it is wide enough for the mouse
        // panning), keep the front Fog's height and vertical position.
        Bounds backgroundBounds = backgroundRenderer.bounds;
        Vector3 fogSpriteSize = fog.sprite.bounds.size;
        Vector3 worldScale = new Vector3(
            backgroundBounds.size.x / fogSpriteSize.x,
            fog.transform.lossyScale.y,
            1f);
        Vector3 parentScale = background.transform.lossyScale;
        backFog.transform.localScale = new Vector3(
            worldScale.x / parentScale.x,
            worldScale.y / parentScale.y,
            1f);
        backFog.transform.position = new Vector3(
            backgroundBounds.center.x,
            fog.transform.position.y,
            background.transform.position.z);

        var renderer = backFog.GetComponent<SpriteRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingLayerName = SortingLayer;
        renderer.sortingOrder = SortingOrder;

        Undo.RegisterCreatedObjectUndo(backFog, "Create Background Fog");

        so.Update();
        so.FindProperty("backFogRenderer").objectReferenceValue = renderer;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(backFog.scene);
        Selection.activeGameObject = backFog;
    }

    private static Material GetOrCreateMaterial(Material source)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material != null) return material;

        material = new Material(source) { name = "Fog_Back" };
        AssetDatabase.CreateAsset(material, MaterialPath);
        AssetDatabase.SaveAssets();
        return material;
    }
}
#endif
