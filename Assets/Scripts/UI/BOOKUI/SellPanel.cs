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
    private int currentMarketPrice;  // Changed to int
    private int currentQuantity = 1;  // The currently selected amount in the UI

    public void SetupSellPanel(string fishName, float weight, int quantity, float marketPrice)
    {
        //Debug.Log($"Setting up sell panel for: {fishName}, Weight: {weight}, Quantity: {quantity}, Market Price: {marketPrice}");
        
        // Reset all values
        currentQuantity = 1;  // Make sure this is set to 1
        selectedFishName = fishName;
        selectedFishWeight = weight;
        availableQuantity = quantity;
        currentMarketPrice = Mathf.RoundToInt(marketPrice);  // Convert to int

        // Reset UI elements
        sellPriceInput.text = currentMarketPrice.ToString();  // No need for F2 format
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

        int listedPrice = int.Parse(sellPriceInput.text);  // Changed to int.Parse
        Debug.Log($"Attempting to list fish: {selectedFishName} x{currentQuantity} at {listedPrice} gold each");
        ListFishInMarket(listedPrice, currentQuantity);
    }

    private bool ValidateInputs()
    {
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

        if (!int.TryParse(sellPriceInput.text, out int price))  // Changed to int.TryParse
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

    private void ListFishInMarket(int price, int quantity)
    {
        string dbPath = "URI=file:" + Application.dataPath + "/StreamingAssets/FishDB.db";
        
        try
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    // Delete specific number of fish entries from inventory
                    command.CommandText = @"
                        DELETE FROM Inventory 
                        WHERE Name = @fishName 
                        AND rowid IN (
                            SELECT rowid FROM Inventory 
                            WHERE Name = @fishName 
                            LIMIT @deleteCount
                        )";

                    command.Parameters.AddWithValue("@fishName", selectedFishName);
                    command.Parameters.AddWithValue("@deleteCount", quantity);

                    int rowsAffected = command.ExecuteNonQuery();
                    
                    if (rowsAffected != quantity)
                    {
                        Debug.LogError($"Failed to delete correct number of fish! Deleted: {rowsAffected}, Expected: {quantity}");
                        return;
                    }

                    // Get the rarity for this fish
                    command.CommandText = "SELECT Rarity FROM Fish WHERE Name = @fishName";
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@fishName", selectedFishName);
                    string rarity = (string)command.ExecuteScalar();

                    // Create the market listings
                    for(int i = 0; i < quantity; i++)
                    {
                        command.CommandText = @"
                            INSERT INTO MarketListings 
                            (FishName, Rarity, ListedPrice, SellerID, IsSold) 
                            VALUES 
                            (@fishName, @rarity, @price, 0, 0)";  // SellerID 0 for player

                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@fishName", selectedFishName);
                        command.Parameters.AddWithValue("@rarity", rarity);
                        command.Parameters.AddWithValue("@price", price);

                        command.ExecuteNonQuery();
                    }
                    
                    // Reset the panel
                    ResetPanel();

                    // Update the inventory UI
                    FishInventory fishInventory = FindObjectOfType<FishInventory>();
                    if (fishInventory != null)
                    {
                        fishInventory.LoadFishInventory();
                        
                        // Reset BigFishView to default state
                        FishBigView bigView = FindObjectOfType<FishBigView>();
                        if (bigView != null)
                        {
                            bigView.ShowDefaultState();
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error listing fish in market: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }

    private void ResetPanel()
    {
        // Reset quantity to 1
        currentQuantity = 1;
        UpdateQuantityDisplay();
        UpdateButtonStates();
        
        // Reset price input to current market price
        sellPriceInput.text = currentMarketPrice.ToString();  // No need for F2 format
        
        // Optionally show a success message or animation here
        Debug.Log("Panel reset after successful listing");
    }
}