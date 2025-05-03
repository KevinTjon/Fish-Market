using UnityEngine;
using Market;

public class SellerNPCInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRadius = 2f;
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private GameObject dialogueBox;
    [SerializeField, TextArea(3, 10)] private string[] dialogueLines;

    private bool isPlayerInRange = false;
    private int currentDialogueLine = 0;

    public bool CanInteract => isPlayerInRange;

    private void Start()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
        if (dialogueBox != null)
        {
            dialogueBox.SetActive(false);
        }
    }

    public void OnInteractionStart()
    {
        Debug.Log($"Starting dialogue with {gameObject.name}");
        if (dialogueBox != null)
        {
            dialogueBox.SetActive(true);
            ShowNextDialogueLine();
        }
    }

    public void OnInteractionEnd()
    {
        Debug.Log($"Ending dialogue with {gameObject.name}");
        if (dialogueBox != null)
        {
            dialogueBox.SetActive(false);
            currentDialogueLine = 0;  // Reset dialogue for next interaction
        }
    }

    public void OnInteractionUpdate()
    {
        // Could be used to handle dialogue progression
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public float GetInteractionRadius()
    {
        return interactionRadius;
    }

    private void ShowNextDialogueLine()
    {
        if (dialogueLines != null && dialogueLines.Length > 0)
        {
            string currentLine = dialogueLines[currentDialogueLine];
            Debug.Log($"Dialogue: {currentLine}");
            // Here you would update your dialogue UI with the current line
            // For example:
            // dialogueText.text = currentLine;

            currentDialogueLine = (currentDialogueLine + 1) % dialogueLines.Length;
        }
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
            // Also ensure dialogue is closed if player walks away
            if (dialogueBox != null)
            {
                dialogueBox.SetActive(false);
                currentDialogueLine = 0;
            }
        }
    }
} 