using UnityEngine;
using UnityEngine.UI;

public class BookTabController : MonoBehaviour
{
    private int currentTab = 0;
    private int currentPage = 0;
    private bool isAnimating = false;  // New flag to track animation state
    [SerializeField] private Button[] tabButtons;
    [SerializeField] private Image pageImage;
    [SerializeField] private Image bookImage;
    [SerializeField] private Animator bookAnimator;
    [SerializeField] private Animator pagesAnimator;
    [SerializeField] private Sprite[] tabSelectedSprites;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private float selectedTabOffset = 10f;  // How far right to move selected tab

    private Vector3[] originalPositions;  // Store original positions of tab buttons

    void Start()
    {
        // Store original positions
        originalPositions = new Vector3[tabButtons.Length];
        for (int i = 0; i < tabButtons.Length; i++)
        {
            originalPositions[i] = tabButtons[i].transform.localPosition;
            int tabIndex = i;
            tabButtons[i].onClick.AddListener(() => OnTabClick(tabIndex));
        }
        
        if (pageImage != null)
        {
            pageImage.enabled = false;
        }
        
        pagesAnimator.enabled = false;
    }

    void Update()
    {
        bool isBookOpen = bookAnimator.GetBool("IsOpen");
        
        if (!isBookOpen)
        {
            bookImage.enabled = true;
            pageImage.enabled = false;
            SetButtonImagesEnabled(false);
            ResetAllTabPositions();  // Reset positions when book closes
            return;
        }

        if (isBookOpen && !isAnimating)
        {
            bookImage.enabled = false;
            pageImage.enabled = true;
            pageImage.sprite = tabSelectedSprites[currentTab];
            SetButtonImagesEnabled(true);
            UpdateTabPositions();  // Update positions when not animating
        }
    }

    void OnTabClick(int newTab)
    {
        if (!bookAnimator.GetBool("IsOpen")) return;
        if (isAnimating) return;  // Prevent clicks during animation
        if (currentTab == newTab) return;  // Prevent clicking same tab

        Debug.Log($"Clicked tab {newTab}, current page is {currentPage}");

        isAnimating = true;  // Start animation
        pagesAnimator.enabled = true;
        SetButtonImagesEnabled(false);  // Hide button images during flip

        if (newTab > currentTab)
        {
            Debug.Log($"Flipping left from {currentTab} to {newTab}");
            pagesAnimator.SetBool("FlipLeft", true);
            Invoke("ResetLeftFlip", 0.5f);
        }
        else if (newTab < currentTab)
        {
            Debug.Log($"Flipping right from {currentTab} to {newTab}");
            pagesAnimator.SetBool("FlipRight", true);
            Invoke("ResetRightFlip", 0.5f);
        }

        currentTab = newTab;
        currentPage = newTab;
        
        Invoke("UpdateTabVisuals", 0.5f);
    }

    void UpdateTabVisuals()
    {
        pagesAnimator.enabled = false;
        pageImage.sprite = tabSelectedSprites[currentTab];
        SetButtonImagesEnabled(true);  // Show button images after flip
        UpdateTabPositions();  // Update positions after animation
        isAnimating = false;  // Animation complete
    }

    void UpdateTabPositions()
    {
        for (int i = 0; i < tabButtons.Length; i++)
        {
            Vector3 position = originalPositions[i];
            if (i == currentTab)
            {
                position.x += selectedTabOffset;  // Move selected tab right
            }
            tabButtons[i].transform.localPosition = position;
        }
    }

    void ResetAllTabPositions()
    {
        for (int i = 0; i < tabButtons.Length; i++)
        {
            tabButtons[i].transform.localPosition = originalPositions[i];
        }
    }

    void SetButtonImagesEnabled(bool enabled)
    {
        foreach (Button button in tabButtons)
        {
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.enabled = enabled;
            }
        }
    }

    void ResetRightFlip()
    {
        pagesAnimator.SetBool("FlipRight", false);
    }

    void ResetLeftFlip()
    {
        pagesAnimator.SetBool("FlipLeft", false);
    }
}