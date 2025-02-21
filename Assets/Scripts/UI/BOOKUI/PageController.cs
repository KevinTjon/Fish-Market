using UnityEngine;
using UnityEngine.UI;

public class PageController : MonoBehaviour
{
    [SerializeField] private Image bookImage;  // Reference to BookImage
    [SerializeField] private Image pageImage;  // Reference to PageImage
    private Animator animator;
    [SerializeField] private Animator bookAnimator; // Reference to BookBase's animator
    private int currentPage = 0;
    //[SerializeField] private int maxPages = 5; // Adjust this to your total pages

    void Start()
    {
        animator = GetComponent<Animator>();
        if (bookAnimator == null)
        {
            // Get the parent's (BookBase) animator if not assigned
            bookAnimator = transform.parent.GetComponent<Animator>();
        }
        // Start with only BookImage visible
        pageImage.enabled = false;
        Debug.Log("Starting on page: " + currentPage);
    }
}