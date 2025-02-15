using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Mono.Data.Sqlite;
using System.Data;

public class SellPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField sellPriceInput;
    [SerializeField] private TextMeshProUGUI quantityText;  // This is just for display
    [SerializeField] private Button sellButton;
    [SerializeField] private Button increaseButton;
    [SerializeField] private Button decreaseButton;
    
    [Header("Selected Fish Info")]
    private string selectedFishName;
    private float selectedFishWeight;
    private int availableQuantity;  // The max quantity from inventory
    private float currentMarketPrice;
    private int currentQuantity = 1;  // The currently selected amount in the UI

    public void SetupSellPanel(string fishName, float weight, int quantity, float marketPrice)
    {
        //Debug.Log($"Setting up sell panel for: {fishName}, Weight: {weight}, Quantity: {quantity}, Market Price: {marketPrice}");
        
        // Reset all values
        currentQuantity = 1;  // Make sure this is set to 1
        selectedFishName = fishName;
        selectedFishWeight = weight;
        availableQuantity = quantity;
        currentMarketPrice = marketPrice;

        // Reset UI elements
        sellPriceInput.text = marketPrice.ToString("F2");
        UpdateQuantityDisplay();  // This will show quantity as 1
        UpdateButtonStates();
        
        //Debug.Log("Sell panel setup complete");
        Debug.Log($"Available quantity: {availableQuantity}, Current quantity: {currentQuantity}");
        gameObject.SetActive(true);
    }

    public void OnIncreaseQuantity()
    {
        Debug.Log($"Attempting to increase quantity. Current: {currentQuantity}, Max: {availableQuantity}");
        if (currentQuantity < availableQuantity)
        {
            currentQuantity++;
            Debug.Log($"Quantity increased to: {currentQuantity}");
            UpdateQuantityDisplay();
            UpdateButtonStates();
        }
        else
        {
            Debug.Log("Cannot increase: at maximum quantity");
        }
    }

    public void OnDecreaseQuantity()
    {
        Debug.Log($"Attempting to decrease quantity. Current: {currentQuantity}");
        if (currentQuantity > 1)
        {
            currentQuantity--;
            Debug.Log($"Quantity decreased to: {currentQuantity}");
            UpdateQuantityDisplay();
            UpdateButtonStates();
        }
        else
        {
            Debug.Log("Cannot decrease: at minimum quantity");
        }
    }

    private void UpdateQuantityDisplay()
    {
        quantityText.text = currentQuantity.ToString();
        Debug.Log($"Quantity display updated to: {currentQuantity}");
    }

    private void UpdateButtonStates()
    {
        if (increaseButton != null)
        {
            increaseButton.interactable = (currentQuantity < availableQuantity);
            //Debug.Log($"Increase button interactable: {increaseButton.interactable}");
        }
        else
        {
            Debug.LogWarning("Increase button reference is missing!");
        }
        
        if (decreaseButton != null)
        {
            decreaseButton.interactable = (currentQuantity > 1);
            //Debug.Log($"Decrease button interactable: {decreaseButton.interactable}");
        }
        else
        {
            Debug.LogWarning("Decrease button reference is missing!");
        }
    }

    public void ValidateAndSell()
    {
        Debug.Log("Sell button clicked - validating inputs");
        if (!ValidateInputs())
        {
            Debug.LogWarning("Input validation failed!");
            return;
        }

        float listedPrice = float.Parse(sellPriceInput.text);
        Debug.Log($"Attempting to list fish: {selectedFishName} x{currentQuantity} at {listedPrice} gold each");
        ListFishInMarket(listedPrice, currentQuantity);
    }

    private bool ValidateInputs()
    {
        // Add debug to see what's in the input field
        Debug.Log($"Attempting to validate price: '{sellPriceInput.text}'");
        
        if (sellPriceInput == null)
        {
            Debug.LogWarning("Price input field is null!");
            return false;
        }

        if (string.IsNullOrEmpty(sellPriceInput.text))
        {
            Debug.LogWarning("Price input is empty!");
            return false;
        }

        if (!float.TryParse(sellPriceInput.text, out float price))
        {
            Debug.LogWarning($"Invalid price format: {sellPriceInput.text}");
            return false;
        }
        
        if (price <= 0)
        {
            Debug.LogWarning($"Price must be greater than 0: {price}");
            return false;
        }

        if (currentQuantity <= 0 || currentQuantity > availableQuantity)
        {
            Debug.LogWarning($"Invalid quantity: {currentQuantity}. Must be between 1 and {availableQuantity}");
            return false;
        }

        Debug.Log("Input validation successful");
        return true;
    }

    private void ListFishInMarket(float price, int quantity)
    {
        Debug.Log($"Opening database connection to list fish in market");
        string dbPath = "URI=file:" + Application.dataPath + "/StreamingAssets/FishDB.db";
        
        try
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();
                Debug.Log("Database connection opened");

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO MarketListings 
                        (FishName, Weight, ListedPrice, ListedTime, IsSold) 
                        VALUES 
                        (@fishName, @weight, @price, @time, 0)";

                    command.Parameters.AddWithValue("@fishName", selectedFishName);
                    command.Parameters.AddWithValue("@weight", selectedFishWeight);
                    command.Parameters.AddWithValue("@price", price);
                    command.Parameters.AddWithValue("@time", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                    Debug.Log($"Executing database command for: {selectedFishName}");
                    command.ExecuteNonQuery();
                    Debug.Log($"Successfully listed {quantity}x {selectedFishName} for {price} gold each");
                    
                    // Close the sell panel
                    gameObject.SetActive(false);
                    
                    // You might want to refresh the inventory display here
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error listing fish in market: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }
}