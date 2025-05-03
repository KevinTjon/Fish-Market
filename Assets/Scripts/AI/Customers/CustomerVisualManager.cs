using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Market;

public class CustomerVisualManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject customerPrefab;
    
    [Header("Spawn Settings")]
    [SerializeField] private BoxCollider2D spawnArea;
    [SerializeField] private float minDistanceBetweenSpawns = 1f;
    
    [Header("Market Settings")]
    [SerializeField] private StallManager stallManager;
    [SerializeField] private float interactionPointOffset = 1f;
    [SerializeField] private bool showDebugLogs = true;

    private Dictionary<int, CustomerVisual> activeCustomers = new Dictionary<int, CustomerVisual>();
    private Dictionary<Customer.SellerType, Transform> sellerInteractionPoints = new Dictionary<Customer.SellerType, Transform>();

    private void Start()
    {
        InitializeManager();
    }

    private void InitializeManager()
    {
        if (spawnArea == null)
        {
            Debug.LogError("[CustomerVisualManager] Spawn area BoxCollider2D not assigned!");
            return;
        }

        if (stallManager == null)
        {
            stallManager = FindObjectOfType<StallManager>();
            if (stallManager == null)
            {
                Debug.LogError("[CustomerVisualManager] StallManager not found!");
                return;
            }
        }

        // Cache seller interaction points
        foreach (var stall in stallManager.GetActiveStalls())
        {
            var interactable = stall.GetComponentInChildren<IInteractable>();
            if (interactable != null)
            {
                sellerInteractionPoints[stall.SellerType] = interactable.GetTransform();
                Debug.Log($"[CustomerVisualManager] Cached interaction point for {stall.SellerType} at position {interactable.GetTransform().position}");
            }
            else
            {
                Debug.LogWarning($"[CustomerVisualManager] No IInteractable found for stall {stall.SellerType}");
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"[CustomerVisualManager] Initialized with {stallManager.GetActiveStalls().Count} stalls");
            foreach (var point in sellerInteractionPoints)
            {
                Debug.Log($"[CustomerVisualManager] Interaction point for {point.Key}: {point.Value.position}");
            }
        }
    }

    public void SpawnCustomer(Customer customer)
    {
        if (spawnArea == null) return;

        Vector2 spawnPosition = GetRandomSpawnPosition();
        if (showDebugLogs)
        {
            Debug.Log($"[CustomerVisualManager] Customer {customer.CustomerID} spawning at position {spawnPosition}");
        }

        GameObject customerObj = Instantiate(customerPrefab, spawnPosition, Quaternion.identity);
        CustomerVisual customerVisual = customerObj.GetComponent<CustomerVisual>();
        
        // Ensure the customer has SimpleCustomerMovement component
        var movement = customerObj.GetComponent<SimpleCustomerMovement>();
        if (movement == null)
        {
            movement = customerObj.AddComponent<SimpleCustomerMovement>();
            if (showDebugLogs)
            {
                Debug.Log($"[CustomerVisualManager] Added SimpleCustomerMovement to Customer {customer.CustomerID}");
            }
        }
        
        customerVisual.Initialize(customer);
        activeCustomers[customer.CustomerID] = customerVisual;
    }

    public void SendCustomerToStall(int customerID, int sellerId)
    {
        if (!activeCustomers.TryGetValue(customerID, out CustomerVisual customer))
        {
            Debug.LogWarning($"[CustomerVisualManager] Customer {customerID} not found in active customers!");
            return;
        }

        var sellerType = (Customer.SellerType)sellerId;
        var stall = stallManager.GetStall(sellerType);
        
        if (stall != null && sellerInteractionPoints.TryGetValue(sellerType, out Transform interactionPoint))
        {
            if (showDebugLogs)
            {
                Debug.Log($"[CustomerVisualManager] Customer {customerID} moving to {sellerType} stall at {interactionPoint.position}");
            }

            var movement = customer.GetComponent<SimpleCustomerMovement>();
            if (movement != null)
            {
                // Move to the interaction point
                movement.MoveTo(interactionPoint.position, () => {
                    if (showDebugLogs)
                    {
                        Debug.Log($"[CustomerVisualManager] Customer {customerID} has reached {sellerType} stall!");
                    }
                    stall.HandleCustomerArrival(customer);
                });
            }
            else
            {
                Debug.LogError($"[CustomerVisualManager] SimpleCustomerMovement not found on customer {customerID}!");
            }
        }
        else
        {
            Debug.LogWarning($"[CustomerVisualManager] Could not find interaction point for seller type {sellerType}");
        }
    }

    public void CustomerFinishedAtStall(int customerID, bool madePurchase)
    {
        if (!activeCustomers.TryGetValue(customerID, out CustomerVisual customer))
        {
            return;
        }

        if (showDebugLogs)
        {
            Debug.Log($"Customer {customerID} finished at stall. Purchase made: {madePurchase}");
        }

        // Play appropriate animation
        if (madePurchase)
        {
            customer.PlayPurchaseAnimation();
        }
        else
        {
            customer.PlayEvaluatingAnimation();
        }

        var sellerType = customer.GetComponent<Customer>().CurrentSeller;
        var stall = stallManager.GetStall(sellerType);
        if (stall != null)
        {
            stall.HandleCustomerDeparture(customer);
        }

        StartCoroutine(SendCustomerAway(customer));
    }

    private IEnumerator SendCustomerAway(CustomerVisual customer)
    {
        int customerId = customer.GetComponent<Customer>().CustomerID;
        if (showDebugLogs)
        {
            Debug.Log($"[CustomerVisualManager] Customer {customerId} preparing to leave market...");
        }

        // Wait for current animation to finish
        yield return new WaitForSeconds(1f);

        var movement = customer.GetComponent<SimpleCustomerMovement>();
        if (movement != null)
        {
            Vector3 exitPoint = spawnArea.bounds.center;
            
            if (showDebugLogs)
            {
                Debug.Log($"[CustomerVisualManager] Customer {customerId} moving to exit at {exitPoint}");
            }

            // Move to exit point
            movement.MoveTo(exitPoint, () => {
                if (showDebugLogs)
                {
                    Debug.Log($"[CustomerVisualManager] Customer {customerId} has left the market.");
                }
                
                // Remove from active customers and destroy
                activeCustomers.Remove(customerId);
                Destroy(customer.gameObject);
            });

            // Wait until customer reaches exit or timeout
            float timeout = 10f;
            float timer = 0f;
            while (timer < timeout && !movement.HasReachedPosition(exitPoint))
            {
                timer += Time.deltaTime;
                yield return null;
            }

            if (timer >= timeout)
            {
                Debug.LogWarning($"[CustomerVisualManager] Customer {customerId} exit movement timed out!");
                // Force destroy if timeout
                activeCustomers.Remove(customerId);
                Destroy(customer.gameObject);
            }
        }
        else
        {
            Debug.LogError($"[CustomerVisualManager] SimpleCustomerMovement not found on Customer {customerId}!");
            // Clean up even if movement component is missing
            activeCustomers.Remove(customerId);
            Destroy(customer.gameObject);
        }
    }

    private Vector2 GetRandomSpawnPosition()
    {
        Bounds bounds = spawnArea.bounds;
        Vector2 spawnPosition;
        int maxAttempts = 10;
        int attempts = 0;

        do
        {
            spawnPosition = new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y)
            );
            attempts++;

            // Check if position is clear of other customers
            if (Physics2D.OverlapCircle(spawnPosition, minDistanceBetweenSpawns) == null)
            {
                return spawnPosition;
            }
        } 
        while (attempts < maxAttempts);

        // Fallback to center if no clear position found
        if (showDebugLogs)
        {
            Debug.Log($"[CustomerVisualManager] Using fallback spawn position at {bounds.center} after {attempts} failed attempts");
        }
        return bounds.center;
    }
} 