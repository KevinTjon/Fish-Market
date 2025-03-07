using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Cooler : MonoBehaviour
{
    private List<CoolerItem> contents;
    private float currWeight;
    [SerializeField] private readonly float maxWeight;

    public List<CoolerItem> Contents { get {return contents;}}
    public float CurrWeight { get {return currWeight;} }
    public float MaxWeight { get {return maxWeight;} }

    public void Instantiate()
    {
    
    }

    public bool CanFishFit(float weight)
    {
        return currWeight + weight <= maxWeight;
    }

    public void AddFishToCooler(uint id, string name, float weight)
    {
        if (CanFishFit(weight))
        {
            var newFish = new CoolerItem(id, name, weight);
            contents.Add(newFish);
            currWeight += weight;
        }
    }

    // Removes a fish from the cooler based on name and weight
    public void ReleaseFishFromCooler(uint id, string name, float weight)
    {
        CoolerItem fish = contents.Find((f) => f.DoesItemMatch(id, name, weight));
        try
        {
            contents.Remove(fish);
            Debug.Log($"Successfully removed fish {fish}");
        }
        catch(Exception e)
        {
            Debug.Log($"Fish is not in cooler: {e.Message}");
        }
        
    }
}
