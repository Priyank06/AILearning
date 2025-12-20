/**
 * Sample JavaScript code for multi-language analysis testing.
 * Contains classes, functions, and common legacy patterns.
 */

const fs = require('fs');
const http = require('http');

// Global variable - anti-pattern
let globalConfig = {
    apiKey: 'hardcoded-api-key-abc123', // Security issue
    databaseUrl: 'mongodb://localhost:27017/mydb'
};

class CustomerManager {
    constructor() {
        this.customers = [];
        this.connectionString = globalConfig.databaseUrl;
    }

    // Method with potential SQL injection
    async getCustomerById(customerId) {
        const query = `SELECT * FROM customers WHERE id = ${customerId}`; // SQL injection risk
        // Simulated database call
        return { id: customerId, name: 'Test Customer' };
    }

    // Method with performance issues
    async getAllCustomers() {
        const customers = [];
        // Loading all customers without pagination
        for (let i = 0; i < 100000; i++) {
            customers.push({
                id: i,
                name: `Customer ${i}`,
                email: `customer${i}@example.com`
            });
        }
        return customers;
    }

    // Method with nested loops - performance concern
    processCustomerData(customers) {
        const result = [];
        for (let i = 0; i < customers.length; i++) {
            for (let j = 0; j < customers.length; j++) {
                if (customers[i].id === customers[j].id) {
                    result.push(customers[i]);
                }
            }
        }
        return result;
    }

    // Method with weak validation
    createCustomer(name, email, password) {
        if (password.length < 3) { // Weak password validation
            throw new Error('Password too short');
        }
        
        // No password hashing
        const customer = {
            name: name,
            email: email,
            password: password, // Stored in plain text
            createdAt: new Date()
        };
        this.customers.push(customer);
        return customer;
    }

    // Method accessing global state
    updateCustomerConfig() {
        globalConfig.apiKey = 'new-key'; // Mutating global state
        return globalConfig;
    }
}

class PaymentService {
    constructor() {
        this.transactions = [];
    }

    // Method with potential race condition
    async processPayment(amount, customerId) {
        // No transaction locking
        const balance = await this.getBalance(customerId);
        if (balance >= amount) {
            return await this.deductAmount(customerId, amount);
        }
        throw new Error('Insufficient funds');
    }

    async getBalance(customerId) {
        // Simulated async call
        return Promise.resolve(1000);
    }

    async deductAmount(customerId, amount) {
        // Simulated async call
        return Promise.resolve({ success: true });
    }
}

// Top-level function
function calculateTotalRevenue(customers) {
    let total = 0;
    customers.forEach(customer => {
        total += customer.revenue || 0;
    });
    return total;
}

// Function with callback hell
function fetchCustomerData(customerId, callback) {
    http.get(`http://api.example.com/customers/${customerId}`, (res) => {
        let data = '';
        res.on('data', (chunk) => {
            data += chunk;
        });
        res.on('end', () => {
            callback(null, JSON.parse(data));
        });
    }).on('error', (err) => {
        callback(err, null);
    });
}

module.exports = {
    CustomerManager,
    PaymentService,
    calculateTotalRevenue,
    fetchCustomerData
};

