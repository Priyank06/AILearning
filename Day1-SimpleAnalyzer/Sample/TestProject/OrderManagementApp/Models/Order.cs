using System;
using System.Collections.Generic;

namespace OrderManagementApp
{
    public class Order
    {
        public Guid Id { get; set; }
        public int CustomerId { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<Product> Products { get; set; } = new List<Product>();
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
    }
    
    public enum OrderStatus
    {
        Pending,
        Processing,
        Shipped,
        Delivered,
        Cancelled
    }
}