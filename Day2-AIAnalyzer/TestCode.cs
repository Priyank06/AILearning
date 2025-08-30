using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LegacyApp
{
    public class CustomerService
    {
        private List<Customer> _customers;
        private string _dataFilePath = "customers.txt";
        
        public CustomerService()
        {
            _customers = new List<Customer>();
            LoadCustomers();
        }
        
        public void AddCustomer(Customer customer)
        {
            if (customer != null)
            {
                _customers.Add(customer);
                SaveToFile(customer);
            }
        }
        
        public Customer FindById(int id)
        {
            foreach(var customer in _customers)
            {
                if(customer.Id == id) 
                    return customer;
            }
            return null;
        }
        
        public List<Customer> GetActiveCustomers()
        {
            List<Customer> activeCustomers = new List<Customer>();
            foreach(var customer in _customers)
            {
                if(customer.IsActive && customer.CreatedDate > DateTime.Now.AddYears(-5))
                {
                    activeCustomers.Add(customer);
                }
            }
            return activeCustomers;
        }
        
        public Customer[] GetCustomersByName(string nameFilter)
        {
            List<Customer> matchingCustomers = new List<Customer>();
            foreach(var customer in _customers)
            {
                if(customer.Name != null && customer.Name.ToLower().Contains(nameFilter.ToLower()))
                {
                    matchingCustomers.Add(customer);
                }
            }
            return matchingCustomers.ToArray();
        }
        
        private void LoadCustomers()
        {
            try
            {
                if (File.Exists(_dataFilePath))
                {
                    string[] lines = File.ReadAllLines(_dataFilePath);
                    foreach(string line in lines)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            string[] parts = line.Split('|');
                            if (parts.Length >= 5)
                            {
                                Customer customer = new Customer();
                                customer.Id = int.Parse(parts[0]);
                                customer.Name = parts[1];
                                customer.Email = parts[2];
                                customer.CreatedDate = DateTime.Parse(parts[3]);
                                customer.IsActive = bool.Parse(parts[4]);
                                _customers.Add(customer);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Poor error handling - just log to console
                Console.WriteLine("Error loading customers: " + ex.Message);
            }
        }
        
        private void SaveToFile(Customer customer)
        {
            try
            {
                string customerData = customer.Id + "|" + customer.Name + "|" + 
                                    customer.Email + "|" + customer.CreatedDate + "|" + 
                                    customer.IsActive;
                File.AppendAllText(_dataFilePath, customerData + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving customer: " + ex.Message);
            }
        }
        
        public void DeleteCustomer(int customerId)
        {
            // Inefficient deletion pattern
            for(int i = 0; i < _customers.Count; i++)
            {
                if (_customers[i].Id == customerId)
                {
                    _customers.RemoveAt(i);
                    break;
                }
            }
            // Should also remove from file, but doesn't (bug!)
        }
    }
    
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
        
        public void UpdateEmail(string newEmail)
        {
            if (!string.IsNullOrEmpty(newEmail))
            {
                Email = newEmail;
            }
        }
        
        public string GetDisplayName()
        {
            if (string.IsNullOrEmpty(Name))
                return "Unknown Customer";
            return Name;
        }
    }
    
    public class OrderProcessor
    {
        private CustomerService _customerService;
        
        public OrderProcessor()
        {
            _customerService = new CustomerService();
        }
        
        public bool ProcessOrder(int customerId, decimal amount)
        {
            Customer customer = _customerService.FindById(customerId);
            if (customer == null)
                return false;
                
            if (amount <= 0)
                return false;
                
            // Process the order (legacy synchronous pattern)
            Console.WriteLine("Processing order for " + customer.Name + " - Amount: $" + amount);
            
            // Simulate some business logic
            if (amount > 10000)
            {
                // Requires approval for large orders
                return RequestApproval(customer, amount);
            }
            
            return true;
        }
        
        private bool RequestApproval(Customer customer, decimal amount)
        {
            // Simulate approval process
            Console.WriteLine("Large order requires approval: " + customer.Name + " - $" + amount);
            return true; // Always approve for demo
        }
    }
}