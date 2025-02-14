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

    void Start()
    {
        for (int i = 0; i < tabButtons.Length; i++)
        {
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
            return;
        }

        if (isBookOpen)
        {
            bookImage.enabled = false;
            pageImage.enabled = true;
            pageImage.sprite = tabSelectedSprites[currentTab];
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
        isAnimating = false;  // Animation complete
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