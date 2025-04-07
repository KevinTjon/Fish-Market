using UnityEngine;

/// <summary>
/// This struct contains the settings for the fishing line.
///     It includes properties such as line color, width, material, and various
///     constants for line behavior.
/// </summary>
public struct LineSettings
{
    public Color lineColor;
    public float lineWidth;
    public Material lineMaterial;
    public float maxLineLength;
    public float pullConstant;
    public float pushConstant;
    public float damping;
    public float reelSpeed;
    public float sinkSpeed;
    public float turnTriggerLength;
}

public class FishingLineController : MonoBehaviour
{
    // Fishing line settings
    private float maxLineLength;
    private float pullConstant;
    private float pushConstant;
    private float damping;
    private float reelSpeed;
    private float sinkSpeed;
    private float turnTriggerLength;
    private float lengthOnWater;
    //private LineSettings lineSettings;

    // Current line state
    public Vector2 currVector { get; private set; }
    public float baseLength { get; private set; }
    public float currLength { get; private set; }


    // References to other Components/GameObjects
    private LineRenderer lineRenderer;
    private Transform rod;
    private Transform hook;
    
    public void InitializeLine(Transform rod, Transform hook, LineSettings lineSettings)
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        this.rod = rod; this.hook = hook;
        // Instantiate the line renderer
        lineRenderer.positionCount = 2;
        lineRenderer.startColor = lineSettings.lineColor;
        lineRenderer.endColor = lineSettings.lineColor;
        lineRenderer.startWidth = lineSettings.lineWidth;
        lineRenderer.endWidth = lineSettings.lineWidth;
        lineRenderer.material = lineSettings.lineMaterial;
        
        // Map line settings to variables
        maxLineLength = lineSettings.maxLineLength;
        pullConstant = lineSettings.pullConstant;
        pushConstant = lineSettings.pushConstant;
        damping = lineSettings.damping;
        reelSpeed = lineSettings.reelSpeed;
        sinkSpeed = lineSettings.sinkSpeed;
        turnTriggerLength = lineSettings.turnTriggerLength;
        
        // Initialize the line length as zero
        currLength = baseLength = 0;

        // Calculate onWaterSurface height
        lengthOnWater = rod.position.y - hook.position.y;
    }

    /**
        Creates the line visuals
    */
    private void Update()
    {
        if (hook != null)
        {
            lineRenderer.SetPosition(0, rod.position);
            lineRenderer.SetPosition(1, hook.position);
        }
    }

    // Starts fishing
    public void StartFishing()
    {
        // Set the line length to the distance between the rod and hook
        baseLength = maxLineLength/2f;
    }

    public void SetLength(float length)
    {
        baseLength = length;
    }
    
    public void AlterLength(float input)
    {
        float dLength;
        if (IsOppositeDirection(input, 1f))
        {
            dLength = input * sinkSpeed * Time.fixedDeltaTime;
        }
        else
        {
            dLength = input * reelSpeed * Time.fixedDeltaTime;
        }

        SetLength(baseLength-dLength);
        if (baseLength < 0f)
        {
            SetLength(0f);
        }
        if (baseLength > maxLineLength)
        {
            SetLength(maxLineLength);
        }
    }

    // Calculates the force vector when simulating tension
    // 
    // Uses Hooke's law
    private void UpdateLine(Vector2 newVector, float newLength)
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
        var dampForce = damping * dMagnitude * newNormalVector / Time.fixedDeltaTime;
        UpdateLine(newVector, newLength);

        return IsOppositeDirection(currVector.x, dVector.x)
            ? (pullConstant * dVector) - dampForce
            : (pushConstant * dVector) - dampForce;
    }

    /// <summary>
    /// Compares two float values to check if they are in opposite directions.
    /// </summary>
    /// <param name="x">The first float</param>
    /// <param name="y">The second float we're comparing</param>
    /// <returns>True if one float is negative and the other is positive</returns>
    private bool IsOppositeDirection(float x, float y)
    {
        return ((int)x)>>31 != ((int)y)>>31;
    }

    public bool DoesTriggerTurn(bool isFacingRight)
    {
        // Get horizontal component of currVector
        var currX = isFacingRight ? -currVector.x : currVector.x;
        if (currX > turnTriggerLength)
        {
            //Debug.Log("Change direction");
            return true;
        }
        return false;
    }

    public void ResetLength()
    {
        baseLength = currLength;
    }
    public void SetLengthOnWaterSurface()
    {
        // Set the line length to the distance between the rod and hook
        SetLength(lengthOnWater);
    }
}
