using UnityEngine;

public class FishSlotButton : MonoBehaviour
{
    public FishSlot fishSlot; // Reference to the fish slot data associated with this slot

    private void OnMouseDown() // This method will be called when the slot is clicked
    {
        // Check if fishSlot is null
        if (fishSlot == null)
        {
            Debug.LogError("FishSlot is null! Cannot create ExtensiveTabData.");
            return; // Exit if fishSlot is null
        }

        // Get the parent of the current GameObject (the slot)
        Transform parentTransform = transform.parent;

        // Find the ExtensiveTab GameObject in the parent
        Transform extensiveTabTransform = parentTransform.parent.Find("ExtensiveTab");
        
        if (extensiveTabTransform != null)
        {
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
            Debug.LogError("ExtensiveTab GameObject not found in the parent hierarchy!");
        }
    }
}