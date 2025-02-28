public class ExtensiveTabData
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Rarity { get; private set; }
    public string AssetPath { get; private set; }
    public float MinWeight { get; private set; }
    public float MaxWeight { get; private set; }
    public float TopSpeed { get; private set; }
    public string IsDiscovered { get; private set; }

    public ExtensiveTabData(string name, string description, string rarity, string assetPath, float minWeight, float maxWeight, float topSpeed, string isDiscovered)
    {
        Name = name;
        Description = description;
        Rarity = rarity;
        AssetPath = assetPath;
        MinWeight = minWeight;
        MaxWeight = maxWeight;
        TopSpeed = topSpeed;
        IsDiscovered = isDiscovered;
    }
}