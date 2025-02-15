using UnityEngine;

public class FishingLineController : MonoBehaviour
{    
    // Add to FishingLine class
    private Color _lineColor = Color.black;
    private const float Width = .03f;
    private const float PullConstant = 100f;
    private const float PushConstant = 120f;
    private const float ReelSpeed = 3f;
    private const float ReelDownConstant = .6f;
    private const float Damping = 25f;
    // --------------------------------------------
    
    // Length of the rod
    public Vector2 currVector { get; private set; }
    public float baseLength { get; private set; }
    public float currLength { get; private set; }
    
    // --------------------------------------------

    // Replace with manual calculation based on player position and boat size
    private const float TurnTriggerLength = 1.75f;
    // --------------------------------------------


    
    private LineRenderer line;

    [SerializeField] private Transform rod;
    //private Vector2 rodPos;
    [SerializeField] private Transform hook;
    

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.startColor = _lineColor;
        line.endColor = _lineColor;
    }

    private void Start()
    {
        line.positionCount = 2;
        var lineBounds = hook.position - rod.position;
        currLength = baseLength = lineBounds.magnitude;
        
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
        var newVector = hook.position - rod.position;
        //currVector = hook.position - rod.position;
        var newNormalVector = newVector.normalized;
        var newBaseVector = newNormalVector * baseLength;
        var newLength = newVector.magnitude;

        var dVector = newBaseVector - newVector;        
        var dMagnitude = newLength - currLength;
        var dampForce = Damping * dMagnitude * newNormalVector / Time.fixedDeltaTime;

        currLength = newLength;
        currVector = newVector;

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

    public bool DoesTriggerTurn(bool isFacingRight)
    {
        // Get horizontal component of currVector
        var currX = isFacingRight ? -currVector.x : currVector.x;
        if (currX > TurnTriggerLength)
        {
            Debug.Log("Change direction");
            return true;
        }
        return false;
    }

    public void ResetLength()
    {
        baseLength = currLength;
    } 
    /*
    public void MoveRod(Vector2 newPos)
    {
        rod.position = newPos;
    }
    */

}
