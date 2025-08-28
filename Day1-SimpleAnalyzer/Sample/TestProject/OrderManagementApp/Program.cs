using System;
using System.Collections.Generic;

namespace OrderManagementApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Order Management System");
            
            var orderService = new OrderService();
            var paymentService = new PaymentService();
            
            // Create sample data
            var customer = new Customer { Id = 1, Name = "John Doe" };
            var products = new List<Product>
            {
                new Product { Id = 1, Name = "Laptop", Price = 999.99m },
                new Product { Id = 2, Name = "Mouse", Price = 25.50m }
            };
            
            // Process order
            orderService.CreateOrder(customer, products);
            Console.WriteLine("Order created successfully!");
        }
    }
}