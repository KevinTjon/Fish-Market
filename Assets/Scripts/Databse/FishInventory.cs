using UnityEngine;
using UnityEngine.UI; // If you're still using standard UI components
using TMPro; // Import the TextMeshPro namespace
using System.Collections.Generic;

public class FishInventory : MonoBehaviour
{
    public GameObject content; 
    private List<GameObject> fishSlots = new List<GameObject>();     
    private FishDB fishDB;

    void Start()
    {
        fishDB = FindObjectOfType<FishDB>(); // Get the FishDB instance
        if (fishDB == null)
        {
            Debug.LogError("FishDB instance not found!"); // Log error if not found
            return; // Exit if FishDB is not found
        }
        LoadFishSlots(); 
        LoadFishInventory();
    }

    void LoadFishSlots()
    {
        fishSlots.Clear();
        foreach (Transform child in content.transform)
        {
            fishSlots.Add(child.gameObject);
        }
    }

    void LoadFishInventory()
    {
        List<FishDB.Fish> fishList = fishDB.GetFish(); // Getting a list from the DB on the current player inventory
        Dictionary<string, int> fishQuantity = new Dictionary<string, int>(); // Dictionary to track fish quantities

        // Count the quantities of each fish
        foreach (var fish in fishList)
        {
            if (fishQuantity.ContainsKey(fish.Name))
            {
                fishQuantity[fish.Name]++; // Increment quantity if fish already exists
            }
            else
            {
                fishQuantity[fish.Name] = 1; // Add new fish with quantity 1
            }
        }

        // Now load the fish slots
        for (int i = 0; i < fishSlots.Count; i++)
        {
            if (i < fishQuantity.Count)
            {
                // Get the fish name for the current slot
                string fishName = fishList[i].Name;

                // Inside the Fish Inventory UI there are squares for each fish called FishSlots
                GameObject fishSlot = fishSlots[i];

                // Looks for the Text child component of each slot
                TextMeshProUGUI fishText = fishSlot.GetComponentInChildren<TextMeshProUGUI>();
                if (fishText != null)
                {
                    fishText.text = "X" + fishQuantity[fishName]; // Display fish name and quantity
                }

                // Store the type and rarity in the slot
                FishSlotData slotData = fishSlot.GetComponent<FishSlotData>();
                if (slotData == null)
                {
                    slotData = fishSlot.AddComponent<FishSlotData>(); // Add the component if it doesn't exist
                }
                slotData.fishType = fishList[i].Type; // Store the type
                slotData.fishRarity = fishList[i].Rarity; // Store the rarity
                slotData.quantity = fishQuantity[fishName]; // Set the quantity for the slot

                // Using the asset path stored in the DB, we can path towards the fish prefab
                GameObject fishPrefab = Resources.Load<GameObject>(fishList[i].AssetPath);
                if (fishPrefab != null)
                {
                    // Instantiate the prefab to gather data
                    GameObject instantiatedFish = Instantiate(fishPrefab);
                    Transform spriteTransform = instantiatedFish.transform.Find("Sprite");
                    if (spriteTransform != null)
                    {
                        SpriteRenderer spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
                        if (spriteRenderer != null)
                        {
                            // Set the sprite to the slot
                            slotData.fishImage = spriteRenderer.sprite;
                            Transform imageTransform = fishSlot.transform.Find("Image");
                            if (imageTransform != null)
                            {
                                Image fishImage = imageTransform.GetComponent<Image>();
                                if (fishImage != null)
                                {
                                    fishImage.sprite = spriteRenderer.sprite;
                                }
                                else
                                {
                                    Debug.LogError("Image component not found in Image child at index: " + i);
                                }
                            }
                            else
                            {
                                Debug.LogError("Child GameObject named 'Image' not found in fishSlot at index: " + i);
                            }
                        }
                        else
                        {
                            Debug.LogError("SpriteRenderer not found in Sprite child at index: " + i);
                        }
                    }
                    else
                    {
                        Debug.LogError("Child GameObject named 'Sprite' not found in prefab at index: " + i);
                    }
                    Destroy(instantiatedFish); // Destroy the object after gathering data
                }
                else
                {
                    Debug.LogError("Prefab not found at path: " + fishList[i].AssetPath);
                }
            }
            else
            {
                // Clear the slot if there are no more fish
                TextMeshProUGUI fishText = fishSlots[i].GetComponentInChildren<TextMeshProUGUI>();
                if (fishText != null)
                {
                    fishText.text = "";
                }

                Transform imageTransform = fishSlots[i].transform.Find("Image");
                if (imageTransform != null)
                {
                    Image fishImage = imageTransform.GetComponent<Image>();
                    if (fishImage != null)
                    {
                        fishImage.sprite = null;
                    }
                }
            }
        }
    }
}