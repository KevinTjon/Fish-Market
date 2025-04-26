using System.Collections.Generic;
using UnityEngine;

public class Cooler : MonoBehaviour
{
    private LinkedList<CoolerItem> coolerItems = new LinkedList<CoolerItem>();
    private readonly float maxWeight = 5f;
    private float currentWeight;


    private void Awake()
    {
        currentWeight = 0f;
    }
    /// <summary>
    /// Adds a fish to the cooler if there is enough space.
    /// </summary>
    /// <param name="fish">The fish to be added to the cooler.</param>
    /// <returns>True if the fish was added successfully, false if the cooler is full.</returns>
    public bool AddFish(BasicFish fish)
    {

        var newWeight = currentWeight + fish.Weight;
        if (newWeight > maxWeight)
        {
            Debug.Log("Item cannot be added to cooler, it is full");
            return false;
        }

        currentWeight = newWeight;
        CoolerItem item = new CoolerItem();
        item.Initialize(fish);
        coolerItems.AddLast(item);
        return true;
    }

    public void DisplayCooler()
    {
        foreach (CoolerItem item in coolerItems)
        {
            item.DisplayFish();
        }
    }
    
    public List<CaughtFishData> SendCoolerToMarket()
    {
        var coolerMarket = new List<CaughtFishData>();
        foreach (CoolerItem item in coolerItems)
        {
            coolerMarket.Add(item.GetCaughtFishData());
        }
        return coolerMarket;
    }

    public LinkedList<string> GetListEntries()
    {
        var entries = new LinkedList<string>();
        foreach (CoolerItem item in coolerItems)
        {
            entries.AddLast(item.GetCoolerListEntry());
        }
        return entries;
    }
}