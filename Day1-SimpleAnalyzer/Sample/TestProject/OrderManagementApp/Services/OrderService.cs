using System;
using System.Collections.Generic;

namespace OrderManagementApp
{
    public class OrderService
    {
        private List<Order> _orders = new List<Order>();
        
        public void CreateOrder(Customer customer, List<Product> products)
        {
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                CreatedDate = DateTime.Now,
                Products = products
            };
            _orders.Add(order);
        }
        
        // Legacy pattern - should use LINQ
        public Order FindOrderById(Guid id)
        {
            foreach (var order in _orders)
            {
                if (order.Id == id) return order;
            }
            return null;
        }
    }
}