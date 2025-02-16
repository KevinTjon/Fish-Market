using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class TestCustomerPurchasing : MonoBehaviour
{
    private CustomerPurchaseManager purchaseManager;
    private CustomerManager customerManager;
    private CustomerPurchaseEvaluator evaluator;
    [SerializeField] private TextMeshProUGUI outputText;
    private string testOutput = "";

    private void Awake()
    {
        purchaseManager = FindObjectOfType<CustomerPurchaseManager>();
        customerManager = FindObjectOfType<CustomerManager>();
        evaluator = FindObjectOfType<CustomerPurchaseEvaluator>();
    }

    public void RunPurchaseTest()
    {
        testOutput = "Starting Customer Purchase Test...\n\n";

        if (evaluator == null || purchaseManager == null)
        {
            testOutput = "Error: Required components not found in scene!";
            outputText.text = testOutput;
            return;
        }

        // Show market prices first
        evaluator.TestAveragePrices(outputText);
        testOutput = outputText.text;

        testOutput += "\n=== TESTING PURCHASE EVALUATION ===\n";
        
        // Test each customer type
        foreach (Customer.CUSTOMERTYPE type in System.Enum.GetValues(typeof(Customer.CUSTOMERTYPE)))
        {
            testOutput += $"\nTesting {type} Customer\n";
            testOutput += "------------------------\n";

            // Get a customer of this type
            var customer = customerManager.GetAllCustomers()
                .FirstOrDefault(c => c.Type == type);

            if (customer != null)
            {
                testOutput += $"Customer ID: {customer.CustomerID}\n";
                testOutput += $"Budget: {customer.Budget:F2} gold\n";
                testOutput += "Shopping List:\n";

                foreach (var item in customer.ShoppingList)
                {
                    testOutput += $"- Needs {item.Amount} {item.Rarity} fish\n";
                    
                    // Get listings for this rarity
                    var listings = purchaseManager.GetListings(item.Rarity);
                    if (listings != null && listings.Count > 0)
                    {
                        testOutput += "Available listings:\n";
                        foreach (var listing in listings)
                        {
                            testOutput += $"- {listing.FishName}: {listing.ListedPrice:F2} gold\n";
                        }

                        // Evaluate purchase
                        var decision = evaluator.EvaluatePurchase(customer, listings, item.Rarity);
                        testOutput += $"\nDecision: {(decision.WillPurchase ? "WILL BUY" : "WILL NOT BUY")}\n";
                        testOutput += $"Reason: {decision.Reason}\n";
                    }
                    else
                    {
                        testOutput += "No listings found for this rarity.\n";
                    }
                }
            }
            else
            {
                testOutput += $"No {type} customer found in database!\n";
            }
            
            testOutput += "------------------------\n";
        }

        outputText.text = testOutput;
        Debug.Log("Purchase test completed");
    }
} 