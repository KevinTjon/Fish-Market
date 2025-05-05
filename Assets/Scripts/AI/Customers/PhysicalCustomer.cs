using UnityEngine;
using System.Collections;
using UnityEngine.AI;

namespace Market
{
    public class PhysicalCustomer : MonoBehaviour
    {
        [Header("References")]
        private Customer customerData;
        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private NavMeshAgent agent;

        [Header("Movement Settings")]
        [SerializeField] private float stoppingDistance = 1f;
        [SerializeField] private float purchaseWaitTime = 2f;
        
        private Transform spawnPoint;
        private bool isMoving = false;
        private bool isDone = false;
        private Vector2 targetPosition;

        // Animation parameter names
        private readonly string isWalkingParam = "IsWalking";
        private readonly string horizontalParam = "Horizontal";
        private readonly string verticalParam = "Vertical";

        private void Awake()
        {
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            agent = GetComponent<NavMeshAgent>();
            
            if (agent != null)
            {
                // Configure NavMeshAgent for 2D
                agent.updateRotation = false;
                agent.updateUpAxis = false;
            }
            else
            {
                Debug.LogError("NavMeshAgent component missing on customer!");
            }
        }

        public void Initialize(Customer customer, Transform spawn)
        {
            customerData = customer;
            spawnPoint = spawn;
            transform.position = spawnPoint.position;
            
            if (agent != null)
            {
                agent.stoppingDistance = stoppingDistance;
            }
        }

        public IEnumerator MoveToSeller(Transform sellerPosition)
        {
            if (agent == null)
            {
                Debug.LogError("NavMeshAgent is null, cannot move customer");
                yield break;
            }

            Debug.Log($"Starting movement to seller at position {sellerPosition.position}");
            isMoving = true;
            targetPosition = sellerPosition.position;
            SetWalkingAnimation(true);

            agent.SetDestination(targetPosition);
            Debug.Log($"Set destination to {targetPosition}, current agent position: {agent.transform.position}");

            while (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                Debug.LogWarning($"Invalid path to seller at {targetPosition}, waiting for valid path...");
                yield return new WaitForSeconds(0.5f);
                agent.SetDestination(targetPosition);
            }

            Debug.Log($"Path status: {agent.pathStatus}, remaining distance: {agent.remainingDistance}");
            while (agent.pathStatus != NavMeshPathStatus.PathComplete || agent.remainingDistance > stoppingDistance)
            {
                if (agent.velocity != Vector3.zero)
                {
                    Vector2 movement = agent.velocity.normalized;
                    UpdateAnimation(movement);
                    Debug.Log($"Moving with velocity {agent.velocity}, remaining distance: {agent.remainingDistance}");
                }
                yield return null;
            }

            Debug.Log("Reached seller position");
            isMoving = false;
            SetWalkingAnimation(false);
            yield return new WaitForSeconds(purchaseWaitTime);
        }

        public IEnumerator ReturnToSpawn()
        {
            if (isDone || agent == null) yield break;

            isMoving = true;
            targetPosition = spawnPoint.position;
            SetWalkingAnimation(true);

            agent.SetDestination(targetPosition);

            while (agent.pathStatus != NavMeshPathStatus.PathComplete || agent.remainingDistance > stoppingDistance)
            {
                if (agent.velocity != Vector3.zero)
                {
                    Vector2 movement = agent.velocity.normalized;
                    UpdateAnimation(movement);
                }
                yield return null;
            }

            isMoving = false;
            isDone = true;
            SetWalkingAnimation(false);

            // Destroy the physical representation
            Destroy(gameObject);
        }

        private void UpdateAnimation(Vector2 movement)
        {
            if (animator != null)
            {
                // Update movement direction
                animator.SetFloat(horizontalParam, movement.x);
                animator.SetFloat(verticalParam, movement.y);

                // Update sprite direction
                if (spriteRenderer != null && Mathf.Abs(movement.x) > 0.1f)
                {
                    spriteRenderer.flipX = movement.x < 0;
                }
            }
        }

        private void SetWalkingAnimation(bool isWalking)
        {
            if (animator != null)
            {
                try
                {
                    animator.SetBool(isWalkingParam, isWalking);
                }
                catch (System.Exception)
                {
                    // Parameter doesn't exist, silently fail
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (isMoving && agent != null && agent.hasPath)
            {
                // Draw the NavMesh path
                Gizmos.color = Color.yellow;
                var path = agent.path;
                Vector3 previousCorner = transform.position;
                foreach (var corner in path.corners)
                {
                    Gizmos.DrawLine(previousCorner, corner);
                    previousCorner = corner;
                }
            }
        }

        public void OnDestroy()
        {
            // Clean up any resources if needed
        }
    }
} 