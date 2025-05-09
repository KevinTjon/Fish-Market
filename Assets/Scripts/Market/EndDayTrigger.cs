using UnityEngine;
using UnityEngine.UI;

public class EndDayTrigger : MonoBehaviour
{
    [SerializeField] private GameObject endDayUI; // Reference to the End Day UI
    //[SerializeField] private Button endDayButton;
    //[SerializeField] private Cooler playerCooler; // Reference to player's cooler

    private void Awake()
    {
        if (endDayUI == null)
        {
            Debug.LogWarning("End Day UI not assigned! Make sure to assign the End Day UI reference.");
            return;
        }

        endDayUI.SetActive(false); // Hide the UI initially

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Trigger Enter detected with: {other.gameObject.name}, Tag: {other.tag}");
        // Check if it's the player
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered trigger area");
            endDayUI.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log($"Trigger Exit detected with: {other.gameObject.name}, Tag: {other.tag}");
        // Hide button when player leaves the trigger area
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player exited trigger area");
            endDayUI.SetActive(false);
        }
    }
} 