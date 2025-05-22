using UnityEngine;
using System.Collections.Generic;

public class FisherAIManager : MonoBehaviour
{
    private List<FisherAI> fishers;

    private void Awake()
    {
        InitializeFishers();
    }

    private void InitializeFishers()
    {
        if (fishers == null)
        {
            fishers = new List<FisherAI>
            {
                new CommonFisherAI(1),
                new RareFisherAI(2),
                new BalancedFisherAI(3),
                new ExpertFisherAI(4)
            };
        }
    }

    public void GenerateAllFishersCatch()
    {
        Debug.Log("FisherAIManager: Starting to generate all fishers' catches...");
        
        if (fishers == null)
        {
            Debug.Log("FisherAIManager: Fishers list was null, initializing...");
            InitializeFishers();
        }

        Debug.Log($"FisherAIManager: Processing {fishers.Count} fishers");
        foreach (var fisher in fishers)
        {
            //Debug.Log($"FisherAIManager: {fisher.Name} starting to generate catch...");
            List<string> catch_ = fisher.GenerateFishCatch();
            //Debug.Log($"FisherAIManager: {fisher.Name} caught {catch_.Count} fish");
            
            //Debug.Log($"FisherAIManager: {fisher.Name} creating market listings...");
            fisher.CreateMarketListings(catch_);
            //Debug.Log($"FisherAIManager: {fisher.Name} finished creating listings");
        }
        //Debug.Log("FisherAIManager: Finished generating all fishers' catches");
    }

    // For testing in Unity Editor
    public void TestGeneration()
    {
        GenerateAllFishersCatch();
    }
}