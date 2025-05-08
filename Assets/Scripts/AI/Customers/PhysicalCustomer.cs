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
        private NavMeshAgent agent;

        [Header("Movement Settings")]
        [SerializeField] private float stoppingDistance = 1f;
        [SerializeField] private float purchaseWaitTime = 2f;
        [SerializeField] private float movementThreshold = 0.01f;
        
        private Transform spawnPoint;
        private bool isMoving = false;
        private bool isDone = false;
        private Vector2 targetPosition;

        // Animation parameter names
        private readonly string isWalkingParam = "IsWalking";
        private readonly string horizontalParam = "DirectionX";
        private readonly string verticalParam = "DirectionY";

        private float baseAgentSpeed = 0f;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            agent = GetComponent<NavMeshAgent>();
            
            if (agent != null)
            {
                // Configure NavMeshAgent for 2D
                agent.updateRotation = false;
                agent.updateUpAxis = false;
                baseAgentSpeed = agent.speed;
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

            // Initialize animator parameters
            if (animator != null)
            {
                Debug.Log($"[Customer {customer?.CustomerID}] Initializing animator parameters");
                animator.SetBool(isWalkingParam, false);
                animator.SetFloat(horizontalParam, 0);
                animator.SetFloat(verticalParam, 0);
                
                // Debug.Log($"[Customer {customer?.CustomerID}] Initial animator values - IsWalking: {animator.GetBool(isWalkingParam)}, " +
                //          $"DirectionX: {animator.GetFloat(horizontalParam)}, " +
                //          $"DirectionY: {animator.GetFloat(verticalParam)}");
            }
            else
            {
                Debug.LogError("Animator component is missing on customer!");
            }
        }

        private void Start()
        {
            // Double check animator setup
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                Debug.LogWarning("Animator was null in Start, attempting to get component");
            }

            if (animator != null)
            {
                // Verify animator has controller
                if (animator.runtimeAnimatorController == null)
                {
                    Debug.LogError("Animator has no controller assigned!");
                }
                else
                {
                    //Debug.Log($"Animator controller: {animator.runtimeAnimatorController.name}");
                }
            }
        }

        public IEnumerator MoveToSeller(Transform sellerPosition)
        {
            if (agent == null)
            {
                Debug.LogError("NavMeshAgent is null, cannot move customer");
                yield break;
            }

            //Debug.Log($"Starting movement to seller at position {sellerPosition.position}");
            isMoving = true;
            targetPosition = sellerPosition.position;
            SetWalkingAnimation(true);

            // First move horizontally, then vertically
            Vector3 horizontalTarget = new Vector3(targetPosition.x, transform.position.y, transform.position.z);
            Vector3 finalTarget = targetPosition;

            // Move horizontally first
            //Debug.Log($"Starting horizontal movement to {horizontalTarget}");
            agent.SetDestination(horizontalTarget);
            
            Vector3 lastPosition = transform.position;
            float elapsedTime = 0f;
            
            while (Vector3.Distance(transform.position, horizontalTarget) > stoppingDistance)
            {
                Vector3 currentPosition = transform.position;
                Vector3 movement = (currentPosition - lastPosition) / Time.deltaTime;
                
                //Debug.Log($"Current position: {currentPosition}, Movement: {movement}, Velocity: {agent.velocity}");
                
                if (movement.magnitude > movementThreshold)
                {
                    Vector2 movement2D = new Vector2(movement.x, movement.y).normalized;
                    //Debug.Log($"Horizontal movement detected - Movement: {movement2D}, Magnitude: {movement2D.magnitude}");
                    UpdateAnimation(movement2D);
                }
                
                lastPosition = currentPosition;
                elapsedTime += Time.deltaTime;
                
                // Timeout after 5 seconds to prevent infinite loops
                if (elapsedTime > 5f)
                {
                    Debug.LogWarning("Horizontal movement timed out");
                    break;
                }
                
                yield return null;
            }

            // Then move vertically
            //Debug.Log($"Starting vertical movement to {finalTarget}");
            agent.SetDestination(finalTarget);
            
            lastPosition = transform.position;
            elapsedTime = 0f;
            
            while (Vector3.Distance(transform.position, finalTarget) > stoppingDistance)
            {
                Vector3 currentPosition = transform.position;
                Vector3 movement = (currentPosition - lastPosition) / Time.deltaTime;
                
                //Debug.Log($"Current position: {currentPosition}, Movement: {movement}, Velocity: {agent.velocity}");
                
                if (movement.magnitude > movementThreshold)
                {
                    Vector2 movement2D = new Vector2(movement.x, movement.y).normalized;
                    //Debug.Log($"Vertical movement detected - Movement: {movement2D}, Magnitude: {movement2D.magnitude}");
                    UpdateAnimation(movement2D);
                }
                
                lastPosition = currentPosition;
                elapsedTime += Time.deltaTime;
                
                // Timeout after 5 seconds to prevent infinite loops
                if (elapsedTime > 5f)
                {
                    Debug.LogWarning("Vertical movement timed out");
                    break;
                }
                
                yield return null;
            }

            //Debug.Log("Reached seller position");
            isMoving = false;
            SetWalkingAnimation(false);
            yield return new WaitForSeconds(purchaseWaitTime);
        }

        public IEnumerator ReturnToSpawn()
        {
            if (isDone || agent == null) yield break;

            //Debug.Log($"Starting return movement to spawn at {spawnPoint.position}");
            isMoving = true;
            targetPosition = spawnPoint.position;
            SetWalkingAnimation(true);

            // First move horizontally, then vertically
            Vector3 horizontalTarget = new Vector3(targetPosition.x, transform.position.y, transform.position.z);
            Vector3 finalTarget = targetPosition;

            // Move horizontally first
            //Debug.Log($"Starting return horizontal movement to {horizontalTarget}");
            agent.SetDestination(horizontalTarget);
            
            Vector3 lastPosition = transform.position;
            float elapsedTime = 0f;
            
            while (Vector3.Distance(transform.position, horizontalTarget) > stoppingDistance)
            {
                Vector3 currentPosition = transform.position;
                Vector3 movement = (currentPosition - lastPosition) / Time.deltaTime;
                
                //Debug.Log($"Return movement - Current position: {currentPosition}, Movement: {movement}, Velocity: {agent.velocity}");
                
                if (movement.magnitude > movementThreshold)
                {
                    Vector2 movement2D = new Vector2(movement.x, movement.y).normalized;
                    //Debug.Log($"Return horizontal movement detected - Movement: {movement2D}, Magnitude: {movement2D.magnitude}");
                    UpdateAnimation(movement2D);
                }
                
                lastPosition = currentPosition;
                elapsedTime += Time.deltaTime;
                
                // Timeout after 5 seconds to prevent infinite loops
                if (elapsedTime > 5f)
                {
                    Debug.LogWarning("Return horizontal movement timed out");
                    break;
                }
                
                yield return null;
            }

            // Then move vertically
            //Debug.Log($"Starting return vertical movement to {finalTarget}");
            agent.SetDestination(finalTarget);
            
            lastPosition = transform.position;
            elapsedTime = 0f;
            
            while (Vector3.Distance(transform.position, finalTarget) > stoppingDistance)
            {
                Vector3 currentPosition = transform.position;
                Vector3 movement = (currentPosition - lastPosition) / Time.deltaTime;
                
                //Debug.Log($"Return movement - Current position: {currentPosition}, Movement: {movement}, Velocity: {agent.velocity}");
                
                if (movement.magnitude > movementThreshold)
                {
                    Vector2 movement2D = new Vector2(movement.x, movement.y).normalized;
                    //Debug.Log($"Return vertical movement detected - Movement: {movement2D}, Magnitude: {movement2D.magnitude}");
                    UpdateAnimation(movement2D);
                }
                
                lastPosition = currentPosition;
                elapsedTime += Time.deltaTime;
                
                // Timeout after 5 seconds to prevent infinite loops
                if (elapsedTime > 5f)
                {
                    Debug.LogWarning("Return vertical movement timed out");
                    break;
                }
                
                yield return null;
            }

            // Wait a moment at spawn point before destroying
            //Debug.Log("Reached spawn point, waiting before despawn...");
            isMoving = false;
            isDone = true;
            SetWalkingAnimation(false);
            yield return new WaitForSeconds(1f);

            //Debug.Log("Customer despawning");
            // Destroy the physical representation
            Destroy(gameObject);
        }

        private void UpdateAnimation(Vector2 movement)
        {
            if (animator != null)
            {
                // Calculate the absolute values for comparison
                float absX = Mathf.Abs(movement.x);
                float absY = Mathf.Abs(movement.y);
                
                //Debug.Log($"Raw movement magnitudes - |X|: {absX}, |Y|: {absY}");

                float xDirection = 0;
                float yDirection = 0;

                // Determine dominant direction based on magnitude
                if (absX > absY && absX > movementThreshold)
                {
                    // Horizontal movement is dominant
                    xDirection = movement.x;
                    yDirection = 0;
                    //Debug.Log("Horizontal movement dominant");
                }
                else if (absY > absX && absY > movementThreshold)
                {
                    // Vertical movement is dominant
                    xDirection = 0;
                    yDirection = movement.y;
                    //Debug.Log("Vertical movement dominant");
                }
                else if (absX > movementThreshold)
                {
                    // If exactly diagonal, prioritize horizontal
                    xDirection = movement.x;
                    yDirection = 0;
                    //Debug.Log("Diagonal movement, prioritizing horizontal");
                }
                
                // Update movement direction
                animator.SetFloat(horizontalParam, xDirection);
                animator.SetFloat(verticalParam, yDirection);

                // Debug.Log($"Final animation values - DirectionX: {xDirection}, DirectionY: {yDirection}");
                // Debug.Log($"Actual animator values - DirectionX: {animator.GetFloat(horizontalParam)}, DirectionY: {animator.GetFloat(verticalParam)}");
            }
        }

        private void SetWalkingAnimation(bool isWalking)
        {
            if (animator != null)
            {
                animator.SetBool(isWalkingParam, isWalking);
                
                // Reset direction parameters when stopping
                if (!isWalking)
                {
                    animator.SetFloat(horizontalParam, 0);
                    animator.SetFloat(verticalParam, 0);
                }
                
                // Debug.Log($"Setting walking animation - IsWalking: {isWalking}, " +
                //          $"DirectionX: {animator.GetFloat(horizontalParam)}, " +
                //          $"DirectionY: {animator.GetFloat(verticalParam)}");
            }
            else
            {
                Debug.LogWarning("Animator is null when trying to set walking animation");
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
            Debug.Log($"[Customer {customerData?.CustomerID}] Destroying customer object");
            StopAllCoroutines();
            // Clean up any resources if needed
        }

        public void SetAnimatorController(RuntimeAnimatorController controller)
        {
            if (animator == null)
                animator = GetComponent<Animator>();
            if (animator != null && controller != null)
            {
                animator.runtimeAnimatorController = controller;
            }
            else
            {
                Debug.LogWarning("Animator or controller is null when trying to set animator controller.");
            }
        }

        /// <summary>
        /// Sets the speed multiplier for the NavMeshAgent. Use for fast forward effects.
        /// </summary>
        /// <param name="multiplier">Multiplier for agent speed (e.g., 2.0 for double speed)</param>
        public void SetSpeedMultiplier(float multiplier)
        {
            if (agent != null && baseAgentSpeed > 0f)
            {
                agent.speed = baseAgentSpeed * multiplier;
            }
        }

        
    }
} 