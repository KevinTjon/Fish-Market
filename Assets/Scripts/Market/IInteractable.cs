using UnityEngine;

namespace Market
{
    public interface IInteractable
    {
        bool CanInteract { get; }
        void OnInteractionStart();
        void OnInteractionEnd();
        void OnInteractionUpdate();
        Transform GetTransform();
        float GetInteractionRadius();
    }
} 