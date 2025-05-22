using UnityEngine;
using UnityEngine.InputSystem;

public class BookUIInteractable : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private GameObject bookUI;
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private GameObject blurImage;

    private bool isPlayerInRange = false;
    private PlayerControls playerControls;

    private void Awake()
    {
        playerControls = new PlayerControls();
        
        // Subscribe to the Interact action
        playerControls.Player.Interact.performed += ctx => TryToggleBookUI();
    }

    private void Start()
    {
        // Hide UI elements at start
        if (bookUI != null) bookUI.SetActive(false);
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        
        Debug.Log($"BookUIInteractable started on {gameObject.name}");
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    private void TryToggleBookUI()
    {
        if (isPlayerInRange)
        {
            if (bookUI != null)
            {
                bookUI.SetActive(!bookUI.activeSelf);
                Debug.Log($"Book UI is now {(bookUI.activeSelf ? "open" : "closed")}");
                // Hide or show the global interaction prompt
                if (bookUI.activeSelf)
                {
                    UIManager.Instance.HideInteractionPrompt();
                    if (blurImage != null) blurImage.SetActive(true);
                }
                else
                {
                    UIManager.Instance.ShowInteractionPrompt();
                    if (blurImage != null) blurImage.SetActive(false);
                }
            }
            else
            {
                Debug.LogError("Book UI reference is missing!");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            Debug.Log("Player entered interaction range");
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
            Debug.Log("Player left interaction range");
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
            // Close UI if player walks away while it's open
            if (bookUI != null && bookUI.activeSelf)
            {
                bookUI.SetActive(false);
                if (blurImage != null) blurImage.SetActive(false);
            }
        }
    }

    // Visualize the interaction area in the Unity Editor
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 2f);
    }
} 