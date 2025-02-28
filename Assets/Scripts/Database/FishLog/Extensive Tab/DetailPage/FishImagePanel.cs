using UnityEngine;
using UnityEngine.UI;

public class FishImagePanel : MonoBehaviour
{
    private Image fishImage; // Reference to the child Image component

    private void Awake()
    {
        // Get the Image component from the child GameObject
        fishImage = GetComponentInChildren<Image>();

    }

    // Method to set the fish image based on the asset path
    public void SetFishImage(string assetPath)
    {
        // Load the sprite from the asset path
        Sprite sprite = Resources.Load<Sprite>(assetPath);
        if (sprite != null)
        {
            fishImage.sprite = sprite; // Assign the sprite to the Image component
            Debug.Log($"Fish image set from path: {assetPath}");
        }
        else
        {
            Debug.LogWarning($"Sprite not found at path: {assetPath}");
        }
    }
}