using UnityEngine;
using UnityEngine.UI;

public class BookTabController : MonoBehaviour
{
    private int currentTab = -1;  // -1 means no tab selected
    [SerializeField] private Button[] tabButtons;
    [SerializeField] private Image pageImage;  // Reference to the page image
    [SerializeField] private Sprite[] tabSelectedSprites;  // Different tab selection states
    [SerializeField] private Sprite normalSprite;  // Book with no tab selected

    void Start()
    {
        Debug.Log($"PageImage reference: {pageImage != null}");
        Debug.Log($"Number of tab sprites: {tabSelectedSprites.Length}");
        
        // Set up click listeners for each tab
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int tabIndex = i;  // Needed for closure
            tabButtons[i].onClick.AddListener(() => OnTabClick(tabIndex));
        }

        // Set initial sprite
        if (pageImage != null && normalSprite != null)
        {
            pageImage.sprite = normalSprite;
        }
    }

    void OnTabClick(int tabIndex)
    {
        Debug.Log($"Tab {tabIndex} clicked");
        if (currentTab != tabIndex)
        {
            currentTab = tabIndex;
            Debug.Log($"Switching to tab {tabIndex}");
            if (currentTab >= 0 && currentTab < tabSelectedSprites.Length)
            {
                Debug.Log($"Setting sprite for tab {tabIndex}");
                pageImage.sprite = tabSelectedSprites[currentTab];
            }
        }
        else
        {
            Debug.Log("Deselecting tab");
            currentTab = -1;
            pageImage.sprite = normalSprite;
        }
    }
}