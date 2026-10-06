#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Creates the "Snow" object in the open scene: a SnowController with three pre-configured
// particle emitters (far, middle, near) plus the generated flake texture and material under
// Assets/Snow. After creation everything is a normal scene object/asset, so the particle
// systems can be hand-tuned in the inspector. If a SnowController already exists, it asks
// before replacing it.
public static class SnowEditorTools
{
    private const string Folder = "Assets/Snow";
    private const string TexturePath = Folder + "/SnowFlake.png";
    private const string MaterialPath = Folder + "/Snow.mat";

    // Camera is orthographic size 5 (10 units tall, ~17.8 wide at 16:9), standing view at
    // y = 0, crouched view at y = -10. The emission boxes cover both views, are widened by the
    // parallax range (background +-18, Trench +-5), and extend upwards by the distance a flake
    // falls during its lifetime so the density stays even at the top of the screen.
    private struct LayerSettings
    {
        public string name;
        public string sortingLayer;
        public int sortingOrder;
        public float z;
        public Vector2 boxSize;
        public float centerY;
        public float rate;
        public int maxParticles;
        public Vector2 lifetime;
        public Vector2 size;
        public Vector2 fallSpeed;
        public float noiseStrength;
        public Color color;
    }

    private static readonly LayerSettings FarSettings = new LayerSettings
    {
        name = "Snow_Far",
        // In front of the background/mountains, behind the ground, enemies and the Fog.
        sortingLayer = "Background",
        sortingOrder = 100,
        boxSize = new Vector2(58f, 40f),
        centerY = 1f,
        rate = 87f,
        maxParticles = 1500,
        lifetime = new Vector2(10f, 14f),
        size = new Vector2(0.03f, 0.06f),
        fallSpeed = new Vector2(0.35f, 0.6f),
        noiseStrength = 0.15f,
        color = new Color(0.85f, 0.88f, 0.93f, 0.55f),
    };

    private static readonly LayerSettings MiddleSettings = new LayerSettings
    {
        name = "Snow_Middle",
        // Same sorting layer as the Fog (MiddleUI, order 2), just in front of it.
        sortingLayer = "MiddleUI",
        sortingOrder = 3,
        boxSize = new Vector2(47f, 40f),
        centerY = 1f,
        rate = 37f,
        maxParticles = 600,
        lifetime = new Vector2(8f, 12f),
        size = new Vector2(0.05f, 0.1f),
        fallSpeed = new Vector2(0.55f, 0.9f),
        noiseStrength = 0.25f,
        color = new Color(0.93f, 0.95f, 0.97f, 0.7f),
    };

    private static readonly LayerSettings NearSettings = new LayerSettings
    {
        name = "Snow_Near",
        // FrontUI so it is drawn over the Trench (order -1) and stays visible when crouching.
        // Order 1 is shared with the Weapon and License sprites (z = 0): the slightly negative
        // z puts the snow in front of them, while Death (order 2) still covers it.
        sortingLayer = "FrontUI",
        sortingOrder = 1,
        z = -0.5f,
        boxSize = new Vector2(34f, 40f),
        centerY = 1f,
        rate = 14f,
        maxParticles = 300,
        lifetime = new Vector2(7f, 10f),
        size = new Vector2(0.09f, 0.16f),
        fallSpeed = new Vector2(0.8f, 1.3f),
        noiseStrength = 0.35f,
        color = new Color(1f, 1f, 1f, 0.85f),
    };

    [MenuItem("Tools/WW2/Create Snow")]
    private static void CreateSnow()
    {
        Material material = GetOrCreateMaterial();
        if (material == null) return;

        var existing = Object.FindFirstObjectByType<SnowController>(FindObjectsInactive.Include);
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "Create Snow",
                "The scene already has a Snow object (" + existing.name + "). Replace it with a freshly generated one? Any hand-tuned particle settings on it will be lost.",
                "Replace",
                "Cancel");
            if (!replace)
            {
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        var root = new GameObject("Snow");
        var controller = root.AddComponent<SnowController>();
        ParticleSystem far = CreateLayer(root.transform, material, FarSettings);
        ParticleSystem middle = CreateLayer(root.transform, material, MiddleSettings);
        ParticleSystem near = CreateLayer(root.transform, material, NearSettings);

        var so = new SerializedObject(controller);
        so.FindProperty("farLayer.particles").objectReferenceValue = far;
        so.FindProperty("middleLayer.particles").objectReferenceValue = middle;
        so.FindProperty("nearLayer.particles").objectReferenceValue = near;
        so.ApplyModifiedPropertiesWithoutUndo();

        Undo.RegisterCreatedObjectUndo(root, "Create Snow");
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
    }

    private static ParticleSystem CreateLayer(Transform parent, Material material, LayerSettings s)
    {
        var go = new GameObject(s.name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, s.centerY, s.z);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(s.lifetime.x, s.lifetime.y);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(s.size.x, s.size.y);
        main.startColor = s.color;
        main.gravityModifier = 0f;
        // Local space: SnowController moves the whole layer for parallax, flakes in the air follow.
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = s.maxParticles;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = s.rate;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position = Vector3.zero;
        shape.rotation = Vector3.zero;
        shape.scale = new Vector3(s.boxSize.x, s.boxSize.y, 0f);

        // All three axes must use the same mode; X (wind) is overwritten by SnowController at runtime.
        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(-s.fallSpeed.y, -s.fallSpeed.x);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        // Slow, low-frequency noise gives the drifting, swaying "szallingozas" motion.
        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = s.noiseStrength;
        noise.frequency = 0.25f;
        noise.scrollSpeed = 0.15f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.15f),
                new GradientAlphaKey(1f, 0.8f),
                new GradientAlphaKey(0f, 1f),
            });
        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = material;
        renderer.sortingLayerName = s.sortingLayer;
        renderer.sortingOrder = s.sortingOrder;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        return ps;
    }

    private static Material GetOrCreateMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material != null) return material;

        Texture2D texture = GetOrCreateTexture();

        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
        {
            Debug.LogError("SnowEditorTools: could not find the URP 2D Sprite-Unlit-Default shader.");
            return null;
        }

        material = new Material(shader) { mainTexture = texture };
        AssetDatabase.CreateAsset(material, MaterialPath);
        AssetDatabase.SaveAssets();
        return material;
    }

    // Soft round flake: white with a smooth radial alpha falloff, so small far flakes read as
    // dots and the bigger near ones look slightly out of focus.
    private static Texture2D GetOrCreateTexture()
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        if (texture != null) return texture;

        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder("Assets", "Snow");
        }

        const int size = 64;
        float radius = size * 0.5f;
        var generated = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius)) / radius;
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = alpha * alpha * (3f - 2f * alpha);
                generated.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        File.WriteAllBytes(TexturePath, generated.EncodeToPNG());
        Object.DestroyImmediate(generated);
        AssetDatabase.ImportAsset(TexturePath);

        var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = true;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
    }
}
#endif
