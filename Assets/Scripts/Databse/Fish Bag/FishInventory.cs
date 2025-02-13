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
        List<FishDB.Fish> fishList = fishDB.GetFish();
        Dictionary<string, int> fishQuantity = new Dictionary<string, int>();

        // Count the quantities of each fish
        foreach (var fish in fishList)
        {
            if (fishQuantity.ContainsKey(fish.Name))
            {
                fishQuantity[fish.Name]++;
            }
            else
            {
                fishQuantity[fish.Name] = 1;
            }
        }

        // Now load the fish slots
        for (int i = 0; i < fishSlots.Count; i++)
        {
            if (i < fishQuantity.Count)
            {
                string fishName = fishList[i].Name;
                GameObject fishSlot = fishSlots[i];

                // Update quantity text
                TextMeshProUGUI fishText = fishSlot.GetComponentInChildren<TextMeshProUGUI>();
                if (fishText != null)
                {
                    fishText.text = "X" + fishQuantity[fishName];
                }

                // Store the type and rarity in the slot
                FishSlotData slotData = fishSlot.GetComponent<FishSlotData>();
                if (slotData == null)
                {
                    slotData = fishSlot.AddComponent<FishSlotData>();
                }
                slotData.fishName = fishName;
                slotData.Weight = fishList[i].Weight;
                slotData.fishRarity = fishList[i].Rarity;
                slotData.quantity = fishQuantity[fishName];

                // Load sprite directly from Resources
                string spritePath = fishList[i].AssetPath;
                Sprite fishSprite = Resources.Load<Sprite>(spritePath);
                
                if (fishSprite != null)
                {
                    // Set the sprite to the slot
                    slotData.fishImage = fishSprite;
                    Transform imageTransform = fishSlot.transform.Find("Image");
                    if (imageTransform != null)
                    {
                        Image fishImage = imageTransform.GetComponent<Image>();
                        if (fishImage != null)
                        {
                            fishImage.sprite = fishSprite;
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
                    Debug.LogError("Sprite not found at path: " + spritePath);
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