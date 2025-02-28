using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class FishSlotManager : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private GameObject content;
    [SerializeField] private Sprite defaultFishSprite;
    
    [Header("Runtime Data")]
    private List<GameObject> fishSlots = new List<GameObject>();
    private bool isInitialized = false;

    void Awake()
    {
        ValidateRequiredComponents();
    }

    void Start()
    {
        LoadFishSlots();
    }

    private void ValidateRequiredComponents()
    {
        if (content == null)
        {
            Debug.LogError($"Content GameObject is not assigned in {gameObject.name}'s FishSlotManager!");
            return;
        }

        if (defaultFishSprite == null)
        {
            Debug.LogError($"Default Fish Sprite is not assigned in {gameObject.name}'s FishSlotManager!");
            return;
        }
    }

    public bool IsSetupComplete()
    {
        if (content == null || defaultFishSprite == null)
        {
            return false;
        }

        // Check if content has any child objects (slots)
        return content.transform.childCount > 0;
    }

    public void LoadFishSlots()
    {
        if (!IsSetupComplete())
        {
            Debug.LogError($"Cannot load fish slots in {gameObject.name}'s FishSlotManager - setup is incomplete!");
            return;
        }

        fishSlots.Clear();
        foreach (Transform child in content.transform)
        {
            if (ValidateSlotPrefab(child.gameObject))
            {
                fishSlots.Add(child.gameObject);
            }
        }

        isInitialized = fishSlots.Count > 0;
        
        if (!isInitialized)
        {
            Debug.LogError($"No valid fish slots found in {gameObject.name}'s FishSlotManager!");
        }
    }

    private bool ValidateSlotPrefab(GameObject slot)
    {
        // Check for required components
        if (slot.GetComponentInChildren<TextMeshProUGUI>() == null)
        {
            Debug.LogError($"Fish slot {slot.name} is missing TextMeshProUGUI component!");
            return false;
        }

        Transform fishImage = slot.transform.Find("FishImage");
        if (fishImage == null || fishImage.GetComponent<Image>() == null)
        {
            Debug.LogError($"Fish slot {slot.name} is missing FishImage with Image component!");
            return false;
        }

        if (slot.GetComponent<FishSlotData>() == null)
        {
            slot.AddComponent<FishSlotData>();
        }

        return true;
    }

    public void ClearAllSlots()
    {
        if (!isInitialized)
        {
            LoadFishSlots();
        }

        foreach (GameObject slot in fishSlots)
        {
            ClearSlot(slot);
        }
    }

    private void ClearSlot(GameObject slot)
    {
        if (slot == null) return;

        // Clear the quantity text
        TextMeshProUGUI fishText = slot.GetComponentInChildren<TextMeshProUGUI>();
        if (fishText != null)
        {
            fishText.text = "";
        }

        // Clear the slot data
        FishSlotData slotData = slot.GetComponent<FishSlotData>();
        if (slotData != null)
        {
            slotData.fishName = "";
            slotData.fishRarity = "";
            slotData.quantity = 0;
            slotData.marketPrice = 0f;
            slotData.fishImage = defaultFishSprite;

            // Reset the fish image
            Transform fishImageTransform = slot.transform.Find("FishImage");
            if (fishImageTransform != null)
            {
                Image fishImage = fishImageTransform.GetComponent<Image>();
                if (fishImage != null)
                {
                    fishImage.sprite = defaultFishSprite;
                }
            }
        }
    }

    public void UpdateSlot(int slotIndex, Fish fish, int quantity, float marketPrice)
    {
        if (!isInitialized)
        {
            LoadFishSlots();
        }

        if (slotIndex >= fishSlots.Count)
        {
            Debug.LogError($"Slot index {slotIndex} is out of range in {gameObject.name}'s FishSlotManager");
            return;
        }

        GameObject fishSlot = fishSlots[slotIndex];
        if (fishSlot == null)
        {
            Debug.LogError($"Fish slot at index {slotIndex} is null in {gameObject.name}'s FishSlotManager");
            return;
        }

        // Update quantity text
        TextMeshProUGUI fishText = fishSlot.GetComponentInChildren<TextMeshProUGUI>();
        if (fishText != null)
        {
            fishText.text = "X" + quantity;
        }

        // Update slot data
        FishSlotData slotData = fishSlot.GetComponent<FishSlotData>();
        if (slotData == null)
        {
            slotData = fishSlot.AddComponent<FishSlotData>();
        }

        slotData.fishName = fish.Name;
        slotData.fishRarity = fish.Rarity;
        slotData.quantity = quantity;
        slotData.marketPrice = marketPrice;

        // Load and set the fish sprite
        Sprite fishSprite = Resources.Load<Sprite>(fish.AssetPath);
        slotData.fishImage = fishSprite ?? defaultFishSprite;

        // Update fish image
        Transform fishImageTransform = fishSlot.transform.Find("FishImage");
        if (fishImageTransform != null)
        {
            Image fishImage = fishImageTransform.GetComponent<Image>();
            if (fishImage != null)
            {
                fishImage.sprite = slotData.fishImage;
            }
        }
    }

    public int GetSlotCount()
    {
        if (!isInitialized)
        {
            LoadFishSlots();
        }
        return fishSlots.Count;
    }
} 