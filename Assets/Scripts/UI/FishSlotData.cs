using UnityEngine;

public class FishSlotData : MonoBehaviour
{
    public string Weight;   // To store the type of the fish
    public string fishRarity; // To store the rarity of the fish
    public Sprite fishImage;  // To store the fish image sprite
    public float marketPrice;  // Add this field
    public int quantity;       // To store the quantity of the fish
    public string fishName;
    // Method to call when the button is pressed
    public void BigView()
    {
        // Find the InventoryPage component in the parent hierarchy
        InventoryPage inventoryPage = GetComponentInParent<InventoryPage>();
        if (inventoryPage != null)
        {
            // Find the FishBigView component in the InventoryPage
            FishBigView bigViewComponent = inventoryPage.GetComponentInChildren<FishBigView>();
            if (bigViewComponent != null)
            {
                // Check if the slot has data
                if (!string.IsNullOrEmpty(fishName) && !string.IsNullOrEmpty(fishRarity) && fishImage != null)
                {
                    // Pass the fish name instead of weight
                    bigViewComponent.Setup(fishImage, fishName, fishRarity, quantity, marketPrice);
                    bigViewComponent.ShowFishDetails(); // Show the details
                    // Debug.Log("Sent info to show BIGUI");
                }
                else
                {
                    Debug.LogWarning("Slot has no data. FishBigView will not be updated.");
                }
            }
            else
            {
                Debug.LogError("FishBigView component not found in children of InventoryPage!");
            }
        }
        else
        {
            Debug.LogError("InventoryPage component not found in parent hierarchy!");
        }
    }
}
