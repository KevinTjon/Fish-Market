using UnityEngine;
using UnityEditor;
using System.IO;

public class FishBehaviorCreator : EditorWindow
{
    [MenuItem("Tools/Fish Market/Create Fish Behaviors")]
    public static void CreateFishBehaviors()
    {
        string basePath = "Assets/Resources/FishBehaviors";
        
        // Create directory if it doesn't exist
        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
        }

        // Tiny Fish (Anchovy-like)
        CreateTinyFishBehavior(basePath);
        
        // Small Fish (Sardine-like)
        CreateSmallFishBehavior(basePath);
        
        // Medium Fish (Mackerel-like)
        CreateMediumFishBehavior(basePath);
        
        // Large Fish (Tuna-like)
        CreateLargeFishBehavior(basePath);
        
        // Huge Fish (Shark-like)
        CreateHugeFishBehavior(basePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateTinyFishBehavior(string basePath)
    {
        var behavior = ScriptableObject.CreateInstance<FishBehavior>();
        
        // Properties
        behavior.size = 0.5f;
        behavior.baseSpeed = 2f;
        behavior.maxSpeed = 4f;
        behavior.turnSpeed = 3f;
        
        // Behavior Weights
        behavior.wanderWeight = 0.3f;
        behavior.flockWeight = 2f;
        behavior.avoidanceWeight = 2f;
        behavior.baitAttractionWeight = 1.5f;
        behavior.predatorAvoidanceWeight = 2f;
        
        // Detection Ranges
        behavior.visionRange = 1.5f;
        behavior.baitDetectionRange = 1f;
        behavior.predatorDetectionRange = 2f;
        behavior.personalSpace = 0.2f;
        
        // Predator Settings
        behavior.isPredator = false;
        behavior.preyChaseWeight = 0f;
        behavior.fishColor = new Color(0.75f, 0.75f, 0.75f); // Light gray for tiny fish

        string path = Path.Combine(basePath, "TinyFishBehavior.asset");
        AssetDatabase.CreateAsset(behavior, path);
    }

    private static void CreateSmallFishBehavior(string basePath)
    {
        var behavior = ScriptableObject.CreateInstance<FishBehavior>();
        
        // Properties
        behavior.size = 0.75f;
        behavior.baseSpeed = 3.5f;
        behavior.maxSpeed = 7f;
        behavior.turnSpeed = 3.5f;
        
        // Behavior Weights
        behavior.wanderWeight = 0.5f;
        behavior.flockWeight = 1.5f;
        behavior.avoidanceWeight = 1.5f;
        behavior.baitAttractionWeight = 1.2f;
        behavior.predatorAvoidanceWeight = 1.8f;
        
        // Detection Ranges
        behavior.visionRange = 4f;
        behavior.baitDetectionRange = 2.5f;
        behavior.predatorDetectionRange = 2f;
        behavior.personalSpace = 0.5f;
        
        // Predator Settings
        behavior.isPredator = false;
        behavior.preyChaseWeight = 0f;
        behavior.fishColor = new Color(0f, 1f, 1f); // Cyan for small fish

        string path = Path.Combine(basePath, "SmallFishBehavior.asset");
        AssetDatabase.CreateAsset(behavior, path);
    }

    private static void CreateMediumFishBehavior(string basePath)
    {
        var behavior = ScriptableObject.CreateInstance<FishBehavior>();
        
        // Properties
        behavior.size = 1f;
        behavior.baseSpeed = 3f;
        behavior.maxSpeed = 6f;
        behavior.turnSpeed = 3f;
        
        // Behavior Weights
        behavior.wanderWeight = 0.8f;
        behavior.flockWeight = 1f;
        behavior.avoidanceWeight = 1f;
        behavior.baitAttractionWeight = 1f;
        behavior.predatorAvoidanceWeight = 1.5f;
        
        // Detection Ranges
        behavior.visionRange = 5f;
        behavior.baitDetectionRange = 3f;
        behavior.predatorDetectionRange = 2f;
        behavior.personalSpace = 0.7f;
        
        // Predator Settings
        behavior.isPredator = false;
        behavior.preyChaseWeight = 0f;
        behavior.fishColor = new Color(0f, 0f, 1f); // Blue for medium fish

        string path = Path.Combine(basePath, "MediumFishBehavior.asset");
        AssetDatabase.CreateAsset(behavior, path);
    }

    private static void CreateLargeFishBehavior(string basePath)
    {
        var behavior = ScriptableObject.CreateInstance<FishBehavior>();
        
        // Properties
        behavior.size = 1.5f;
        behavior.baseSpeed = 2.5f;
        behavior.maxSpeed = 5f;
        behavior.turnSpeed = 2.5f;
        
        // Behavior Weights
        behavior.wanderWeight = 1f;
        behavior.flockWeight = 0.5f;
        behavior.avoidanceWeight = 0.5f;
        behavior.baitAttractionWeight = 0.8f;
        behavior.predatorAvoidanceWeight = 0.3f;
        
        // Detection Ranges
        behavior.visionRange = 6f;
        behavior.baitDetectionRange = 4f;
        behavior.predatorDetectionRange = 2f;
        behavior.personalSpace = 1f;
        
        // Predator Settings
        behavior.isPredator = true;
        behavior.preyLayer = LayerMask.GetMask("Fish");
        behavior.preyChaseWeight = 1.2f;
        behavior.preyMaxSizeRatio = 0.5f;
        behavior.fishColor = new Color(1f, 0f, 1f); // Magenta for medium predators

        string path = Path.Combine(basePath, "LargeFishBehavior.asset");
        AssetDatabase.CreateAsset(behavior, path);
    }

    private static void CreateHugeFishBehavior(string basePath)
    {
        var behavior = ScriptableObject.CreateInstance<FishBehavior>();
        
        // Properties
        behavior.size = 2f;
        behavior.baseSpeed = 2f;
        behavior.maxSpeed = 4f;
        behavior.turnSpeed = 2f;
        
        // Behavior Weights
        behavior.wanderWeight = 1.2f;
        behavior.flockWeight = 0f;
        behavior.avoidanceWeight = 0.2f;
        behavior.baitAttractionWeight = 0.5f;
        behavior.predatorAvoidanceWeight = 0f;
        
        // Detection Ranges
        behavior.visionRange = 8f;
        behavior.baitDetectionRange = 5f;
        behavior.predatorDetectionRange = 2f;
        behavior.personalSpace = 1.5f;
        
        // Predator Settings
        behavior.isPredator = true;
        behavior.preyLayer = LayerMask.GetMask("Fish");
        behavior.preyChaseWeight = 1.5f;
        behavior.preyMaxSizeRatio = 0.75f;
        behavior.fishColor = new Color(1f, 0f, 0f); // Red for predators

        string path = Path.Combine(basePath, "HugeFishBehavior.asset");
        AssetDatabase.CreateAsset(behavior, path);
    }
} 