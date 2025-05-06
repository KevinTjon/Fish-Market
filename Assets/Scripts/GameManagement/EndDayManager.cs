using UnityEngine;
using System.Collections;
using TMPro;
using Market;

public class EndDayManager : MonoBehaviour
{
    private static EndDayManager _instance;
    public static EndDayManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Find existing instance
                _instance = FindObjectOfType<EndDayManager>();
                
                // If no instance exists, create one
                if (_instance == null)
                {
                    GameObject obj = new GameObject("EndDayManager");
                    _instance = obj.AddComponent<EndDayManager>();
                }
            }
            return _instance;
        }
    }

    [SerializeField] private ClearMarketListings clearMarketListings;
    [SerializeField] private MarketPriceInitializer marketPriceInitializer;
    [SerializeField] private CustomerManager customerManager;
    [SerializeField] private CustomerPurchaseManager purchaseManager;
    [SerializeField] private FisherAIManager fisherAIManager;
    [SerializeField] private MarketPriceAdjuster marketPriceAdjuster;
    [SerializeField] private TextMeshProUGUI dayText; // Reference to UI text showing current day
    [SerializeField] private float customerProcessingDelay = 2f; // Delay between customer processing to allow for movement
    
    private int currentDay = 1;
    private bool isProcessingCustomers = false;

    private void Awake()
    {
        // If there's already an instance and it's not this one, destroy this one
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Make this the singleton instance
        _instance = this;
        
        // Ensure this GameObject is at the root level
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        
        // Don't destroy on load (only called once we're sure this is the singleton instance)
        DontDestroyOnLoad(gameObject);

        // Initialize references
        if (marketPriceAdjuster == null)
            marketPriceAdjuster = FindObjectOfType<MarketPriceAdjuster>();
        
        if (marketPriceAdjuster == null)
            Debug.LogError("MarketPriceAdjuster not found!");

        // Initialize MarketPriceInitializer if not set
        if (marketPriceInitializer == null)
        {
            marketPriceInitializer = FindObjectOfType<MarketPriceInitializer>();
            if (marketPriceInitializer == null)
            {
                Debug.LogError("MarketPriceInitializer not found in scene! Creating one...");
                GameObject obj = new GameObject("MarketPriceInitializer");
                marketPriceInitializer = obj.AddComponent<MarketPriceInitializer>();
                Debug.Log("Created new MarketPriceInitializer");
            }
            else
            {
                Debug.Log("MarketPriceInitializer found and initialized");
            }
        }

        // Initialize CustomerPurchaseManager if not set
        if (purchaseManager == null)
        {
            purchaseManager = FindObjectOfType<CustomerPurchaseManager>();
            if (purchaseManager == null)
            {
                Debug.LogError("CustomerPurchaseManager not found in scene! Creating one...");
                GameObject obj = new GameObject("CustomerPurchaseManager");
                purchaseManager = obj.AddComponent<CustomerPurchaseManager>();
                Debug.Log("Created new CustomerPurchaseManager");
            }
            else
            {
                Debug.Log("CustomerPurchaseManager found and initialized");
            }
        }

        // Initialize CustomerManager if not set
        if (customerManager == null)
        {
            customerManager = FindObjectOfType<CustomerManager>();
            if (customerManager == null)
            {
                Debug.LogError("CustomerManager not found in scene! Creating one...");
                GameObject obj = new GameObject("CustomerManager");
                customerManager = obj.AddComponent<CustomerManager>();
                Debug.Log("Created new CustomerManager");
            }
            else
            {
                Debug.Log("CustomerManager found and initialized");
            }
        }

        // Initialize FisherAIManager if not set
        if (fisherAIManager == null)
        {
            fisherAIManager = FindObjectOfType<FisherAIManager>();
            if (fisherAIManager == null)
            {
                Debug.LogError("FisherAIManager not found in scene! Creating one...");
                GameObject obj = new GameObject("FisherAIManager");
                fisherAIManager = obj.AddComponent<FisherAIManager>();
                Debug.Log("Created new FisherAIManager");
            }
            else
            {
                Debug.Log("FisherAIManager found and initialized");
            }
        }

        UpdateDayText();
    }

    public void ProcessDay()
    {
        if (!isProcessingCustomers)
        {
            StartCoroutine(ProcessDaySequence());
        }
        else
        {
            Debug.Log("Already processing customers, please wait...");
        }
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
        isProcessingCustomers = true;
        Debug.Log($"Processing Day {currentDay}...");

        // Generate initial customers only on day 1
        if (currentDay == 1)
        {
            Debug.Log("Day 1: Generating initial customers...");
            customerManager.GenerateInitialCustomers(5);
            yield return new WaitForSeconds(customerProcessingDelay);

            // Initialize first day prices
            Debug.Log("Day 1: Generating initial market prices...");
            marketPriceInitializer.GenerateDayPrices();
            yield return new WaitForSeconds(0.1f);
        }
        else
        {
            // On subsequent days, update prices before generating fish
            Debug.Log($"Day {currentDay}: Updating market prices...");
            marketPriceAdjuster.UpdateAllPrices();
            yield return new WaitForSeconds(0.1f);
        }

        // Generate AI fisher catches and listings
        Debug.Log("Generating AI fisher catches...");
        fisherAIManager.GenerateAllFishersCatch();
        yield return new WaitForSeconds(0.1f);

        // Check if there are any listings before processing purchases
        var listings = DatabaseManager.Instance.GetUnsoldListings("COMMON"); // Check at least common fish
        Debug.Log($"Found {listings.Count} unsold common fish listings before processing purchases");

        if (listings.Count == 0)
        {
            Debug.LogWarning("No listings available! Skipping customer purchases.");
            yield break;
        }

        // Process customer purchases
        Debug.Log("Processing customer purchases...");
        Debug.Log($"Active customers before processing: {purchaseManager.GetActiveCustomers().Count}");
        Debug.Log($"Waiting customers before processing: {purchaseManager.GetWaitingCustomers().Count}");

        purchaseManager.ProcessCustomerPurchases();
        yield return new WaitForSeconds(customerProcessingDelay);

        // Optional: Display debug information
        string debugInfo = purchaseManager.DebugRemainingShoppingLists();
        Debug.Log($"Customer Status after purchases:\n{debugInfo}");

        // Comment out daily table clearing for debugging
        // Debug.Log("Clearing daily tables for next day...");
        // clearMarketListings.ClearDailyTables();
        // yield return new WaitForSeconds(0.1f);

        // Clear the listings cache in purchase manager
        purchaseManager.ClearListingsCache();
        Debug.Log("Cleared listings cache in purchase manager");

        Debug.Log($"Day {currentDay} processing complete! Total active customers: {purchaseManager.GetActiveCustomers().Count}");

        isProcessingCustomers = false;
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