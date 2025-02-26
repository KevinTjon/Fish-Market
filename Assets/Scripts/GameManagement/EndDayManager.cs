using UnityEngine;
using System.Collections;
using TMPro;

public class EndDayManager : MonoBehaviour
{
    [SerializeField] private ClearMarketListings clearMarketListings;
    [SerializeField] private MarketPriceInitializer marketPriceInitializer;
    [SerializeField] private CustomerManager customerManager;
    [SerializeField] private CustomerPurchaseManager purchaseManager;
    [SerializeField] private FisherAIManager fisherAIManager;
    [SerializeField] private MarketPriceAdjuster marketPriceAdjuster;
    [SerializeField] private TextMeshProUGUI dayText; // Reference to UI text showing current day
    
    private int currentDay = 1;

    private void Awake()
    {
        if (marketPriceAdjuster == null)
            marketPriceAdjuster = FindObjectOfType<MarketPriceAdjuster>();
        
        if (marketPriceAdjuster == null)
            Debug.LogError("MarketPriceAdjuster not found!");

        UpdateDayText();
    }

    public void ProcessDay()
    {
        StartCoroutine(ProcessDaySequence());
    }

    public void ResetToDay1()
    {
        currentDay = 1;
        UpdateDayText();
        StartCoroutine(ResetSequence());
    }

    private void UpdateDayText()
    {
        if (dayText != null)
        {
            dayText.text = $"Day {currentDay}";
        }
    }

    private IEnumerator ResetSequence()
    {
        Debug.Log("Resetting to Day 1...");

        // Clear all tables
        clearMarketListings.ClearTables();
        yield return new WaitForSeconds(0.1f);

        Debug.Log("Reset to Day 1 complete! Press Next Day to start the simulation.");
    }

    private IEnumerator ProcessDaySequence()
    {
        Debug.Log($"Processing Day {currentDay}...");

        // Generate initial customers only on day 1
        if (currentDay == 1)
        {
            Debug.Log("Day 1: Generating initial customers...");
            customerManager.GenerateInitialCustomers(5);
            yield return new WaitForSeconds(0.1f);

            // Initialize first day prices
            Debug.Log("Day 1: Generating initial market prices...");
            marketPriceInitializer.GenerateDayPrices();
            yield return new WaitForSeconds(0.1f);
        }

        // Generate AI fisher catches and listings
        Debug.Log("Generating AI fisher catches...");
        fisherAIManager.GenerateAllFishersCatch();
        yield return new WaitForSeconds(0.1f);

        // Process purchases
        Debug.Log("Processing customer purchases...");
        purchaseManager.ProcessCustomerPurchases();
        
        // Optional: Display debug information
        string debugInfo = purchaseManager.DebugRemainingShoppingLists();
        Debug.Log($"Customer Status after purchases:\n{debugInfo}");

        // Generate next day's market prices after purchases are processed
        if (currentDay >= 1)
        {
            Debug.Log("Generating next day's market prices...");
            marketPriceAdjuster.UpdateAllPrices();
            yield return new WaitForSeconds(0.1f);
            
            // Clear daily tables after prices are updated but before next day
            Debug.Log("Clearing daily tables for next day...");
            clearMarketListings.ClearDailyTables();
            yield return new WaitForSeconds(0.1f);

            // Clear the listings cache in purchase manager
            purchaseManager.ClearListingsCache();
        }

        currentDay++;
        UpdateDayText();
        Debug.Log($"Day {currentDay-1} processing complete! Total active customers: {purchaseManager.GetActiveCustomers().Count}");
    }

    // For testing in Unity Editor
    [ContextMenu("Process Day")]
    public void TestProcessDay()
    {
        ProcessDay();
    }

    [ContextMenu("Reset To Day 1")]
    public void TestResetToDay1()
    {
        ResetToDay1();
    }
}