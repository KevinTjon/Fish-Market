using UnityEngine;

public class FishingLineController : MonoBehaviour
{    
    // Add to FishingLine class
    private Color lineColor = Color.black;
    private const float Width = .1f;
    private const float PullConstant = 40f;
    private const float PushConstant = 50f;
    private const float ReelSpeed = 2f;
    

    // Length of the rod
    private float baseLength;
    private const float ReelDownConstant = .6f;
    // ------------------------


    
    private LineRenderer line;

    [SerializeField] private Transform rod;
    //private Vector2 rodPos;
    [SerializeField] private Transform hook;
    

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        line.positionCount = 2;

        var lineBounds = hook.position - rod.position;
        var normal = lineBounds.normalized;
        baseLength = lineBounds.x / normal.x;
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
        var deltaLength = input * ReelSpeed * Time.fixedDeltaTime;
        if (IsOppositeDirection(input, 1f))
        {
            deltaLength *= ReelDownConstant;
        }
        baseLength -= deltaLength;
        if (baseLength < 0f)
        {
            baseLength = 0f;
        }
    }
    // Calculates the force vector when simulating tension
    // 
    // Uses Hooke's law
    public Vector2 CalculateTension()
    {
        var currVector = hook.position - rod.position;
        // Gets line as a vector pointing towards the fishing line
        var baseVector = currVector.normalized * baseLength;
        var displacement = baseVector - currVector;

        if (IsOppositeDirection(currVector.x, displacement.x))
        {
            return PullConstant * displacement;
        }
        else
        {
            return PushConstant * displacement;
        }
    }

    private bool IsOppositeDirection(float x, float y)
    {
        return ((int)x)>>31 != ((int)y)>>31;
    }

    // function to draw current length to screen

}
