using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class TestCustomerPurchasing : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI outputText;
    [SerializeField] private CustomerManager customerManager;
    [SerializeField] private CustomerPurchaseManager purchaseManager;
    
    private void Awake()
    {
        // Wait for CustomerManager to initialize first
        if (customerManager == null)
        {
            customerManager = FindObjectOfType<CustomerManager>();
            if (customerManager == null)
            {
                Debug.LogError("Could not find CustomerManager!");
                return;
            }
        }

        // Wait for PurchaseManager to initialize
        if (purchaseManager == null)
        {
            purchaseManager = FindObjectOfType<CustomerPurchaseManager>();
            if (purchaseManager == null)
            {
                Debug.LogError("Could not find CustomerPurchaseManager!");
                return;
            }
        }
    }

    public void RunPurchaseTest()
    {
        // Process purchases and display results
        purchaseManager.ProcessCustomerPurchases();
        
        // Get the debug output and display it in the UI
        var debugOutput = purchaseManager.DebugRemainingShoppingLists();
        if (outputText != null)
        {
            outputText.text = debugOutput;
        }
        else
        {
            Debug.Log(debugOutput);
        }
    }
} 