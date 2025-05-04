using UnityEngine;

public class RodController : MonoBehaviour
{
    [Header("Line Settings")]
    [SerializeField] private readonly Color lineColor = Color.black;
    [SerializeField] private readonly float lineWidth = 0.05f;
    [SerializeField] private readonly float maxLineLength = 100f;
    [SerializeField] private readonly float pullConstant = 300f;
    [SerializeField] private readonly float pushConstant = 350f;
    [SerializeField] private readonly float damping = 25f;
    [SerializeField] private readonly float reelSpeed = 5f;
    [SerializeField] private readonly float sinkSpeed = 3f;
    [SerializeField] private readonly float turnTriggerLength = 1.5f;
    private Material lineMaterial;

    [Header("Hook Surface Settings")]
    [SerializeField] private readonly float detachForce = 2f;
    [SerializeField] private readonly float detachLengthChange = 0.5f;

    [Header("Charge Settings")]
    [SerializeField] private readonly float castPowerMultiplier = 10f;
    [SerializeField] private readonly float maxChargeTime = 5f;
    [SerializeField] private readonly Vector2 castAngle = new Vector2(0.5f, 0.5f);
    private float chargeTime;

    [Header("References")]
    //[SerializeField] private Transform player;
    [SerializeField] private Transform rodConnection;
    [SerializeField] private GameObject hookPrefab; // Reference to hook object
    
    public FishingLineController line { get; private set; }
    public HookController hook { get; private set; }
    private Cooler fishCooler;

    public enum RodState
    {
        Idle,
        Charging,
        Casting,
        Fishing
    }

    public RodState rodState { get; private set; }

    public bool IsFishing => rodState == RodState.Fishing || rodState == RodState.Casting; // Public get

    private void Awake()
    {
        // Initialize line and hook
        if (line != null)
        {
            Destroy(line);
        }
        line = new GameObject("FishingLine").AddComponent<FishingLineController>();
        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        line.transform.SetParent(transform);


        if (hook != null)
        {
            Destroy(hook);
        }
        hook = Instantiate(hookPrefab, rodConnection.position, Quaternion.identity).GetComponent<HookController>();
        hook.transform.SetParent(transform);

        fishCooler = GameObject.FindWithTag("Cooler").GetComponent<Cooler>();
    }

    /// <summary>
    /// Instantiates the fishing line and hook and sets up the flags
    /// </summary>
    private void Start()
    {
        // Initialize hook first
        hook.InitializeHook(rodConnection);

        LineSettings settings = new LineSettings
        {
            lineColor = lineColor,
            lineWidth = lineWidth,
            lineMaterial = lineMaterial,
            maxLineLength = maxLineLength,
            pullConstant = pullConstant,
            pushConstant = pushConstant,
            damping = damping,
            reelSpeed = reelSpeed,
            sinkSpeed = sinkSpeed,
            turnTriggerLength = turnTriggerLength
        };

        line.InitializeLine(rodConnection, hook, settings);
        rodState = RodState.Idle;

        chargeTime = 0f;
    }

    /// <summary>
    /// Handles charge logic
    /// </summary>
    private void Update()
    {
        if(rodState == RodState.Charging)
        {
            chargeTime += Time.deltaTime;
            Camera.main.ScreenToWorldPoint(rodConnection.position);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="charge"></param>
    public void HandleChargeInput(bool charge)
    {
        switch (rodState)
        {
            case RodState.Idle:
                if (charge)
                {
                    rodState = RodState.Charging;
                }
                break;
            case RodState.Charging:
                if (!charge)
                {
                    //Vector2 castEndPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    //Vector2 castDirection = (castStartPosition - castEndPosition).normalized;
                    float chargePower = Mathf.Clamp(chargeTime, 0, maxChargeTime);

                    var hookVelocity = chargeTime * chargePower * castPowerMultiplier * castAngle;
                    line.StartFishing();
                    hook.StartFishing(hookVelocity);
                    
                    chargeTime = 0f;

                    rodState = RodState.Casting;
                }
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Self-contained logic to handle reel input
    /// Alters length, attaches hook to water surface, and adds items to the cooler
    /// </summary>
    /// <param name="input">Player input</param>
    public void ReceiveReelInput(float input)
    {
        // Apply force to hook
        var tensionForce = line.CalculateHookForce(rodState == RodState.Fishing);
        hook.AddForce(tensionForce);
        
        if (hook.OnWaterSurface) 
        {
            Debug.Log("Hook is on the water surface");
            //Debug.Log(line.PrintLength());

            if (input > 0)
            {
                
                if (hook.attachedObject != null)
                {
                    Debug.Log("Hook is attached to an object");
                    var fish = hook.attachedObject.GetComponent<BasicFish>();
                    Debug.Log("Fish: " + fish);
                    if (fish != null)
                    {
                        // Debug
                        if (fishCooler.AddFish(fish))
                        {
                            fishCooler.DisplayCooler();
                            Debug.Log(fish.name + " caught!");
                            hook.CatchObject();
                        }
                        else
                        {
                            Debug.Log("Item cannot be added to cooler, it is full");
                        }
                    }
                }
                else
                {
                    Debug.Log("Hook is not attached to an object");
                }
            }
            else if (input < 0)
            {
                hook.DetachHookFromSurface(detachForce);
                line.DetachHookFromSurface(detachLengthChange);
            }
        }
        else
        {
            line.AlterLength(input);
            var hookY = hook.hookRB.position.y;
            switch (rodState)
            {
                case RodState.Casting:
                    if (hookY < hook.WaterLevel)
                    {
                        line.SetLengthOnWaterSurface();
                        hook.AttachHookToSurface();
                        rodState = RodState.Fishing;
                    }
                    break;
                case RodState.Fishing:
                    if (hookY > hook.WaterLevel)
                    {
                        line.SetLengthOnWaterSurface();
                        hook.AttachHookToSurface();
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="waterLevel"></param>
    public void SetWaterLevel(float waterLevel)
    {
        hook.SetWaterLevel(waterLevel);
    }
}