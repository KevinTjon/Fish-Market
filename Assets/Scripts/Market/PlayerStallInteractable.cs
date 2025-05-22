using UnityEngine;
using Market;

public class PlayerStallInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRadius = 2f;
    [SerializeField] private GameObject interactionPrompt;  // UI prompt to show when player is near
    [SerializeField] private GameObject marketUI;  // Reference to your market UI panel

    private bool isPlayerInRange = false;

    public bool CanInteract => isPlayerInRange;

    private void Start()
    {
        // Ensure prompt is hidden at start
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }

    public void OnInteractionStart()
    {
        Debug.Log("Opening Player Market UI");
        if (marketUI != null)
        {
            marketUI.SetActive(true);
            // You might want to pause the game or disable player movement here
        }
    }

    public void OnInteractionEnd()
    {
        Debug.Log("Closing Player Market UI");
        if (marketUI != null)
        {
            marketUI.SetActive(false);
            // Resume game or enable player movement here
        }
    }

    public void OnInteractionUpdate()
    {
        // Can be used for continuous updates while interacting
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public float GetInteractionRadius()
    {
        return interactionRadius;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
        }
    }
} 