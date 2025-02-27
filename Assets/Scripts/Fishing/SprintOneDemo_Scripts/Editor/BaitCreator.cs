using UnityEngine;
using UnityEditor;
using System.IO;

public class BaitCreator : EditorWindow
{
    [MenuItem("Tools/Fish Market/Create Bait Types")]
    public static void CreateBaitTypes()
    {
        string basePath = "Assets/Resources/Bait";
        
        // Create directory if it doesn't exist
        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
        }

        // Create different bait types
        CreateWormBait(basePath);
        CreateMinnowBait(basePath);
        CreateShrimpBait(basePath);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // Create prefabs after creating the bait types
        CreateBaitPrefabs();
    }
    
    private static void CreateWormBait(string basePath)
    {
        var bait = ScriptableObject.CreateInstance<Bait>();
        
        bait.baitName = "Worm";
        bait.attractionRadius = 2f;
        bait.attractionStrength = 1.2f;
        bait.targetFishSizes = new FishSize[] { FishSize.Tiny, FishSize.Small };
        bait.sizeSpecificAttractionMultiplier = 1.5f;
        bait.baitColor = new Color(0.7f, 0.4f, 0.3f); // Brown
        bait.size = 0.25f;
        
        string path = Path.Combine(basePath, "WormBait.asset");
        AssetDatabase.CreateAsset(bait, path);
    }
    
    private static void CreateMinnowBait(string basePath)
    {
        var bait = ScriptableObject.CreateInstance<Bait>();
        
        bait.baitName = "Minnow";
        bait.attractionRadius = 3f;
        bait.attractionStrength = 1.5f;
        bait.targetFishSizes = new FishSize[] { FishSize.Tiny, FishSize.Small };
        bait.sizeSpecificAttractionMultiplier = 1.8f;
        bait.baitColor = new Color(0.8f, 0.8f, 0.9f); // Silver
        bait.size = 0.35f;
        
        string path = Path.Combine(basePath, "MinnowBait.asset");
        AssetDatabase.CreateAsset(bait, path);
    }
    
    private static void CreateShrimpBait(string basePath)
    {
        var bait = ScriptableObject.CreateInstance<Bait>();
        
        bait.baitName = "Shrimp";
        bait.attractionRadius = 4f;
        bait.attractionStrength = 2f;
        bait.targetFishSizes = new FishSize[] { FishSize.Large, FishSize.Huge };
        bait.sizeSpecificAttractionMultiplier = 2f;
        bait.baitColor = new Color(1f, 0.6f, 0.6f); // Pink
        bait.size = 0.4f;
        
        string path = Path.Combine(basePath, "ShrimpBait.asset");
        AssetDatabase.CreateAsset(bait, path);
    }
    
    [MenuItem("Tools/Fish Market/Create Bait Prefabs")]
    public static void CreateBaitPrefabs()
    {
        string prefabPath = "Assets/Prefabs/Bait";
        string baitPath = "Assets/Resources/Bait";
        
        // Create directory if it doesn't exist
        if (!Directory.Exists(prefabPath))
        {
            Directory.CreateDirectory(prefabPath);
        }
        
        // Ensure Bait layer exists
        CreateBaitLayer();
        
        // Create prefabs for each bait type
        CreateBaitPrefab("Worm", "WormBait", prefabPath, baitPath);
        CreateBaitPrefab("Minnow", "MinnowBait", prefabPath, baitPath);
        CreateBaitPrefab("Shrimp", "ShrimpBait", prefabPath, baitPath);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    
    private static void CreateBaitLayer()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");
        
        // Add Bait layer if it doesn't exist
        bool hasLayer = false;
        int firstEmptySlot = -1;
        
        // Check existing layers (starting from 8 since 0-7 are Unity's built-in layers)
        for (int i = 8; i < layersProp.arraySize; i++)
        {
            SerializedProperty layerSP = layersProp.GetArrayElementAtIndex(i);
            if (layerSP.stringValue == "Bait")
            {
                hasLayer = true;
                break;
            }
            // Keep track of the first empty slot we find
            if (firstEmptySlot == -1 && string.IsNullOrEmpty(layerSP.stringValue))
            {
                firstEmptySlot = i;
            }
        }
        
        // If we don't have the layer and we found an empty slot, create it
        if (!hasLayer && firstEmptySlot != -1)
        {
            SerializedProperty layerSP = layersProp.GetArrayElementAtIndex(firstEmptySlot);
            layerSP.stringValue = "Bait";
            tagManager.ApplyModifiedProperties();
            Debug.Log($"Created Bait layer in slot {firstEmptySlot}");
        }
        else if (!hasLayer)
        {
            Debug.LogError("Could not create Bait layer. Please create it manually in the Tags and Layers settings:\n" +
                         "1. Edit > Project Settings > Tags and Layers\n" +
                         "2. Under 'Layers', find an empty slot (User Layer 8-31)\n" +
                         "3. Type 'Bait' into the empty slot");
        }
    }
    
    private static void CreateBaitPrefab(string baitName, string baitAssetName, string prefabPath, string baitPath)
    {
        // Check if prefab already exists
        string prefabFilePath = Path.Combine(prefabPath, baitName + "Bait.prefab");
        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFilePath);
        
        // Create the main GameObject
        GameObject baitObject = new GameObject(baitName + "Bait");
        
        // Add required components
        var collider = baitObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        
        var baitComponent = baitObject.AddComponent<BaitObject>();
        
        // Load and assign bait data
        string baitAssetPath = Path.Combine(baitPath, baitAssetName + ".asset");
        var baitData = AssetDatabase.LoadAssetAtPath<Bait>(baitAssetPath);
        if (baitData != null)
        {
            baitComponent.baitData = baitData;
        }
        
        // Create or update the prefab
        bool success;
        if (existingPrefab != null)
        {
            success = PrefabUtility.SaveAsPrefabAssetAndConnect(baitObject, prefabFilePath, InteractionMode.AutomatedAction) != null;
        }
        else
        {
            success = PrefabUtility.SaveAsPrefabAsset(baitObject, prefabFilePath, out success);
        }
        
        // Clean up the temporary object
        Object.DestroyImmediate(baitObject);
        
        if (success)
        {
            Debug.Log($"Created/Updated bait prefab: {baitName}");
        }
        else
        {
            Debug.LogError($"Failed to create/update bait prefab: {baitName}");
        }
    }
} 