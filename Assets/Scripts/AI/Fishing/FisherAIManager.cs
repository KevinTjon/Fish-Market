using UnityEngine;
using System.Collections.Generic;

public class FisherAIManager : MonoBehaviour
{
    private List<FisherAI> fishers;

    private void Start()
    {
        InitializeFishers();
    }

    private void InitializeFishers()
    {
        fishers = new List<FisherAI>
        {
            new CommonFisherAI(1),
            new RareFisherAI(2),
            new BalancedFisherAI(3),
            new ExpertFisherAI(4)
        };
    }

    public void GenerateAllFishersCatch()
    {
        foreach (var fisher in fishers)
        {
            List<string> catch_ = fisher.GenerateFishCatch();
            //Debug.Log($"{fisher.Name} caught {catch_.Count} fish");
            fisher.CreateMarketListings(catch_);
        }
    }

    // For testing in Unity Editor
    public void TestGeneration()
    {
        GenerateAllFishersCatch();
    }
}