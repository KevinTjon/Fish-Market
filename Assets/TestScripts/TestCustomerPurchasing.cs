using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class TestCustomerPurchasing : MonoBehaviour
{
    private CustomerPurchaseManager purchaseManager;
    private CustomerPurchaseEvaluator evaluator;
    [SerializeField] private TextMeshProUGUI outputText;
    private string testOutput = "";
    [SerializeField] private CustomerManager customerManager;
    [SerializeField] private CustomerPurchaseManager customerPurchaseManager;
    [SerializeField] private CustomerPurchaseEvaluator purchaseEvaluator;

    private void Awake()
    {
        customerManager = FindObjectOfType<CustomerManager>();
        customerPurchaseManager = FindObjectOfType<CustomerPurchaseManager>();
        purchaseEvaluator = FindObjectOfType<CustomerPurchaseEvaluator>();
    }

    public void RunPurchaseTest()
    {
        Debug.Log("Starting purchase test...");

        // Get market averages using historical data
        var marketAverages = customerPurchaseManager.GetHistoricalAveragePrices(Customer.FISHRARITY.COMMON);
        Debug.Log("\nMarket Average Prices:\n");
        PrintMarketAverages(marketAverages);

        Debug.Log("\n=== TESTING PURCHASE EVALUATION ===\n");

        // Test each customer type
        TestCustomerType(Customer.CUSTOMERTYPE.BUDGET);
        TestCustomerType(Customer.CUSTOMERTYPE.CASUAL);
        TestCustomerType(Customer.CUSTOMERTYPE.COLLECTOR);
        TestCustomerType(Customer.CUSTOMERTYPE.WEALTHY);

        // Now process the actual purchases
        Debug.Log("\n=== PROCESSING PURCHASES ===\n");
        customerPurchaseManager.ProcessCustomerPurchases();

        Debug.Log("Purchase test complete");
    }

    private void TestCustomerType(Customer.CUSTOMERTYPE type)
    {
        Debug.Log($"Testing {type} Customer");
        Debug.Log("------------------------");

        // Get existing customer of the desired type
        var customers = customerManager.GetAllCustomers();
        var customer = customers.FirstOrDefault(c => c.Type == type);
        
        if (customer == null)
        {
            Debug.LogError($"No {type} customer found!");
            return;
        }

        Debug.Log($"Customer ID: {customer.CustomerID}");
        Debug.Log($"Budget: {customer.Budget:F2} gold");
        
        // The evaluator already has access to market prices through the database
        customerPurchaseManager.ProcessCustomerPurchases();
    }

    private void PrintMarketAverages(Dictionary<string, float> marketAverages)
    {
        foreach (var kvp in marketAverages)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value:F2} gold");
        }
    }
} 