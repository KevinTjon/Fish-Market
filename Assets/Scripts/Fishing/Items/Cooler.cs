using System.Collections.Generic;
using UnityEngine;

public class Cooler : MonoBehaviour
{
    private LinkedList<CoolerItem> coolerItems = new LinkedList<CoolerItem>();
    private int count;
    public int Count => count;

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
        count++;
        return true;
    }

    public CoolerItem GetFish(int index)
    {
        if (index < 0 || index >= count)
        {
            Debug.LogWarning("Index out of range: " + index);
            return null;
        }

        var node = coolerItems.First;
        for (int i = 0; i < index; i++)
        {
            node = node.Next;
        }
        return node.Value;
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
        var currFish = coolerItems.First;
        for (int i = 0; i < count; i++)
        {
            coolerMarket.Add(currFish.Value.GetCaughtFishData());
            currFish = currFish.Next;
        }
        
        return coolerMarket;
    }

    public LinkedList<string> GetListEntries()
    {
        var entries = new LinkedList<string>();
        var currFish = coolerItems.First;
        for (int i = 0; i < count; i++)
        {
            entries.AddLast(currFish.Value.GetCoolerListEntry());
            currFish = currFish.Next;
        }
        return entries;
    }
}