using UnityEngine;
using UnityEngine.InputSystem;

public class BookController : MonoBehaviour
{
    [SerializeField] private GameObject endDayButton;
    private Animator animator;
    private EndDayButton endDayButtonComponent;

    void OnEnable()
    {
        InitializeComponents();
    }

    void Start()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        animator = GetComponent<Animator>();
        if (endDayButton != null)
        {
            endDayButtonComponent = endDayButton.GetComponent<EndDayButton>();
            // Set initial state based on book state
            if (endDayButtonComponent != null && animator != null)
            {
                bool isOpen = animator.GetBool("IsOpen");
                endDayButtonComponent.ShowButtons(isOpen);
            }
        }
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            bool isOpen = animator.GetBool("IsOpen");
            animator.SetBool("IsOpen", !isOpen);
            // Show button when book is open, hide when closed
            if (endDayButtonComponent != null)
            {
                endDayButtonComponent.ShowButtons(!isOpen);
            }
        }
    }
}