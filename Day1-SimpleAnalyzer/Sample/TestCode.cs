using System;
using System.Collections.Generic;

namespace LegacyApp
{
    public class CustomerService
    {
        private List<Customer> _customers;
        
        public CustomerService()
        {
            _customers = new List<Customer>();
        }
        
        public void AddCustomer(Customer customer)
        {
            _customers.Add(customer);
        }
        
        public Customer FindById(int id)
        {
            foreach(var customer in _customers)
            {
                if(customer.Id == id) return customer;
            }
            return null;
        }
    }
    
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}