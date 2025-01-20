using UnityEngine;
using UnityEngine.UI; // Required for UI components
using TMPro; // Make sure to include this at the top of your file

public class FishBigView : MonoBehaviour
{
    public Sprite displayImage; // Reference to the Image component where the fish image will be displayed
    public string typeText;      // Reference to the Text component for fish type
    public string rarityText;    // Reference to the Text component for fish rarity

     public void Setup(Sprite img, string type, string rarity)
    {
        displayImage = img;
        typeText = type;
        rarityText = rarity;
    }

    // Method to show fish details
    public void ShowFishDetails()
    {
        // Find the Image GameObject that is a child of the Panel_Image
        Transform imageTransform = transform.Find("Panel_Image/Image");
        if (imageTransform != null)
        {
            Image childImage = imageTransform.GetComponent<Image>(); // Get the Image component from the GameObject
            if (childImage != null)
            {
                if (displayImage != null)
                {
                    childImage.sprite = displayImage; // Set the sprite for the child Image
                    Debug.Log("Sprite assigned to child Image.");
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

        // Find the TextMeshPro component for the type in Panel_Type
        Transform typeTransform = transform.Find("Panel_Type/Type");
        if (typeTransform != null)
        {
            TextMeshProUGUI typeTextComponent = typeTransform.GetComponent<TextMeshProUGUI>(); // Get the TextMeshProUGUI component
            if (typeTextComponent != null)
            {
                typeTextComponent.text = !string.IsNullOrEmpty(typeText) ? typeText : "Unknown Type"; // Set the type text
                Debug.Log("Type text assigned.");
            }
            else
            {
                Debug.LogError("TextMeshProUGUI component not found on the type GameObject!");
            }
        }
        else
        {
            Debug.LogError("Child Text GameObject not found under Panel_Type!");
        }

        // Find the TextMeshPro component for the rarity in Panel_Rarity
        Transform rarityTransform = transform.Find("Panel_Rarity/Rarity");
        if (rarityTransform != null)
        {
            TextMeshProUGUI rarityTextComponent = rarityTransform.GetComponent<TextMeshProUGUI>(); // Get the TextMeshProUGUI component
            if (rarityTextComponent != null)
            {
                rarityTextComponent.text = !string.IsNullOrEmpty(rarityText) ? rarityText : "Unknown Rarity"; // Set the rarity text
                Debug.Log("Rarity text assigned.");
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
    }
}