using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Bait : MonoBehaviour
{
    public BaitData baitData;
    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider;
    private Rigidbody2D rb;

    private void Awake()
    {
        // Setup sprite renderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Create circle sprite if not already assigned
        if (spriteRenderer.sprite == null)
        {
            // Create a circle sprite
            spriteRenderer.sprite = CreateCircleSprite();
            transform.localScale = new Vector3(0.2f, 0.2f, 1f); // Adjust size as needed
        }

        // Set the color from bait data
        if (baitData != null)
        {
            spriteRenderer.color = baitData.baitColor;
        }

        // Setup collider
        circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.isTrigger = true; // Make it a trigger so it doesn't cause physical collisions
        circleCollider.radius = 0.5f; // Match the sprite size

        // Setup rigidbody
        rb = GetComponent<Rigidbody2D>();
        rb.isKinematic = true; // Make it kinematic so it follows the hook perfectly
        rb.gravityScale = 0;
        
        // Set layer to a special bait layer (you'll need to create this in Unity)
        gameObject.layer = LayerMask.NameToLayer("Bait");
    }

    private Sprite CreateCircleSprite()
    {
        // Create a white circle texture
        int size = 32; // Size of the texture
        Texture2D texture = new Texture2D(size, size);
        
        // Calculate center and radius
        Vector2 center = new Vector2(size / 2, size / 2);
        float radius = size / 2;
        
        // Fill the texture with a circle
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    texture.SetPixel(x, y, Color.white);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        
        // Create sprite from texture
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    public bool IsCompatibleWithFish(FishSize fishSize)
    {
        return baitData != null && baitData.targetFishSize == fishSize;
    }
} 