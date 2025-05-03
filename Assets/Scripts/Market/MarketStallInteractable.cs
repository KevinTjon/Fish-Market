using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class MarketStallInteractable : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent onInteract;

    private bool playerInRange = false;
    private Collider2D interactionTrigger;

    private void Start()
    {
        // Get or add trigger collider
        interactionTrigger = GetComponent<Collider2D>();
        interactionTrigger.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            UIManager.Instance.ShowInteractionPrompt();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            UIManager.Instance.HideInteractionPrompt();
        }
    }

    public void TryInteract()
    {
        if (playerInRange)
        {
            onInteract?.Invoke();
        }
    }
} 