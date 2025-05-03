using UnityEngine;

namespace Market
{
    public interface ICustomerQueueable
    {
        bool TryEnqueueCustomer(CustomerVisual customer);
        bool TryDequeueCustomer(out CustomerVisual customer);
        Vector3 GetQueuePosition(int queueIndex);
        int GetCurrentQueueLength();
        int GetMaxQueueLength();
    }

    public interface ISellerSpawnable
    {
        Transform GetSellerSpawnPoint();
        void SpawnSeller();
        void DespawnSeller();
    }
} 