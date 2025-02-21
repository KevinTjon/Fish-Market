using UnityEngine;

public class CloseButton : MonoBehaviour
{
    // Method to deactivate the parent GameObject
    public void DeactivateTab()
    {
        // Check if the parent exists
        if (transform.parent != null)
        {
            // Deactivate the parent GameObject
            transform.parent.gameObject.SetActive(false);
            Debug.Log("Parent tab deactivated.");
        }
        else
        {
            Debug.LogWarning("No parent found to deactivate.");
        }
    }

    // You can add other methods or functionality here as needed
}