using UnityEngine;

namespace Models
{
    public class Seller
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public Customer.SellerType Type { get; set; }
        public bool IsActive { get; set; }
        public float Reputation { get; set; }
    }
} 