using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RodController : MonoBehaviour
{
    public FishingLineController line { get; private set; }
    public HookController hook { get; private set; }
    public Transform rodConnection { get; private set; }

    public Cooler fishCooler { get; private set; }

    private bool isCasting;

    

    private const float LineWidth = .03f;
    private const float LinePullConstant = 100f;
    private const float LinePushConstant = 120f;
    private const float LineDamping = 25f;
    private const float ReelSpeed = 3f;
    private const float ReelDownConstant = .6f;


    // --------------------------------------------
    private void Awake()
    {
        line = gameObject.transform.GetChild(0).GetComponent<FishingLineController>();
        hook = gameObject.transform.GetChild(1).GetComponent<HookController>();
        rodConnection = gameObject.transform.parent.GetChild(0).GetChild(3);

        fishCooler = GameObject.FindWithTag("Cooler").GetComponent<Cooler>();

        isCasting = true;    
    }
    
    // Start is called before the first frame update
    void Start()
    {
        line.InitializeLine(rodConnection, hook.transform, LineWidth);
        hook.InitializeHook(hook.GetComponent<Rigidbody2D>());
    }

    public void SetWaterLevel(float waterLevel)
    {
        hook.SetWaterLevel(waterLevel);
    }

    public void ReceiveReelInput(float input)
    {
        // Apply force to hook
        var tensionForce = line.CalculateHookForce(!isCasting);
        hook.AddForce(tensionForce);

        if (hook.onWaterSurface) 
        {
            //Debug.Log("Hook is on the water surface");
            if (input > 0)
            {
                var fishObj = hook.attachedObject;
                if (fishObj != null)
                {
                    Debug.Log(fishObj.name + " caught!");
                    fishCooler.AddFish(fishObj.GetComponent<FishHookable>());
                    Destroy(fishObj);
                    fishCooler.DisplayCooler();
                }
            }
            else if (input < 0)
            {
                hook.DetachHookFromSurface();
            }
        }
        else
        {
            line.AlterLength(input);
            
            var hookY = hook.hookRB.position.y;
            if ((isCasting && hookY < hook.waterLevel) ||
                (!isCasting && hookY > hook.waterLevel))
            {
                line.SetLength(line.currLength);
                hook.AttachHookToSurface();
                isCasting = false;
            }
        }
    }
}
