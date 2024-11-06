using UnityEngine;

public class FishingLineController : MonoBehaviour
{    
    // Add to FishingLine class
    private Color lineColor = Color.black;
    private const float width = .1f;
    private const float pullConstant = 10f;
    private const float pushConstant = 15f;

    // Length of the rod
    private float baseLength;
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

    // Assigns the points of the line to draw
    /*
    public void CreateLine(Transform rodPoint, Transform hookPoint)
    {
        line.positionCount = 2;
        rodPosition = rodPoint.position;
        hookPosition = hookPoint.position;
    }
    */

    private void Update()
    {
        line.SetPosition(0, rod.position);
        line.SetPosition(1, hook.position);
    }

    // Calculates the force vector when simulating tension
    // 
    // Uses Hooke's law
    public Vector2 CalculateTension()
    {
        var hookPos = hook.position;
        var rodPos = rod.position;
        
        var currVector = hookPos - rodPos;
        var displacement = CalculateLineDisplacement(currVector);

        if (IsOppositeDirection(currVector.x, displacement.x))
            return pullConstant * displacement;
        else
            return pushConstant * displacement;
    }
    
    // Calculate the difference between the current position of the 
    // line and the unstretched line (line in the same direction, but
    // has length "baseLength").
    //
    // The vector will face towards the rod connection point
    public Vector2 CalculateLineDisplacement(Vector2 currVector)
    {
        var direction = currVector.normalized;
        var baseVector = direction * baseLength;
        return baseVector - currVector;
    }

    private bool IsOppositeDirection(float x, float y)
    {
        return ((int)x)>>31 != ((int)y)>>31;
    }

    // function to draw current length to screen

}
