using UnityEngine;
using TMPro;

public class Description : MonoBehaviour
{
    public TextMeshProUGUI descriptionText; // Reference to the Text component

    // Method to set the description
    public void SetDescription(string description)
    {
        if (descriptionText != null)
        {
            descriptionText.text = description.ToUpper(); // Set the text to the description
        }
        else
        {
            Debug.LogError("DescriptionText reference is not set!");
        }
    }
}