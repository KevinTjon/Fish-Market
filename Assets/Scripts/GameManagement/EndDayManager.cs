using UnityEngine;
using System.Collections;
using TMPro;
using Market;
using UnityEngine.SceneManagement;

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
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Collider2D targetArea; // Assign your market area 2D collider here
    [SerializeField] private float cameraZoomDuration = 1.5f;
    [SerializeField] private float cameraPadding = 2f; // Extra space around bounds
    [SerializeField] private MarketplaceCameraController cameraController;
    [SerializeField] public FishMarketPlayerController playerController;
    
    private int currentDay = 1;
    public int CurrentDay => currentDay;
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

    private IEnumerator ZoomCameraToColliderBounds()
    {
        if (mainCamera == null || targetArea == null)
        {
            Debug.LogWarning("Camera or target area not assigned!");
            yield break;
        }

        // Hide and freeze player
        if (playerController != null)
            playerController.gameObject.SetActive(false);

        // Disable camera controller
        if (cameraController != null)
            cameraController.enabled = false;

        // Ensure the collider is enabled to get correct bounds
        bool wasEnabled = targetArea.enabled;
        if (!wasEnabled) targetArea.enabled = true;

        Bounds bounds = targetArea.bounds;
        Vector3 targetPosition = bounds.center;

        float halfHeight = bounds.size.y * 0.5f;
        float halfWidth = bounds.size.x * 0.5f;
        float aspect = mainCamera.aspect;
        float sizeToFitWidth = halfWidth / aspect;
        float targetSize = Mathf.Max(halfHeight, sizeToFitWidth) + cameraPadding;

        Debug.Log($"Camera aspect: {aspect}");
        Debug.Log($"Collider bounds: center={bounds.center}, size={bounds.size}");
        Debug.Log($"halfHeight={halfHeight}, halfWidth={halfWidth}, sizeToFitWidth={sizeToFitWidth}, targetSize={targetSize}, startSize={mainCamera.orthographicSize}");

        Vector3 startPosition = mainCamera.transform.position;
        float startSize = mainCamera.orthographicSize;

        // Only zoom out: if targetSize is less than current, keep current
        if (targetSize < startSize)
            targetSize = startSize;

        float elapsed = 0f;
        while (elapsed < cameraZoomDuration)
        {
            float t = elapsed / cameraZoomDuration;
            mainCamera.transform.position = Vector3.Lerp(startPosition, new Vector3(targetPosition.x, targetPosition.y, startPosition.z), t);
            mainCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        mainCamera.transform.position = new Vector3(targetPosition.x, targetPosition.y, startPosition.z);
        mainCamera.orthographicSize = targetSize;

        // Restore collider enabled state if it was disabled
        if (!wasEnabled) targetArea.enabled = false;

        // Unhide and unfreeze player
        // (Removed: do not re-enable playerController at the end of this phase)
    }

    private IEnumerator ProcessDaySequence()
    {
        isProcessingCustomers = true;
        Debug.Log($"Processing Day {currentDay}...");

        // Disable blurry background image if present
        if (UIManager.Instance != null)
        {
            var uiManagerType = UIManager.Instance.GetType();
            var blurImageField = uiManagerType.GetField("blurImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (blurImageField != null)
            {
                GameObject blurImage = blurImageField.GetValue(UIManager.Instance) as GameObject;
                if (blurImage != null)
                    blurImage.SetActive(false);
            }
        }

        // Close Book UI if open
        var bookController = GameObject.FindObjectOfType<BookController>();
        if (bookController != null)
        {
            var animator = bookController.GetComponent<Animator>();
            if (animator != null && animator.GetBool("IsOpen"))
            {
                animator.SetBool("IsOpen", false);
                // Wait for the close animation to finish (adjust duration as needed)
                yield return new WaitForSeconds(0.5f);
            }
            // Now disable the BookUI GameObject if it exists
            var bookUIGameObject = GameObject.Find("BookUI");
            if (bookUIGameObject != null)
            {
                bookUIGameObject.SetActive(false);
            }
        }

        // Camera zoom out before spawning customers
        yield return StartCoroutine(ZoomCameraToColliderBounds());

        // Generate initial customers only on day 1
        if (currentDay == 1)
        {
            Debug.Log("Day 1: Generating initial customers...");
            customerManager.GenerateInitialCustomers(10);
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

        // Wait for all customers to leave before transitioning scenes
        StartCoroutine(WaitForAllCustomersToLeave());
    }

    private IEnumerator WaitForAllCustomersToLeave()
    {
        // Wait until there are no PhysicalCustomer objects left in the scene
        while (FindObjectsOfType<Market.PhysicalCustomer>().Length > 0)
        {
            yield return null;
        }
        // All customers have left, transition to EndDay scene
        GameSceneManager.EnsureExists();
        if (purchaseManager != null)
        {
            purchaseManager.SaveAllSellerGold();
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene("EndDay");
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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "EndDay")
        {
            Destroy(gameObject);
        }
    }
}