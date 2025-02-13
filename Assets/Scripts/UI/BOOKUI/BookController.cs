using UnityEngine;

public class BookController : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        // Remove the automatic SetBool here
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            bool isOpen = animator.GetBool("IsOpen");
            animator.SetBool("IsOpen", !isOpen);
        }
    }
}