using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishingLine
{
    // Add to FishingLine class
    private Color lineColor = Color.black;
    private const float width = .1f;
    private const float pullConstant = 10f;
    private const float pushConstant = 15f;

    // Length of the rod
    private float baseLength;
    // ------------------------

    public FishingLine()
    {
        baseLength = 10f;
    }

    public FishingLine(float baseLength)
    {
        this.baseLength = baseLength;
    }

}
