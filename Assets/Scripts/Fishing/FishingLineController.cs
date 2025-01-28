
using UnityEngine;

public class FishingLineController : MonoBehaviour
{    
    // Add to FishingLine class
    private Color lineColor = Color.black;
    private const float Width = .03f;
    private const float PullConstant = 100f;
    private const float PushConstant = 120f;
    private const float ReelSpeed = 3f;
    
    //private const

    // Length of the rod
    
    public float baseLength { get; private set; }
    public float prevLength { get; private set; }
    private const float ReelDownConstant = .6f;
    private const float Damping = 25f;    
    // ------------------------


    
    private LineRenderer line;

    [SerializeField] private Transform rod;
    //private Vector2 rodPos;
    [SerializeField] private Transform hook;
    

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.startColor = lineColor;
        line.endColor = lineColor;
    }

    private void Start()
    {

        line.positionCount = 2;
        var lineBounds = hook.position - rod.position;
        baseLength = lineBounds.magnitude;
        prevLength = baseLength;
        
        line.startWidth = Width;
    }


    /**
        Creates the line visuals
    */
    private void Update()
    {
        // Line visuals
        line.SetPosition(0, rod.position);
        line.SetPosition(1, hook.position);
    }

    public void AlterLength(float input)
    {
        var dLength = input * ReelSpeed * Time.fixedDeltaTime;
        if (IsOppositeDirection(input, 1f))
        {
            dLength *= ReelDownConstant;
        }
        baseLength -= dLength;
        if (baseLength < 0f)
        {
            baseLength = 0f;
        }
    }

    // Calculates the force vector when simulating tension
    // 
    // Uses Hooke's law
    public Vector2 CalculateHookForce()
    {
        var currVector = hook.position - rod.position;
        var currUnitVector = currVector.normalized;
        // Gets line as a vector pointing towards the fishing line
        var baseVector = currUnitVector * baseLength;
        var dVector = baseVector - currVector;
        
        // Damping force
        var currLength = currVector.magnitude;
        var dMagnitude = currLength - prevLength;
        var dampForce = Damping * dMagnitude * currUnitVector / Time.fixedDeltaTime;

        prevLength = currLength;

        if (IsOppositeDirection(currVector.x, dVector.x))
        {
            return (PullConstant * dVector) - dampForce;
        }
        else
        {
            return (PushConstant * dVector) - dampForce;
        }
    }

    private bool IsOppositeDirection(float x, float y)
    {
        return ((int)x)>>31 != ((int)y)>>31;
    }

}
