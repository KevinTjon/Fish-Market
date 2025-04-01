using UnityEngine;
using UnityEngine.InputSystem;

public class BaitManager : MonoBehaviour
{
    [Header("Bait Prefabs")]
    [SerializeField] private GameObject smallBaitPrefab;
    [SerializeField] private GameObject mediumBaitPrefab;
    [SerializeField] private GameObject largeBaitPrefab;

    [Header("References")]
    [SerializeField] private SimpleRodController rodController;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Default small bait with 'B' key
        if (keyboard.bKey.wasPressedThisFrame)
        {
            SpawnBait(smallBaitPrefab);
        }
        // Medium bait with 'N' key
        else if (keyboard.nKey.wasPressedThisFrame)
        {
            SpawnBait(mediumBaitPrefab);
        }
        // Large bait with 'M' key
        else if (keyboard.mKey.wasPressedThisFrame)
        {
            SpawnBait(largeBaitPrefab);
        }
    }

    private void SpawnBait(GameObject baitPrefab)
    {
        if (rodController != null && rodController.CurrentHook != null)
        {
            // Get the SimpleHookController component
            SimpleHookController hookController = rodController.CurrentHook.GetComponent<SimpleHookController>();
            if (hookController != null)
            {
                // Create and attach the bait
                GameObject baitObject = Instantiate(baitPrefab, hookController.transform.position, Quaternion.identity);
                baitObject.transform.SetParent(hookController.transform);
                baitObject.transform.localPosition = Vector3.down * 0.5f; // Offset the bait below the hook
            }
        }
    }
} 