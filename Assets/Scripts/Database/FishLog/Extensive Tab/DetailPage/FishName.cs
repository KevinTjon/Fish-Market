using UnityEngine;
using TMPro;

public class FishName : MonoBehaviour
{
    public TextMeshProUGUI nameText; // Reference to the Text component

    // Method to set the fish name
    public void SetFishName(string name)
    {
        if (nameText != null)
        {
            nameText.text = name.ToUpper(); // Set the text to the fish name
        }
        else
        {
            Debug.LogError("NameText reference is not set!");
        }
    }
}