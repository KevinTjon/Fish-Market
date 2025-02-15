using UnityEngine;
using UnityEngine.UI; // Required for UI components
using TMPro; // Make sure to include this at the top of your file
using Mono.Data.Sqlite;
using System.Data;
using System;  // Add this for DBNull

public class FishBigView : MonoBehaviour
{
    public Sprite displayImage;
    public string fishName;
    public string rarityText;
    public string quantityText;
    public string marketPriceText;
    private float currentMarketPrice;
    private int currentQuantity;

    [SerializeField] private SellPanel sellPanel;
    [SerializeField] private Sprite defaultSprite;

    // References to UI components
    private TextMeshProUGUI nameTextComponent;
    private TextMeshProUGUI rarityTextComponent;
    private TextMeshProUGUI quantityTextComponent;
    private TextMeshProUGUI marketPriceTextComponent;

    void Start()
    {
        // Find all text components
        nameTextComponent = transform.Find("FishName").GetComponent<TextMeshProUGUI>();
        rarityTextComponent = transform.Find("FishRarity").GetComponent<TextMeshProUGUI>();
        quantityTextComponent = transform.Find("FishQuantity").GetComponent<TextMeshProUGUI>();
        marketPriceTextComponent = transform.Find("MarketPrice").GetComponent<TextMeshProUGUI>();

        ShowDefaultState();
    }

    public void ShowDefaultState()
    {
        displayImage = defaultSprite;
        fishName = "Fish:";
        rarityText = "Rarity: ";
        quantityText = "";
        marketPriceText = "Market Price:";
        currentMarketPrice = 0;
        currentQuantity = 0;

        // Update UI components
        if (nameTextComponent) nameTextComponent.text = fishName;
        if (rarityTextComponent) rarityTextComponent.text = rarityText;
        if (quantityTextComponent) quantityTextComponent.text = quantityText;
        if (marketPriceTextComponent) marketPriceTextComponent.text = marketPriceText;

        // Find and update the fish image
        Transform imageTransform = transform.Find("FishIconBorder/FishIcon");
        if (imageTransform != null)
        {
            Image fishImage = imageTransform.GetComponent<Image>();
            if (fishImage != null)
            {
                fishImage.sprite = defaultSprite;
            }
        }

        // Make sure sell panel is hidden
        if (sellPanel != null)
            sellPanel.gameObject.SetActive(true);

        Debug.Log("FishBigView reset to default state");
    }

    public void Setup(Sprite img, string name, string rarity, int qty, float marketPrice)
    {
        displayImage = img;
        fishName = "Fish: " + name;
        rarityText = "Rarity: " + rarity;
        currentQuantity = qty;
        quantityText = "x" + qty;
        marketPriceText = "Market Price: " + marketPrice + "g";
        currentMarketPrice = marketPrice;

        // Update UI components
        if (nameTextComponent) nameTextComponent.text = fishName;
        if (rarityTextComponent) rarityTextComponent.text = rarityText;
        if (quantityTextComponent) quantityTextComponent.text = quantityText;
        if (marketPriceTextComponent) marketPriceTextComponent.text = marketPriceText;

        // Find and update the fish image
        Transform imageTransform = transform.Find("FishIconBorder/FishIcon");
        if (imageTransform != null)
        {
            Image fishImage = imageTransform.GetComponent<Image>();
            if (fishImage != null)
            {
                fishImage.sprite = displayImage;
            }
        }

        if (sellPanel != null)
        {
            sellPanel.SetupSellPanel(
                name,  // Pass just the name without "Fish: "
                currentMarketPrice,
                currentQuantity,
                currentMarketPrice
            );
        }
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

    public void ShowFishDetails()
    {
        // No need to update anything here as Setup already handles everything
        Debug.Log($"Showing details for: {fishName}, Quantity: {currentQuantity}");
    }

    public void OnSellButtonClick()
    {
        if (sellPanel != null)
        {
            Debug.Log($"Attempting to open sell panel with quantity: {currentQuantity}");
            sellPanel.SetupSellPanel(
                fishName,
                currentMarketPrice,
                currentQuantity,
                currentMarketPrice
            );
        }
        else
        {
            Debug.LogError("Sell Panel reference not set!");
        }
    }
}