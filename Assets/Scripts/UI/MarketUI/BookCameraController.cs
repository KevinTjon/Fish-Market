using UnityEngine;

public class BookCameraController : MonoBehaviour
{
    [SerializeField] private GameObject book;
    [SerializeField] private float cameraSize = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f);
    [SerializeField] private bool adjustOnStart = true;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            Debug.LogError("Camera component not found!");
            return;
        }

        if (book == null)
        {
            Debug.LogError("Book reference not set!");
            return;
        }

        if (adjustOnStart)
        {
            AdjustCamera();
        }
    }

    void AdjustCamera()
    {
        // Set orthographic size
        if (mainCamera.orthographic)
        {
            mainCamera.orthographicSize = cameraSize;
        }
        
        // Position camera relative to book
        transform.position = book.transform.position + offset;
        
        // Ensure camera is looking at the book
        transform.LookAt(book.transform);
    }

    // Optional: Call this if you need to readjust the camera later
    public void UpdateCameraPosition()
    {
        AdjustCamera();
    }

#if UNITY_EDITOR
    // Optional: Visual helper in editor
    void OnDrawGizmosSelected()
    {
        if (book != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, book.transform.position);
        }
    }
#endif
}