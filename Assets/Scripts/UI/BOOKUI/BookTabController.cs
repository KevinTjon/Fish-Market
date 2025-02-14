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
    [Header("Page Content")]
    [SerializeField] private GameObject[] pageContents;  // Array of page content objects

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
        
        // Initialize all pages as hidden
        foreach (GameObject content in pageContents)
        {
            if (content != null)
            {
                content.SetActive(false);
            }
        }
        
        // Show initial page if book starts open
        if (bookAnimator.GetBool("IsOpen") && pageContents.Length > 0 && pageContents[0] != null)
        {
            pageContents[0].SetActive(true);
        }
    }

    void Update()
    {
        bool isBookOpen = bookAnimator.GetBool("IsOpen");
        
        if (!isBookOpen)
        {
            bookImage.enabled = true;
            pageImage.enabled = false;
            SetButtonImagesEnabled(false);
            ResetAllTabPositions();
            
            // Hide all content when book is closed
            foreach (GameObject content in pageContents)
            {
                if (content != null)
                {
                    content.SetActive(false);
                }
            }
            return;
        }

        if (isBookOpen && !isAnimating)
        {
            bookImage.enabled = false;
            pageImage.enabled = true;
            pageImage.sprite = tabSelectedSprites[currentTab];
            SetButtonImagesEnabled(true);
            UpdateTabPositions();
            
            // Show current tab content when book is open
            if (currentTab >= 0 && currentTab < pageContents.Length)
            {
                pageContents[currentTab].SetActive(true);
            }
        }
    }

    void OnTabClick(int newTab)
    {
        if (!bookAnimator.GetBool("IsOpen")) return;
        if (isAnimating) return;
        if (currentTab == newTab) return;

        Debug.Log($"Clicked tab {newTab}, current page is {currentPage}");

        isAnimating = true;
        pagesAnimator.enabled = true;
        SetButtonImagesEnabled(false);

        // Hide current page content
        if (currentTab >= 0 && currentTab < pageContents.Length)
        {
            pageContents[currentTab].SetActive(false);
        }

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
        
        // Show new page content after animation
        Invoke("UpdateTabVisuals", 0.5f);
    }

    void UpdateTabVisuals()
    {
        pagesAnimator.enabled = false;
        pageImage.sprite = tabSelectedSprites[currentTab];
        SetButtonImagesEnabled(true);
        UpdateTabPositions();

        // Show new page content
        if (currentTab >= 0 && currentTab < pageContents.Length)
        {
            pageContents[currentTab].SetActive(true);
        }

        isAnimating = false;
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