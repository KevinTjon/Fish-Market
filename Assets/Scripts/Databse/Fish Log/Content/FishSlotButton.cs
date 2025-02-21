using UnityEngine;

public class FishSlotButton : MonoBehaviour
{
    public FishSlot fishSlot; // Reference to the fish slot data associated with this slot

    public void OnMouseDown() // This method will be called when the slot is clicked
    {
        // Check if fishSlot is null
        if (fishSlot == null)
        {
            Debug.LogError("FishSlot is null! Cannot create ExtensiveTabData.");
            return; // Exit if fishSlot is null
        }

        if (string.IsNullOrEmpty(fishSlot.isDiscovered) || fishSlot.isDiscovered == "No")
        {
            Debug.Log("Fish not yet discovered!");
            return; // Exit the method if fish is not discovered
        }

        // Find the ExtensiveTab by searching up through all parents
        Transform current = transform;
        Transform extensiveTabTransform = null;
        
        while (current != null)
        {
            // Try to find FishDetails at each level
            extensiveTabTransform = current.Find("LeftPage/FishDetails");
            if (extensiveTabTransform != null) break;
            
            // Move up to parent
            current = current.parent;
        }
        
        if (extensiveTabTransform != null)
        {
            extensiveTabTransform.gameObject.SetActive(true);
            // Get the ExtensiveTab component from the found GameObject
            ExtensiveTab extensiveTab = extensiveTabTransform.GetComponent<ExtensiveTab>();
            
            if (extensiveTab != null)
            {
                // Create an instance of ExtensiveTabData from the fishSlot
                ExtensiveTabData extensiveTabData = new ExtensiveTabData(
                    fishSlot.Fishname, // Assuming Fishname is a property of FishSlot
                    fishSlot.description, // Assuming description is a property of FishSlot
                    fishSlot.rarity, // Assuming rarity is a property of FishSlot
                    fishSlot.assetPath, // Assuming assetPath is a property of FishSlot
                    float.Parse(fishSlot.minWeight), // Assuming minWeight is a string property of FishSlot
                    float.Parse(fishSlot.maxWeight), // Assuming maxWeight is a string property of FishSlot
                    float.Parse(fishSlot.topSpeed),//Assuming topSpeed is a string property of FishSlot
                    fishSlot.isDiscovered// Assuming isDiscovered is an integer property of FishSlot
                );

                 extensiveTab.ShowFishDetails(extensiveTabData);
            }
            else
            {
                Debug.LogError("ExtensiveTab component not found on ExtensiveTab GameObject!");
            }
        }
        else
        {
            Debug.LogError("Could not find LeftPage/FishDetails in any parent!");
        }
    }
}