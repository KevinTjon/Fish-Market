using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class SimpleHookController : MonoBehaviour
{
    [SerializeField] private float hookRadius = 0.2f;
    
    private CircleCollider2D hookCollider;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        // Setup collider
        hookCollider = GetComponent<CircleCollider2D>();
        hookCollider.radius = hookRadius;
        hookCollider.isTrigger = true;

        // Setup sprite
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // For now, just debug log what we hit
        Debug.Log($"Hook hit: {other.gameObject.name}");
    }
} 