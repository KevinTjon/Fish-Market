using UnityEngine;
using UnityEngine.UI; // Required for UI components
using TMPro; // Make sure to include this at the top of your file
using Mono.Data.Sqlite;
using System.Data;
using System;  // Add this for DBNull

public class FishBigView : MonoBehaviour
{
    public Sprite displayImage; // Reference to the Image component where the fish image will be displayed
    public string fishName;      // Changed from typeText to fishName
    public string rarityText;    // Reference to the Text component for fish rarity

    public int quantityText;
    private float currentMarketPrice;
    private string Weight;  // Store the weight of the fish

    [SerializeField] private SellPanel sellPanel;

    public void Setup(Sprite img, string name, string rarity, int qty, string weight)
    {
        displayImage = img;
        fishName = name;         // Updated to use fishName
        rarityText = rarity;
        quantityText = qty;
        Weight = weight;  // Store the weight
        FetchLatestMarketPrice();
    }

    private void FetchLatestMarketPrice()
    {
        string dbPath = "URI=file:" + Application.dataPath + "/StreamingAssets/FishDB.db";
        using (IDbConnection dbConnection = new SqliteConnection(dbPath))
        {
            dbConnection.Open();
            using (IDbCommand cmd = dbConnection.CreateCommand())
            {
                // Get the latest day's price for this fish
                cmd.CommandText = @"
                    SELECT Price 
                    FROM MarketPrices 
                    WHERE FishName = @fishName 
                    AND Day = (SELECT MAX(Day) FROM MarketPrices)
                    LIMIT 1";

                var parameter = cmd.CreateParameter();
                parameter.ParameterName = "@fishName";
                parameter.Value = fishName;
                cmd.Parameters.Add(parameter);

                try
                {
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        currentMarketPrice = float.Parse(result.ToString());
                        //Debug.Log($"Fetched price for {fishName}: {currentMarketPrice}");
                    }
                    else
                    {
                        currentMarketPrice = 0;
                        Debug.LogWarning($"No price found for fish: {fishName}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error fetching price: {e.Message}");
                    currentMarketPrice = 0;
                }
            }
        }
    }

    // Method to show fish details
    public void ShowFishDetails()
    {
        // Find the Image GameObject that is a child of the Panel_Image
        Transform imageTransform = transform.Find("FishIconBorder/FishIcon");
        if (imageTransform != null)
        {
            Image childImage = imageTransform.GetComponent<Image>(); // Get the Image component from the GameObject
            if (childImage != null)
            {
                if (displayImage != null)
                {
                    childImage.sprite = displayImage; // Set the sprite for the child Image
                    //Debug.Log("Sprite assigned to child Image.");
                }
                else
                {
                    Debug.LogWarning("Display image is not set! Cannot assign sprite to child Image.");
                }
            }
            else
            {
                Debug.LogError("Child Image component not found on the Image GameObject!");
            }
        }
        else
        {
            Debug.LogError("Child Image GameObject not found under Panel_Image!");
        }

         // Find the TextMeshPro component for the quantity in Panel_Image
        Transform qtyTransform = transform.Find("FishQuantity");
        if (qtyTransform != null)
        {
            TextMeshProUGUI qtyTextComponent = qtyTransform.GetComponent<TextMeshProUGUI>(); // Get the TextMeshProUGUI component
            if (qtyTextComponent != null)
            {
                qtyTextComponent.text = "x" + quantityText; // Set the quantity text
                //Debug.Log("Quantity text assigned.");
            }
            else
            {
                Debug.LogError("TextMeshProUGUI component not found on the qty GameObject!");
            }
        }
        else
        {
            Debug.LogError("Child qty GameObject not found under Panel_Image!");
        }

        // Find the TextMeshPro component for the fish name in Panel_Type
        Transform nameTransform = transform.Find("FishName");  // Keep the same path, just showing fish name now
        if (nameTransform != null)
        {
            TextMeshProUGUI nameTextComponent = nameTransform.GetComponent<TextMeshProUGUI>();
            if (nameTextComponent != null)
            {
                nameTextComponent.text = "Fish: " + (!string.IsNullOrEmpty(fishName) ? fishName : "Unknown Fish");
                //Debug.Log("Fish name assigned.");
            }
            else
            {
                Debug.LogError("TextMeshProUGUI component not found on the name GameObject!");
            }
        }
        else
        {
            Debug.LogError("Child Text GameObject not found under Panel_Type!");
        }

        // Find the TextMeshPro component for the rarity in Panel_Rarity
        Transform rarityTransform = transform.Find("FishRarity");
        if (rarityTransform != null)
        {
            TextMeshProUGUI rarityTextComponent = rarityTransform.GetComponent<TextMeshProUGUI>(); // Get the TextMeshProUGUI component
            if (rarityTextComponent != null)
            {
                rarityTextComponent.text = "Rarity: " + (!string.IsNullOrEmpty(rarityText) ? rarityText : "Unknown Rarity"); // Set the rarity text
                // Debug.Log("Rarity text assigned.");
            }
            else
            {
                Debug.LogError("TextMeshProUGUI component not found on the rarity GameObject!");
            }
        }
        else
        {
            Debug.LogError("Child Text GameObject not found under Panel_Rarity!");
        }

        // New market price display
        Transform priceTransform = transform.Find("MarketPrice");
        if (priceTransform != null)
        {
            TextMeshProUGUI priceTextComponent = priceTransform.GetComponent<TextMeshProUGUI>();
            if (priceTextComponent != null)
            {
                priceTextComponent.text = $"Market Price: {currentMarketPrice:F2} g";
            }
        }

        // Setup sell panel with current fish data
        if (sellPanel != null)
        {
            // First deactivate and reactivate the sell panel to reset its state
            sellPanel.gameObject.SetActive(false);
            
            Debug.Log($"Setting up sell panel for: {fishName}, Quantity: {quantityText}");
            sellPanel.SetupSellPanel(
                fishName,
                float.Parse(Weight),
                quantityText,
                currentMarketPrice
            );
        }
        else
        {
            Debug.LogError("Sell Panel reference not set in FishBigView!");
        }
    }

    public void OnSellButtonClick()
    {
        if (sellPanel != null)
        {
            Debug.Log($"Attempting to open sell panel with quantity: {quantityText}");
            sellPanel.SetupSellPanel(
                fishName,
                float.Parse(Weight),
                quantityText,
                currentMarketPrice
            );
        }
        else
        {
            Debug.LogError("Sell Panel reference not set!");
        }
    }
}