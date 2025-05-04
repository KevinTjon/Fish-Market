using UnityEngine;
using UnityEngine.UI;

public class EndDayTrigger : MonoBehaviour
{
    [SerializeField] private Button endDayButton;
    [SerializeField] private Cooler playerCooler; // Reference to player's cooler

    private void Start()
    {
        // Ensure we have a BoxCollider2D and set it as trigger
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        boxCollider.isTrigger = true;

        // Hide button initially
        if (endDayButton != null)
        {
            endDayButton.gameObject.SetActive(false);
            endDayButton.onClick.AddListener(OnEndDayClick);
        }

        // Find the cooler if not assigned
        if (playerCooler == null)
        {
            playerCooler = FindObjectOfType<Cooler>();
            if (playerCooler == null)
            {
                Debug.LogWarning("No Cooler found in scene! Make sure to assign the Cooler reference.");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if it's the player
        if (other.CompareTag("Player"))
        {
            if (endDayButton != null)
            {
                endDayButton.gameObject.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Hide button when player leaves the trigger area
        if (other.CompareTag("Player"))
        {
            if (endDayButton != null)
            {
                endDayButton.gameObject.SetActive(false);
            }
        }
    }

    private void OnEndDayClick()
    {
        // Ensure GameSceneManager exists
        GameSceneManager.EnsureExists();
        
        // Save cooler contents to inventory
        if (playerCooler != null)
        {
            GameSceneManager.Instance.SaveCoolerToInventory(playerCooler);
            Debug.Log("Saved cooler contents to inventory");
        }
        else
        {
            Debug.LogWarning("No Cooler reference found when trying to save!");
        }
        
        // Load the market scene
        GameSceneManager.Instance.LoadMarketScene();
    }
} 