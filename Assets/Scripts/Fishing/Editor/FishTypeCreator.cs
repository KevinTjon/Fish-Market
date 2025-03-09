using UnityEngine;
using UnityEditor;
using System.IO;

public class FishTypeCreator : EditorWindow
{
    [MenuItem("Tools/Fish Market/Create Fish Types")]
    public static void CreateFishTypes()
    {
        string basePath = "Assets/Resources/FishTypes";
        
        // Create directory if it doesn't exist
        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
        }

        // Create different fish types
        CreateSardineType(basePath);
        CreateTunaType(basePath);
        CreateSharkType(basePath);
        CreateAnchovyType(basePath);
        CreateSalmonType(basePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateSardineType(string basePath)
    {
        var fishType = ScriptableObject.CreateInstance<FishType>();
        
        fishType.fishName = "Sardine";
        fishType.size = FishSize.Small;
        fishType.baseValue = 10f;
        fishType.minValue = 8f;
        fishType.maxValue = 15f;
        fishType.canEat = false;
        fishType.preySize = new FishSize[0]; // Sardines don't eat other fish
        fishType.maxPopulation = 20;
        fishType.spawnRate = 1.5f;
        fishType.spawnChance = 0.7f;

        string path = Path.Combine(basePath, "SardineFishType.asset");
        AssetDatabase.CreateAsset(fishType, path);
    }

    private static void CreateTunaType(string basePath)
    {
        var fishType = ScriptableObject.CreateInstance<FishType>();
        
        fishType.fishName = "Tuna";
        fishType.size = FishSize.Large;
        fishType.baseValue = 50f;
        fishType.minValue = 40f;
        fishType.maxValue = 75f;
        fishType.canEat = true;
        fishType.preySize = new FishSize[] { FishSize.Tiny, FishSize.Small };
        fishType.maxPopulation = 8;
        fishType.spawnRate = 0.5f;
        fishType.spawnChance = 0.4f;

        string path = Path.Combine(basePath, "TunaFishType.asset");
        AssetDatabase.CreateAsset(fishType, path);
    }

    private static void CreateSharkType(string basePath)
    {
        var fishType = ScriptableObject.CreateInstance<FishType>();
        
        fishType.fishName = "Shark";
        fishType.size = FishSize.Huge;
        fishType.baseValue = 100f;
        fishType.minValue = 80f;
        fishType.maxValue = 150f;
        fishType.canEat = true;
        fishType.preySize = new FishSize[] { FishSize.Tiny, FishSize.Small, FishSize.Medium, FishSize.Large };
        fishType.maxPopulation = 3;
        fishType.spawnRate = 0.2f;
        fishType.spawnChance = 0.2f;

        string path = Path.Combine(basePath, "SharkFishType.asset");
        AssetDatabase.CreateAsset(fishType, path);
    }

    private static void CreateAnchovyType(string basePath)
    {
        var fishType = ScriptableObject.CreateInstance<FishType>();
        
        fishType.fishName = "Anchovy";
        fishType.size = FishSize.Tiny;
        fishType.baseValue = 5f;
        fishType.minValue = 3f;
        fishType.maxValue = 8f;
        fishType.canEat = false;
        fishType.preySize = new FishSize[0];
        fishType.maxPopulation = 30;
        fishType.spawnRate = 2f;
        fishType.spawnChance = 0.8f;

        string path = Path.Combine(basePath, "AnchovyFishType.asset");
        AssetDatabase.CreateAsset(fishType, path);
    }

    private static void CreateSalmonType(string basePath)
    {
        var fishType = ScriptableObject.CreateInstance<FishType>();
        
        fishType.fishName = "Salmon";
        fishType.size = FishSize.Medium;
        fishType.baseValue = 30f;
        fishType.minValue = 25f;
        fishType.maxValue = 45f;
        fishType.canEat = true;
        fishType.preySize = new FishSize[] { FishSize.Tiny };
        fishType.maxPopulation = 12;
        fishType.spawnRate = 0.8f;
        fishType.spawnChance = 0.5f;

        string path = Path.Combine(basePath, "SalmonFishType.asset");
        AssetDatabase.CreateAsset(fishType, path);
    }
} 