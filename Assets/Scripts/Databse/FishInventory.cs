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

    void LoadFishInventory(){
        List<FishDB.Fish> fishList = fishDB.GetFish(); // Getting a list from the DB on the current player inventory
        for (int i = 0; i < fishSlots.Count; i++){
            if (i < fishList.Count){
                //Inside the Fish Inventory UI there are squares for each fish called FishSlots, this allows us to have each slot store different data
                GameObject fishSlot = fishSlots[i]; 

                //Looks for the Text child component of each slot. We are using TextMeshPro becuase its a default child component of the button object in UI
                TextMeshProUGUI fishText = fishSlot.GetComponentInChildren<TextMeshProUGUI>();

                //Sets the button name to the fish name *CURRENTLY NOT IN USE
                fishText.text = fishList[i].Name;

                // Store the type and rarity in the slot
                FishSlotData slotData = fishSlot.GetComponent<FishSlotData>();
                if (slotData == null){
                    slotData = fishSlot.AddComponent<FishSlotData>(); // Add the component if it doesn't exist
                }
                slotData.fishType = fishList[i].Type; // Store the type
                slotData.fishRarity = fishList[i].Rarity; // Store the rarity

                //Using the asset path stored in the DB, we can path towards the fish prefab where we can take all the data necessary for each fish without needing to store all
                //that data in the DB. This looks for the fishPrefab in the directory : Assets/Resources/Fish/Prefab.prefab. Note thats not the path stored inside the db
                GameObject fishPrefab = Resources.Load<GameObject>(fishList[i].AssetPath);
                if (fishPrefab != null){
                    //Once we find the prefab instantiate it, creating an object so we can take all the neccessary data it has
                    GameObject instantiatedFish = Instantiate(fishPrefab);
                    //Takes the sprite of the fish located in the child of the object called "Sprite"
                    Transform spriteTransform = instantiatedFish.transform.Find("Sprite");
                    if (spriteTransform != null){
                        SpriteRenderer spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
                        if (spriteRenderer != null){
                            //Now that we have the fish image, we go into the slot object and find its child object called image
                            Transform imageTransform = fishSlot.transform.Find("Image");
                            if (imageTransform != null){
                                //Take the actual image component from the child object
                                Image fishImage = imageTransform.GetComponent<Image>();
                                if (fishImage != null){
                                    //set the found sprite to the image component
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
                    //We destroy the object now since we only used the object to gather data.
                    Destroy(instantiatedFish);
                }
                else{
                    Debug.LogError("Prefab not found at path: " + fishList[i].AssetPath);
                }
            }
            else{
                //If there are no more fish in the list but there are still slots, clear the sprite image and text
                TextMeshProUGUI fishText = fishSlots[i].GetComponentInChildren<TextMeshProUGUI>();
                if (fishText != null){
                    fishText.text = "";
                }

                Transform imageTransform = fishSlots[i].transform.Find("Image");
                if (imageTransform != null){
                    Image fishImage = imageTransform.GetComponent<Image>();
                    if (fishImage != null){
                        fishImage.sprite = null;
                    }
                }
            }
        }
    }
}