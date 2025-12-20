/**
 * Sample TypeScript code for multi-language analysis testing.
 * Contains classes, interfaces, and TypeScript-specific patterns.
 */

import { Request, Response } from 'express';
import * as fs from 'fs';

interface Order {
    id: number;
    customerId: number;
    items: OrderItem[];
    total: number;
    status: OrderStatus;
}

interface OrderItem {
    productId: number;
    quantity: number;
    price: number;
}

enum OrderStatus {
    Pending = 'pending',
    Processing = 'processing',
    Completed = 'completed',
    Cancelled = 'cancelled'
}

class OrderProcessor {
    private apiKey: string = 'hardcoded-secret-key-xyz789'; // Security issue
    private orders: Order[] = [];
    private dbConnection: string;

    constructor(dbConnection: string) {
        this.dbConnection = dbConnection;
    }

    // Method with potential SQL injection
    async getOrderById(orderId: number): Promise<Order | null> {
        const query = `SELECT * FROM orders WHERE id = ${orderId}`; // SQL injection risk
        // Simulated database call
        return {
            id: orderId,
            customerId: 1,
            items: [],
            total: 0,
            status: OrderStatus.Pending
        };
    }

    // Method with performance issues - no pagination
    async getAllOrders(): Promise<Order[]> {
        const orders: Order[] = [];
        // Loading all orders at once
        for (let i = 0; i < 50000; i++) {
            orders.push({
                id: i,
                customerId: Math.floor(i / 10),
                items: [],
                total: i * 10,
                status: OrderStatus.Pending
            });
        }
        return orders;
    }

    // Method with nested loops
    processOrders(orders: Order[]): Order[] {
        const processed: Order[] = [];
        for (const order of orders) {
            for (const otherOrder of orders) {
                if (order.customerId === otherOrder.customerId && order.id !== otherOrder.id) {
                    processed.push(order);
                }
            }
        }
        return processed;
    }

    // Method with weak validation
    createOrder(customerId: number, items: OrderItem[]): Order {
        if (items.length === 0) {
            throw new Error('Order must have at least one item');
        }

        const total = items.reduce((sum, item) => sum + (item.price * item.quantity), 0);
        
        const order: Order = {
            id: this.orders.length + 1,
            customerId,
            items,
            total,
            status: OrderStatus.Pending
        };

        this.orders.push(order);
        return order;
    }

    // Method with async/await but no error handling
    async updateOrderStatus(orderId: number, status: OrderStatus): Promise<void> {
        const order = await this.getOrderById(orderId);
        if (order) {
            order.status = status;
            // No error handling for database update
        }
    }
}

class PaymentGateway {
    private credentials: { apiKey: string; secret: string };

    constructor() {
        this.credentials = {
            apiKey: 'public-key-123',
            secret: 'secret-key-456' // Hardcoded credentials
        };
    }

    // Method with potential race condition
    async processPayment(orderId: number, amount: number): Promise<boolean> {
        // No transaction locking
        const balance = await this.checkBalance(orderId);
        if (balance >= amount) {
            return await this.charge(orderId, amount);
        }
        return false;
    }

    private async checkBalance(orderId: number): Promise<number> {
        return Promise.resolve(1000);
    }

    private async charge(orderId: number, amount: number): Promise<boolean> {
        return Promise.resolve(true);
    }
}

// Top-level function
function calculateTotalRevenue(orders: Order[]): number {
    return orders.reduce((total, order) => total + order.total, 0);
}

// Function with type issues
function processOrderData(data: any): Order[] {
    // Using 'any' type - type safety issue
    return data.map((item: any) => ({
        id: item.id,
        customerId: item.customerId,
        items: item.items || [],
        total: item.total || 0,
        status: item.status || OrderStatus.Pending
    }));
}

export {
    OrderProcessor,
    PaymentGateway,
    Order,
    OrderItem,
    OrderStatus,
    calculateTotalRevenue,
    processOrderData
};

