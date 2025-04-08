using UnityEngine;

public class SeaBackgroundAnimator : MonoBehaviour
{
    [Header("Sea Animation")]
    public Sprite[] seaFrames;        // 4 frames
    public float seaAnimationSpeed = 0.2f;
    
    [Header("Clouds Parallax")]
    public Transform[] cloudLayers;
    public float[] cloudSpeeds = { 0.1f, 0.2f, 0.3f };
    
    [Header("References")]
    [SerializeField] private SpriteRenderer seaRenderer;
    
    private float seaTimer;
    private int currentSeaFrame;
    private float screenWidth;

    private void Start()
    {
        // Try to find the renderer if not set in inspector
        if (seaRenderer == null)
        {
            Transform seaTransform = transform.Find("Sea");
            if (seaTransform != null)
            {
                seaRenderer = seaTransform.Find("SeaAnimation")?.GetComponent<SpriteRenderer>();
            }
        }

        // Validate components
        if (seaRenderer == null)
        {
            Debug.LogError("SeaRenderer not found! Please assign it in the inspector or ensure the GameObject structure is correct.");
            enabled = false;
            return;
        }

        // Validate frames
        if (seaFrames == null || seaFrames.Length == 0)
        {
            Debug.LogError("No sea frames assigned!");
            enabled = false;
            return;
        }

        // Set initial frame
        seaRenderer.sprite = seaFrames[0];
        
        // Calculate screen width
        if (Camera.main != null)
        {
            screenWidth = Camera.main.orthographicSize * Camera.main.aspect * 2;
        }
        else
        {
            Debug.LogError("Main camera not found!");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        // Animate sea
        seaTimer += Time.deltaTime;
        if (seaTimer >= seaAnimationSpeed)
        {
            seaTimer = 0;
            currentSeaFrame = (currentSeaFrame + 1) % seaFrames.Length;
            seaRenderer.sprite = seaFrames[currentSeaFrame];
        }

        // Move clouds
        if (cloudLayers != null)
        {
            for (int i = 0; i < cloudLayers.Length; i++)
            {
                if (cloudLayers[i] != null)
                {
                    cloudLayers[i].Translate(Vector3.right * cloudSpeeds[i] * Time.deltaTime);
                    
                    // Reset position when out of view
                    if (cloudLayers[i].position.x > screenWidth)
                        cloudLayers[i].position = new Vector3(-screenWidth, cloudLayers[i].position.y, 0);
                }
            }
        }
    }
}