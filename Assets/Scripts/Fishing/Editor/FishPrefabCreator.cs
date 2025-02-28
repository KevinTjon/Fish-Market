using UnityEngine;
using UnityEditor;
using System.IO;

public class FishPrefabCreator : EditorWindow
{
    private static readonly Color TINY_FISH_COLOR = new Color(0.75f, 0.75f, 0.75f); // Light gray for tiny fish
    private static readonly Color SMALL_FISH_COLOR = new Color(0f, 1f, 1f); // Cyan for small fish
    private static readonly Color MEDIUM_FISH_COLOR = new Color(0f, 0f, 1f); // Blue for medium fish
    private static readonly Color LARGE_FISH_COLOR = new Color(1f, 0f, 1f); // Magenta for large fish
    private static readonly Color HUGE_FISH_COLOR = new Color(1f, 0f, 0f); // Red for huge fish

    [MenuItem("Tools/Fish Market/Create Fish Prefabs")]
    public static void CreateFishPrefabs()
    {
        string prefabPath = "Assets/Prefabs/Fish";
        string behaviorPath = "Assets/Resources/FishBehaviors";
        
        // Create directories if they don't exist
        if (!Directory.Exists(prefabPath))
        {
            Directory.CreateDirectory(prefabPath);
        }

        // Ensure required layers and tags exist
        CreateRequiredLayersAndTags();

        // Create fish prefabs for each behavior
        CreateFishPrefab("TinyFish", "TinyFishBehavior", prefabPath, behaviorPath, TINY_FISH_COLOR);
        CreateFishPrefab("SmallFish", "SmallFishBehavior", prefabPath, behaviorPath, SMALL_FISH_COLOR);
        CreateFishPrefab("MediumFish", "MediumFishBehavior", prefabPath, behaviorPath, MEDIUM_FISH_COLOR);
        CreateFishPrefab("LargeFish", "LargeFishBehavior", prefabPath, behaviorPath, LARGE_FISH_COLOR);
        CreateFishPrefab("HugeFish", "HugeFishBehavior", prefabPath, behaviorPath, HUGE_FISH_COLOR);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateRequiredLayersAndTags()
    {
        // Get the TagManager
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        // Add Fish tag if it doesn't exist
        bool hasTag = false;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals("Fish")) { hasTag = true; break; }
        }

        if (!hasTag)
        {
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
            newTag.stringValue = "Fish";
        }

        // Add Hookable layer if it doesn't exist (using layer 8 if available)
        bool hasLayer = false;
        for (int i = 8; i < layersProp.arraySize; i++)
        {
            SerializedProperty layerSP = layersProp.GetArrayElementAtIndex(i);
            if (layerSP.stringValue == "Hookable") { hasLayer = true; break; }
        }

        if (!hasLayer)
        {
            SerializedProperty layerSP = layersProp.GetArrayElementAtIndex(8);
            if (string.IsNullOrEmpty(layerSP.stringValue))
            {
                layerSP.stringValue = "Hookable";
            }
            else
            {
                Debug.LogError("Could not create Hookable layer. Please create it manually in the Tags and Layers settings.");
            }
        }

        // Apply the changes to the tag manager
        tagManager.ApplyModifiedProperties();
    }

    private static void CreateFishPrefab(string fishName, string behaviorAssetName, string prefabPath, string behaviorPath, Color defaultColor)
    {
        // Check if prefab already exists
        string prefabFilePath = Path.Combine(prefabPath, fishName + ".prefab");
        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFilePath);
        Sprite existingSprite = null;
        SpriteRenderer existingSpriteRenderer = null;
        
        // If prefab exists, store its sprite
        if (existingPrefab != null)
        {
            Transform spriteTransform = existingPrefab.transform.Find("Sprite");
            if (spriteTransform != null)
            {
                existingSpriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
                if (existingSpriteRenderer != null)
                {
                    existingSprite = existingSpriteRenderer.sprite;
                }
            }
        }
        
        // Create the main GameObject
        GameObject fishObject = new GameObject(fishName);
        
        // Add required components
        var fishRigidbody = fishObject.AddComponent<Rigidbody2D>();
        fishRigidbody.gravityScale = 0f;
        fishRigidbody.drag = 0.5f;
        fishRigidbody.angularDrag = 0.5f;
        fishRigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        var fishCollider = fishObject.AddComponent<CircleCollider2D>();
        fishCollider.isTrigger = false;
        
        // Create sprite child object
        GameObject spriteObject = new GameObject("Sprite");
        spriteObject.transform.SetParent(fishObject.transform);
        spriteObject.transform.localPosition = Vector3.zero;
        
        // Add sprite renderer
        var spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
        
        // Load behavior to get color
        string behaviorAssetPath = Path.Combine(behaviorPath, behaviorAssetName + ".asset");
        var fishBehavior = AssetDatabase.LoadAssetAtPath<FishBehavior>(behaviorAssetPath);
        
        // Use existing sprite if available, otherwise create temporary one
        if (existingSprite != null)
        {
            spriteRenderer.sprite = existingSprite;
            // Preserve sorting layer and order if they exist
            if (existingSpriteRenderer != null)
            {
                spriteRenderer.sortingLayerID = existingSpriteRenderer.sortingLayerID;
                spriteRenderer.sortingOrder = existingSpriteRenderer.sortingOrder;
            }
        }
        else
        {
            Color fishColor = fishBehavior != null ? fishBehavior.fishColor : defaultColor;
            
            // Create a temporary colored sprite
            Texture2D texture = new Texture2D(32, 32);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, fishColor);
                }
            }
            texture.Apply();
            
            Sprite fishSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            spriteRenderer.sprite = fishSprite;
        }
        
        // Add FishHookable component
        var fishHookable = fishObject.AddComponent<FishHookable>();
        
        // Add FishMovement component
        var fishMovement = fishObject.AddComponent<FishMovement>();
        
        // Add FishSchooling component
        var fishSchooling = fishObject.AddComponent<FishSchooling>();
        
        // Load and assign behavior
        if (fishBehavior != null)
        {
            fishHookable.behavior = fishBehavior;
        }
        
        // Set up layers and tags
        fishObject.layer = LayerMask.NameToLayer("Hookable");
        fishObject.tag = "Fish";
        
        // Scale the fish based on behavior size
        if (fishBehavior != null)
        {
            fishObject.transform.localScale = Vector3.one * fishBehavior.size;
            fishCollider.radius = 0.5f * fishBehavior.size; // Adjust collider size
        }
        
        // Create or update the prefab
        bool success = false;
        if (existingPrefab != null)
        {
            success = PrefabUtility.SaveAsPrefabAssetAndConnect(fishObject, prefabFilePath, InteractionMode.AutomatedAction) != null;
        }
        else
        {
            success = PrefabUtility.SaveAsPrefabAsset(fishObject, prefabFilePath, out success);
        }
        
        // Clean up the temporary object
        Object.DestroyImmediate(fishObject);
        
        if (success)
        {
            Debug.Log($"Created/Updated fish prefab: {fishName}");
        }
        else
        {
            Debug.LogError($"Failed to create/update fish prefab: {fishName}");
        }
    }
} 