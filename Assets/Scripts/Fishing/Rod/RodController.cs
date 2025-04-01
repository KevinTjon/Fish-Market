using UnityEngine;

public class RodController : MonoBehaviour
{
    [Header("Rod Settings")]
    [SerializeField] private float maxLineLength = 10f;
    [SerializeField] private float castPowerMultiplier = 10f;
    [SerializeField] private float reelSpeed = 5f;
    [SerializeField] private float sinkSpeed = 3f;

    [Header("References")]
    [SerializeField] private Transform rodTip;
    [SerializeField] private GameObject hookPrefab;
    
    private LineRenderer fishingLine;
    private GameObject currentHook;
    private Vector2 hookVelocity;
    private bool isCasting;
    private bool isReeling;
    private float currentLineLength;
    private Vector2 castStartPosition;

    private void Start()
    {
        // Setup line renderer
        fishingLine = gameObject.AddComponent<LineRenderer>();
        fishingLine.startWidth = 0.05f;
        fishingLine.endWidth = 0.05f;
        fishingLine.material = new Material(Shader.Find("Sprites/Default"));
        fishingLine.startColor = Color.black;
        fishingLine.endColor = Color.black;
        fishingLine.positionCount = 2;

        SpawnHook();
    }

    private void SpawnHook()
    {
        // Spawn hook at rod tip
        if (currentHook != null) Destroy(currentHook);
        currentHook = Instantiate(hookPrefab, rodTip.position, Quaternion.identity);
        currentHook.transform.SetParent(transform);
        hookVelocity = Vector2.zero;
        currentLineLength = 0;
        UpdateLinePositions();
    }

    private void Update()
    {
        HandleInput();
        UpdateHookMovement();
        UpdateLinePositions();
    }

    private void HandleInput()
    {
        // Start casting
        if (Input.GetMouseButtonDown(0) && !isCasting)
        {
            castStartPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            isCasting = true;
            isReeling = false;
        }
        
        // Release cast
        if (Input.GetMouseButtonUp(0) && isCasting)
        {
            Vector2 castEndPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 castDirection = (castStartPosition - castEndPosition).normalized;
            float castPower = Mathf.Clamp(Vector2.Distance(castStartPosition, castEndPosition), 0, 2);
            
            hookVelocity = castDirection * castPower * castPowerMultiplier;
            isCasting = false;
        }

        // Reeling
        if (Input.GetKey(KeyCode.Space))
        {
            isReeling = true;
        }
        else
        {
            isReeling = false;
        }
    }

    private void UpdateHookMovement()
    {
        if (currentHook == null) return;

        if (!isCasting)
        {
            // Apply gravity when not being cast
            if (!isReeling)
            {
                hookVelocity.y -= sinkSpeed * Time.deltaTime;
            }
            else
            {
                // Reel in hook
                Vector2 hookToRod = (Vector2)rodTip.position - (Vector2)currentHook.transform.position;
                hookVelocity = hookToRod.normalized * reelSpeed;
            }

            // Update hook position
            Vector2 newPosition = currentHook.transform.position;
            newPosition += hookVelocity * Time.deltaTime;

            // Constrain to max line length
            Vector2 toHook = newPosition - (Vector2)rodTip.position;
            if (toHook.magnitude > maxLineLength)
            {
                newPosition = (Vector2)rodTip.position + toHook.normalized * maxLineLength;
                hookVelocity = Vector2.zero;
            }

            currentHook.transform.position = newPosition;
            currentLineLength = toHook.magnitude;
        }
    }

    private void UpdateLinePositions()
    {
        if (fishingLine == null || currentHook == null) return;
        
        fishingLine.SetPosition(0, rodTip.position);
        fishingLine.SetPosition(1, currentHook.transform.position);
    }
}
