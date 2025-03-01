using UnityEngine;
using UnityEditor;
using System.IO;

public class BaitPrefabGenerator : EditorWindow
{
    [MenuItem("Tools/Fish Market/Generate Bait Prefabs")]
    public static void GenerateBaitPrefabs()
    {
        // Create base directories
        string prefabPath = "Assets/Prefabs/Bait";
        string resourcePath = "Assets/Resources/Bait";
        
        CreateDirectoryIfNeeded(prefabPath);
        CreateDirectoryIfNeeded(resourcePath);

        // Generate each bait type
        CreateWormBait();
        CreateMinnowBait();
        CreateShrimpBait();
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("Generated all bait prefabs successfully!");
    }

    private static void CreateWormBait()
    {
        var bait = ScriptableObject.CreateInstance<Bait>();
        bait.baitName = "Worm";
        bait.attractionRadius = 2f;
        bait.attractionStrength = 1f;
        bait.targetFishSizes = new FishSize[] { FishSize.Tiny, FishSize.Small };
        bait.sizeSpecificAttractionMultiplier = 1.2f;
        bait.baitColor = new Color(0.6f, 0.4f, 0.2f); // Brown
        bait.size = 0.3f;

        SaveBaitAndCreatePrefab(bait);
    }

    private static void CreateMinnowBait()
    {
        var bait = ScriptableObject.CreateInstance<Bait>();
        bait.baitName = "Minnow";
        bait.attractionRadius = 3f;
        bait.attractionStrength = 1.5f;
        bait.targetFishSizes = new FishSize[] { FishSize.Small, FishSize.Medium };
        bait.sizeSpecificAttractionMultiplier = 1.5f;
        bait.baitColor = new Color(0.7f, 0.7f, 0.8f); // Silver
        bait.size = 0.4f;

        SaveBaitAndCreatePrefab(bait);
    }

    private static void CreateShrimpBait()
    {
        var bait = ScriptableObject.CreateInstance<Bait>();
        bait.baitName = "Shrimp";
        bait.attractionRadius = 4f;
        bait.attractionStrength = 2f;
        bait.targetFishSizes = new FishSize[] { FishSize.Medium, FishSize.Large };
        bait.sizeSpecificAttractionMultiplier = 1.8f;
        bait.baitColor = new Color(1f, 0.6f, 0.5f); // Pink
        bait.size = 0.5f;

        SaveBaitAndCreatePrefab(bait);
    }

    private static void SaveBaitAndCreatePrefab(Bait baitData)
    {
        // Save the bait scriptable object
        string assetPath = $"Assets/Resources/Bait/{baitData.baitName}Bait.asset";
        AssetDatabase.CreateAsset(baitData, assetPath);

        // Create the bait game object
        GameObject baitObject = new GameObject(baitData.baitName);
        
        // Add required components
        var baitComponent = baitObject.AddComponent<BaitObject>();
        baitComponent.baitData = baitData;
        
        var collider = baitObject.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        collider.isTrigger = true;
        
        // Create sprite child object
        GameObject spriteObject = new GameObject("Sprite");
        spriteObject.transform.SetParent(baitObject.transform);
        spriteObject.transform.localPosition = Vector3.zero;
        
        // Add sprite renderer and create sprite
        var spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
        
        // Create sprite texture
        Texture2D texture = new Texture2D(32, 32);
        Vector2 center = new Vector2(16, 16);
        float radius = 14f;
        
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance < radius ? baitData.baitColor : Color.clear);
            }
        }
        texture.Apply();
        
        // Create and assign sprite
        Sprite baitSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), 
            new Vector2(0.5f, 0.5f));
        spriteRenderer.sprite = baitSprite;
        
        // Set layer to Bait
        int baitLayer = LayerMask.NameToLayer("Bait");
        if (baitLayer == -1)
        {
            Debug.LogWarning("Bait layer not found. Please create it in Edit > Project Settings > Tags and Layers");
        }
        else
        {
            baitObject.layer = baitLayer;
        }

        // Save the prefab
        string prefabPath = $"Assets/Prefabs/Bait/{baitData.baitName}Bait.prefab";
        PrefabUtility.SaveAsPrefabAsset(baitObject, prefabPath);
        
        // Clean up the scene object
        Object.DestroyImmediate(baitObject);
        
        Debug.Log($"Created {baitData.baitName} bait prefab and scriptable object");
    }

    private static void CreateDirectoryIfNeeded(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
} 