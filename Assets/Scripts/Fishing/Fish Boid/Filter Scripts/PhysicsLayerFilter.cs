using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Flock/Filter/Physics Layer")]
public class PhysicsLayerFilter : ContextFilter
{
    public LayerMask mask;

    public override List<Transform> Filter(FlockAgent agent, List<Transform> original){
        List<Transform> filtered = new List<Transform>();
        foreach(Transform item in original){
            
            //bit shifting 1 by game object.layer (=5) will reutrn 10000 layermask
            var layerMaskForItem = 1 << item.gameObject.layer;

            //OR it with the mask
            layerMaskForItem = layerMaskForItem | mask;

            //now if the layermaskforitem is same as mask it means the item is included in the mask
            if(mask == layerMaskForItem){
                filtered.Add(item);
            }
        }
        return filtered;
    }
}
