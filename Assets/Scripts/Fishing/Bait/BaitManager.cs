using UnityEngine;

public class BaitManager : MonoBehaviour
{
    [Header("Bait Prefabs")]
    [SerializeField] private GameObject smallBaitPrefab;
    [SerializeField] private GameObject mediumBaitPrefab;
    [SerializeField] private GameObject largeBaitPrefab;

    [Header("References")]
    [SerializeField] private RodController rod;

    private HookController hook;

    private void Awake()
    {
        // Check if the rod is assigned
        if (rod == null)
        {
            Debug.LogError("RodController reference is missing in BaitManager.");
            return;
        }
    }
    
    private void Update()
    {
        // Adds bait to the hook if nothing is attached to the hook
        // Check if we have a valid hook
        hook = rod.hook.GetComponent<HookController>();
            
        if (hook)
        {
            // Check if there's nothing attached to the hook
            if (hook.HasHookedObject == false)
            {
                SpawnBait(smallBaitPrefab);
            }
        }
    }

    private void SpawnBait(GameObject baitPrefab)
    {
        var newBait = Instantiate(baitPrefab).GetComponent<Bait>();
        hook.AttachBait(newBait); // Attach the bait to the hook
    }
}
 