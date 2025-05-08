using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject marketplaceUI;
    [SerializeField] private TextMeshProUGUI interactionPromptText;
    [SerializeField] private GameObject blurImage;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Hide UI elements at start
        if (marketplaceUI != null) marketplaceUI.SetActive(false);
        if (interactionPromptText != null) interactionPromptText.gameObject.SetActive(false);
    }

    public void ShowInteractionPrompt()
    {
        if (interactionPromptText != null)
        {
            interactionPromptText.gameObject.SetActive(true);
        }
    }

    public void HideInteractionPrompt()
    {
        if (interactionPromptText != null)
        {
            interactionPromptText.gameObject.SetActive(false);
        }
    }

    public bool IsInteractionPromptVisible()
    {
        return interactionPromptText != null && interactionPromptText.gameObject.activeSelf;
    }

    public void ShowMarketplaceUI()
    {
        if (marketplaceUI != null)
        {
            marketplaceUI.SetActive(true);
            HideInteractionPrompt(); // Hide the prompt when marketplace UI opens
            // Optional: Pause game or disable player movement
            Time.timeScale = 0f;
        }
    }

    public void HideMarketplaceUI()
    {
        if (marketplaceUI != null)
        {
            marketplaceUI.SetActive(false);
            // Resume game
            Time.timeScale = 1f;
            if (blurImage != null) blurImage.SetActive(false);
        }
    }
} 