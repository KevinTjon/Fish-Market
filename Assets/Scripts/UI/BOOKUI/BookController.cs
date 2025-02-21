using UnityEngine;

public class BookController : MonoBehaviour
{
    [SerializeField] private GameObject endDayButton;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            bool isOpen = animator.GetBool("IsOpen");
            animator.SetBool("IsOpen", !isOpen);
            // Show button when book is open, hide when closed
            endDayButton.SetActive(isOpen);
        }
    }
}