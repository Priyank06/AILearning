/**
 * Sample Java code for multi-language analysis testing.
 * Contains classes, methods, and common legacy Java patterns.
 */

package com.example.service;

import java.util.*;
import java.sql.*;
import java.util.concurrent.ConcurrentHashMap;

public class ProductService {
    private String dbConnection;
    private String apiKey = "hardcoded-api-key-java123"; // Security issue
    private Map<Integer, Product> productsCache;
    
    public ProductService(String dbConnection) {
        this.dbConnection = dbConnection;
        this.productsCache = new ConcurrentHashMap<>();
    }
    
    // Method with potential SQL injection
    public Product getProductById(int productId) throws SQLException {
        String query = "SELECT * FROM products WHERE id = " + productId; // SQL injection risk
        Connection conn = DriverManager.getConnection(dbConnection);
        Statement stmt = conn.createStatement();
        ResultSet rs = stmt.executeQuery(query);
        
        if (rs.next()) {
            return new Product(
                rs.getInt("id"),
                rs.getString("name"),
                rs.getDouble("price")
            );
        }
        return null;
    }
    
    // Method with performance issues - loading all products
    public List<Product> getAllProducts() {
        List<Product> products = new ArrayList<>();
        // Loading all products without pagination
        for (int i = 0; i < 100000; i++) {
            products.add(new Product(i, "Product " + i, i * 10.0));
        }
        return products;
    }
    
    // Method with nested loops - performance concern
    public List<Product> findRelatedProducts(List<Product> products) {
        List<Product> related = new ArrayList<>();
        for (Product product : products) {
            for (Product other : products) {
                if (product.getCategoryId() == other.getCategoryId() 
                    && product.getId() != other.getId()) {
                    related.add(product);
                }
            }
        }
        return related;
    }
    
    // Method with weak validation
    public Product createProduct(String name, double price, String category) {
        if (name == null || name.length() < 2) { // Weak validation
            throw new IllegalArgumentException("Product name too short");
        }
        
        if (price < 0) {
            throw new IllegalArgumentException("Price cannot be negative");
        }
        
        Product product = new Product(
            productsCache.size() + 1,
            name,
            price
        );
        product.setCategory(category);
        
        productsCache.put(product.getId(), product);
        return product;
    }
    
    // Method with potential null pointer exception
    public void updateProductPrice(int productId, double newPrice) {
        Product product = productsCache.get(productId);
        // No null check
        product.setPrice(newPrice);
    }
    
    // Method with resource leak potential
    public void processProducts() {
        Connection conn = null;
        try {
            conn = DriverManager.getConnection(dbConnection);
            // Missing finally block to close connection
            Statement stmt = conn.createStatement();
            ResultSet rs = stmt.executeQuery("SELECT * FROM products");
            // Connection not closed in finally block
        } catch (SQLException e) {
            e.printStackTrace();
        }
    }
}

class Product {
    private int id;
    private String name;
    private double price;
    private String category;
    private int categoryId;
    
    public Product(int id, String name, double price) {
        this.id = id;
        this.name = name;
        this.price = price;
    }
    
    // Getters and setters
    public int getId() { return id; }
    public void setId(int id) { this.id = id; }
    
    public String getName() { return name; }
    public void setName(String name) { this.name = name; }
    
    public double getPrice() { return price; }
    public void setPrice(double price) { this.price = price; }
    
    public String getCategory() { return category; }
    public void setCategory(String category) { this.category = category; }
    
    public int getCategoryId() { return categoryId; }
    public void setCategoryId(int categoryId) { this.categoryId = categoryId; }
}

class InventoryService {
    private Map<Integer, Integer> inventory;
    
    public InventoryService() {
        this.inventory = new HashMap<>();
    }
    
    // Method with potential race condition
    public synchronized void updateInventory(int productId, int quantity) {
        // Synchronized but could have better concurrency control
        int current = inventory.getOrDefault(productId, 0);
        inventory.put(productId, current + quantity);
    }
    
    // Method without synchronization
    public void addStock(int productId, int quantity) {
        // Not thread-safe
        int current = inventory.getOrDefault(productId, 0);
        inventory.put(productId, current + quantity);
    }
}

// Top-level function equivalent (static method)
class ProductUtils {
    public static double calculateTotalValue(List<Product> products) {
        double total = 0.0;
        for (Product product : products) {
            total += product.getPrice();
        }
        return total;
    }
}

