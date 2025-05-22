using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleRodController : MonoBehaviour
{
    [Header("Rod Settings")]
    [SerializeField] private float maxLineLength = 10f;
    [SerializeField] private float castPowerMultiplier = 10f;
    [SerializeField] private float reelSpeed = 5f;
    [SerializeField] private float sinkSpeed = 3f;
    [SerializeField] private float maxChargeTime = 5f;
    [SerializeField] private float castAngle = 45f;

    [Header("References")]
    [SerializeField] private Transform rodTip;
    [SerializeField] private GameObject hookPrefab;
    
    private LineRenderer fishingLine;
    private GameObject currentHook;
    private Vector2 hookVelocity;
    private float chargeStartTime;
    private bool isReeling;
    private bool isCharging;
    private bool isCasting;

    // Public property to access the current hook
    public GameObject CurrentHook => currentHook;

    private enum RodState
    {
        Ready,
        Charging,
        Fishing
    }

    private RodState currentState = RodState.Ready;

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
        if (currentHook != null) Destroy(currentHook);
        currentHook = Instantiate(hookPrefab, rodTip.position, Quaternion.identity);
        currentHook.transform.SetParent(transform);
        ResetHookPosition();
        UpdateLinePositions();
    }

    private void ResetHookPosition()
    {
        if (currentHook != null)
        {
            currentHook.transform.position = rodTip.position;
            hookVelocity = Vector2.zero;
            currentState = RodState.Ready;
            isCharging = false;
            isReeling = false;
            isCasting = false;
        }
    }

    private void Update()
    {
        HandleInput();
        UpdateHookMovement();
        UpdateLinePositions();
    }

    private void HandleInput()
    {
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;

        if (mouse == null || keyboard == null) return;

        switch (currentState)
        {
            case RodState.Ready:
                if (mouse.rightButton.wasPressedThisFrame)
                {
                    currentState = RodState.Charging;
                    chargeStartTime = Time.time;
                    isCharging = true;
                }
                break;

            case RodState.Charging:
                if (isCharging)
                {
                    float currentChargeTime = Time.time - chargeStartTime;
                    
                    // Start casting when released or at max charge
                    if (currentChargeTime >= maxChargeTime || mouse.rightButton.wasReleasedThisFrame)
                    {
                        float powerRatio = Mathf.Min(currentChargeTime / maxChargeTime, 1f);
                        StartCast(powerRatio);
                        currentState = RodState.Fishing;
                        isCharging = false;
                    }
                }
                break;

            case RodState.Fishing:
                isReeling = keyboard.spaceKey.isPressed;
                
                if (keyboard.rKey.wasPressedThisFrame)
                {
                    ResetHookPosition();
                }
                break;
        }
    }

    private void StartCast(float powerRatio)
    {
        Vector2 castDirection = Quaternion.Euler(0, 0, castAngle) * Vector2.right;
        hookVelocity = castDirection * (powerRatio * castPowerMultiplier);
        isCasting = true;
    }

    private void UpdateHookMovement()
    {
        if (currentHook == null) return;

        if (currentState == RodState.Fishing)
        {
            Vector2 newPosition = currentHook.transform.position;

            if (isReeling)
            {
                // Move directly towards rod tip when reeling
                Vector2 toRodTip = (Vector2)rodTip.position - (Vector2)currentHook.transform.position;
                if (toRodTip.magnitude < 0.1f)
                {
                    ResetHookPosition();
                    return;
                }
                newPosition = Vector2.MoveTowards(newPosition, rodTip.position, reelSpeed * Time.deltaTime);
            }
            else if (isCasting)
            {
                // During initial cast arc
                hookVelocity.y -= sinkSpeed * Time.deltaTime;
                newPosition += hookVelocity * Time.deltaTime;

                // Check if we've reached max distance
                Vector2 toHook = newPosition - (Vector2)rodTip.position;
                if (toHook.magnitude > maxLineLength)
                {
                    // Stop casting and start falling
                    isCasting = false;
                    hookVelocity = Vector2.zero;
                    newPosition = (Vector2)rodTip.position + (toHook.normalized * maxLineLength);
                }
            }
            else
            {
                // Just fall straight down when not casting or reeling
                newPosition.y -= sinkSpeed * Time.deltaTime;
                
                // Maintain max line length
                Vector2 toHook = newPosition - (Vector2)rodTip.position;
                if (toHook.magnitude > maxLineLength)
                {
                    float xDist = Mathf.Abs(newPosition.x - rodTip.position.x);
                    if (xDist > maxLineLength)
                    {
                        // Clamp X position if too far
                        float direction = Mathf.Sign(newPosition.x - rodTip.position.x);
                        newPosition.x = rodTip.position.x + (direction * maxLineLength);
                        newPosition.y = rodTip.position.y;
                    }
                    else
                    {
                        // Calculate maximum Y position at current X position
                        float maxY = rodTip.position.y - Mathf.Sqrt(maxLineLength * maxLineLength - xDist * xDist);
                        newPosition.y = Mathf.Max(newPosition.y, maxY);
                    }
                }
            }

            currentHook.transform.position = newPosition;
        }
        else if (currentState == RodState.Ready || currentState == RodState.Charging)
        {
            currentHook.transform.position = rodTip.position;
        }
    }

    private void UpdateLinePositions()
    {
        if (fishingLine == null || currentHook == null) return;
        fishingLine.SetPosition(0, rodTip.position);
        fishingLine.SetPosition(1, currentHook.transform.position);
    }

    private void OnGUI()
    {
        if (currentState == RodState.Charging && isCharging)
        {
            float currentChargeTime = Time.time - chargeStartTime;
            float powerRatio = Mathf.Min(currentChargeTime / maxChargeTime, 1f);
            GUI.Label(new Rect(10, 10, 200, 20), $"Power: {(powerRatio * 100):F0}%");
        }
    }

    private void OnDrawGizmos()
    {
        // Draw cast angle indicator when charging
        if (currentState == RodState.Charging)
        {
            Gizmos.color = Color.yellow;
            Vector2 direction = Quaternion.Euler(0, 0, castAngle) * Vector2.right;
            Gizmos.DrawLine(rodTip.position, (Vector2)rodTip.position + direction * (chargeStartTime/maxChargeTime * 2f));
        }
    }
} 