using UnityEngine;
using TMPro;

public class Rarity : MonoBehaviour
{
    public TextMeshProUGUI rarityText; // Reference to the Text component

    // Method to set the rarity
    public void SetRarity(string rarity)
    {
        if (rarityText != null)
        {
            rarityText.text = rarity.ToUpper(); // Set the text to the rarity
        }
        else
        {
            Debug.LogError("RarityText reference is not set!");
        }
    }
}