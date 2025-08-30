using System;
using System.Collections.Generic;

namespace LegacyApp.Managers
{
    public class ProductManager
    {
        private List<Product> products;
        
        public ProductManager()
        {
            products = new List<Product>();
            LoadProducts();
        }
        
        private void LoadProducts()
        {
            // Simulated data loading
            products.Add(new Product { Id = 1, Name = "Widget A", Price = 19.99m });
            products.Add(new Product { Id = 2, Name = "Widget B", Price = 29.99m });
        }
        
        public Product FindProduct(int id)
        {
            foreach (Product p in products)
            {
                if (p.Id == id)
                    return p;
            }
            return null;
        }
        
        public List<Product> GetExpensiveProducts(decimal minPrice)
        {
            List<Product> result = new List<Product>();
            foreach (Product p in products)
            {
                if (p.Price >= minPrice)
                    result.Add(p);
            }
            return result;
        }
    }
    
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}
