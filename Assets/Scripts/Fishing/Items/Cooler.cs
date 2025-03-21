using System.Collections;
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

    public void AddFish(FishHookable fish)
    {
        var newWeight = currentWeight + fish.fishWeight;
        if (newWeight > maxWeight)
        {
            Debug.Log("Item cannot be added to cooler, it is full");
            return;
        }

        CoolerItem item = new CoolerItem();
        item.Initialize(fish.fishType, fish.fishWeight);
        coolerItems.AddLast(item);
        //Destroy(item);
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
}
