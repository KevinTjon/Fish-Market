using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoolerItem : MonoBehaviour
{
    // Private set variables
    // Integer of the fish's entry in the local database
    // This should be instantiated during load time
    // Allows us to easily convert the fish between its various contexts
    private uint fishID;
    private string fishName;
    private string fishRarity;
    private string fishAssetPath;
    private float fishWeight;
    // Public get variables
    public uint ID { get {return fishID;} } 
    public string Name { get {return fishName;} }
    public string Rarity { get {return fishRarity;} }
    public string AssetPath { get {return fishAssetPath;} }
    public float Weight { get {return fishWeight;} }

    public void Initialize(/* Fish link variable */)
    {
        
    }

    private void Awake()
    {

    }
}
