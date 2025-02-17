using UnityEngine;
using System.Collections;

public class EndDayManager : MonoBehaviour
{
    [SerializeField] private ClearMarketListings clearMarketListings;
    [SerializeField] private MarketPriceInitializer marketPriceInitializer;
    [SerializeField] private CustomerManager customerManager;
    [SerializeField] private CustomerPurchaseManager purchaseManager;
    [SerializeField] private FisherAIManager fisherAIManager;
    
    private int currentDay = 1;

    public void ProcessDay()
    {
        StartCoroutine(ProcessDaySequence());
    }

    private IEnumerator ProcessDaySequence()
    {
        Debug.Log($"Processing Day {currentDay}...");

        // Only clear tables on Day 1
        if (currentDay == 1)
        {
            Debug.Log("Day 1: Clearing all tables...");
            clearMarketListings.ClearTables();
            yield return new WaitForSeconds(0.1f); // Give database time to process
        }

        // Generate market prices
        Debug.Log("Generating market prices...");
        marketPriceInitializer.GenerateDayPrices();
        yield return new WaitForSeconds(0.1f);

        // Generate AI fisher catches and listings
        Debug.Log("Generating AI fisher catches...");
        fisherAIManager.GenerateAllFishersCatch();
        yield return new WaitForSeconds(0.1f);

        // Generate customers
        Debug.Log("Generating customers...");
        customerManager.TestGenerateInitialCustomers();
        yield return new WaitForSeconds(0.1f);

        // Process purchases
        Debug.Log("Processing customer purchases...");
        purchaseManager.ProcessCustomerPurchases();
        
        // Optional: Display debug information
        Debug.Log(purchaseManager.DebugRemainingShoppingLists());

        currentDay++;
        Debug.Log("Day processing complete!");
    }

    // For testing in Unity Editor
    [ContextMenu("Process Day")]
    public void TestProcessDay()
    {
        ProcessDay();
    }
}