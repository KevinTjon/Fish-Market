using UnityEngine;
using UnityEngine.InputSystem;

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
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            bool isOpen = animator.GetBool("IsOpen");
            animator.SetBool("IsOpen", !isOpen);
            // Show button when book is open, hide when closed
            endDayButton.SetActive(isOpen);
        }
    }
}