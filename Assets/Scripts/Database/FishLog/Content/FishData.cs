public class FishData
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Rarity { get; private set; }
    public string AssetPath { get; private set; }
    public float MinWeight { get; private set; }
    public float MaxWeight { get; private set; }
    public float TopSpeed { get; private set; }
    public int HookedFuncNum { get; private set; }
    public int IsDiscovered { get; private set; }

    public FishData(string name, string description, string rarity, string assetPath, float minWeight, float maxWeight, float topSpeed, int hookedFuncNum, int isDiscovered)
    {
        Name = name;
        Description = description;
        Rarity = rarity;
        AssetPath = assetPath;
        MinWeight = minWeight;
        MaxWeight = maxWeight;
        TopSpeed = topSpeed;
        HookedFuncNum = hookedFuncNum;
        IsDiscovered = isDiscovered;
    }
}