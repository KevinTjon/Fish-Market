using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class FishingLineController : MonoBehaviour
{    
    // Add to FishingLine class
    private Color lineColor = Color.black;
    private float width;
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
    private const float TurnTriggerLength = 1.5f;
    // --------------------------------------------


    
    private LineRenderer lineRenderer;
    private Transform rod;
    private Transform hook;
    
    public void InstantiateLine(Transform rod, Transform hook, float width)
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.startWidth = width;
        lineRenderer.positionCount = 2;
        
        this.rod = rod; this.hook = hook;

        
        var lineBounds = hook.position - rod.position;
        currLength = baseLength = lineBounds.magnitude;
        
    }

    /**
        Creates the line visuals
    */
    private void Update()
    {
        // Line visuals
        lineRenderer.SetPosition(0, rod.position);
        lineRenderer.SetPosition(1, hook.position);
    }

    private void FixedUpdate()
    {
        
    }

    public void SetLength(float length)
    {
        baseLength = length;
        //currLength = length;
    }
    
    public void AlterLength(float input)
    {
        var dLength = input * ReelSpeed * Time.fixedDeltaTime;
        if (IsOppositeDirection(input, 1f))
        {
            dLength *= ReelDownConstant;
        }
        SetLength(baseLength-dLength);
        if (baseLength < 0f)
        {
            SetLength(0f);
        }
    }



    // Calculates the force vector when simulating tension
    // 
    // Uses Hooke's law
    private void UpdateLine(Vector2 newVector,float newLength)
    {
        currLength = newLength;
        currVector = newVector;
    }
    public Vector2 CalculateHookForce(bool hasTension)
    {
        var newVector = hook.position - rod.position;
        var newLength = newVector.magnitude;

        if (!hasTension)
        {
            UpdateLine(newVector, newLength);
            return Vector2.zero;
        }

        var newNormalVector = newVector.normalized;
        var newBaseVector = newNormalVector * baseLength;
        var dVector = newBaseVector - newVector;        
        var dMagnitude = newLength - currLength;
        var dampForce = Damping * dMagnitude * newNormalVector / Time.fixedDeltaTime;
        UpdateLine(newVector, newLength);

        return IsOppositeDirection(currVector.x, dVector.x)
            ? (PullConstant * dVector) - dampForce
            : (PushConstant * dVector) - dampForce;
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
}
