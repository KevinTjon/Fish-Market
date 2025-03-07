using System;
using UnityEngine;

public class CoolerItem
{
    private readonly float weightEpsilon = 0.01f;
    // Private set variables
    // fishID: Integer of the fish's entry in the local database
    //         This should be instantiated during load time
    //         Allows us to easily convert the fish between its various contexts
    private uint fishID;
    private string fishName;
    private string fishRarity;
    private string fishAssetPath;
    private float fishWeight;
    private string fishDescription;
    private CatchData fishCatchData;
    // Public get variables
    public uint ID { get {return fishID;} } 
    public string Name { get {return fishName;} }
    public string Rarity { get {return fishRarity;} }
    public string AssetPath { get {return fishAssetPath;} }
    public float Weight { get {return fishWeight;} }
    public string Description { get {return fishDescription;} }
    public CatchData CatchData { get {return fishCatchData;} }

    public CoolerItem(uint id, string name, float weight)
    {
        // Search up id in current fish list created at the start of the scene
        var fish = "";

        fishID = id;
        fishName = name;
        fishWeight = weight;
        fishCatchData = new CatchData("Test Location");
        //return this;
    }

    public bool DoesItemMatch(uint id, string name, float weight)
    {
        return id == fishID && name == fishName && 
               Math.Abs(weight-fishWeight) >= weightEpsilon;
    }
}
