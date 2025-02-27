using UnityEngine;

[CreateAssetMenu(fileName = "FishBehavior", menuName = "Fish/Behavior")]
public class FishBehavior : ScriptableObject
{
    [Header("Fish Properties")]
    public float size = 1f;
    public float baseSpeed = 5f;
    public float maxSpeed = 10f;
    public float turnSpeed = 3f;
    
    [Header("Behavior Weights")]
    public float wanderWeight = 1f;
    public float flockWeight = 1f;
    public float avoidanceWeight = 1f;
    public float baitAttractionWeight = 1f;
    public float predatorAvoidanceWeight = 1f;
    
    [Header("Detection Ranges")]
    public float visionRange = 5f;
    public float baitDetectionRange = 3f;
    public float predatorDetectionRange = 4f;
    public float personalSpace = 1f;
    
    [Header("Predator Settings")]
    [Tooltip("If true, this fish is considered a predator")]
    public bool isPredator = false;
    [Tooltip("Layer mask for potential prey")]
    public LayerMask preyLayer;
    [Tooltip("How strongly this predator pursues prey")]
    public float preyChaseWeight = 1f;
    [Tooltip("Minimum size ratio to be considered prey (e.g. 0.5 means fish half its size or smaller)")]
    public float preyMaxSizeRatio = 0.75f;
    [Tooltip("Visual indicator color for this fish type")]
    public Color fishColor = Color.white;
} 