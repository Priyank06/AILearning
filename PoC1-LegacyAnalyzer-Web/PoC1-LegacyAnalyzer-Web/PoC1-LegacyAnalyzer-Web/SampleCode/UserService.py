"""
Sample Python code for multi-language analysis testing.
Contains classes, methods, and patterns typical of legacy Python codebases.
"""

import os
import json
from typing import List, Dict, Optional
from datetime import datetime


class UserService:
    """Service for managing user operations."""
    
    def __init__(self, db_connection: str):
        self.db_connection = db_connection
        self.api_key = "hardcoded-secret-key-12345"  # Security issue
        self.users_cache = {}
    
    def get_user_by_id(self, user_id: int) -> Optional[Dict]:
        """Retrieve user by ID with potential SQL injection risk."""
        query = f"SELECT * FROM users WHERE id = {user_id}"  # SQL injection risk
        # Simulated database call
        return {"id": user_id, "name": "Test User"}
    
    def create_user(self, username: str, email: str, password: str) -> Dict:
        """Create a new user with weak password validation."""
        if len(password) < 4:  # Weak validation
            raise ValueError("Password too short")
        
        # No password hashing - security issue
        user = {
            "username": username,
            "email": email,
            "password": password,  # Stored in plain text
            "created_at": datetime.now()
        }
        return user
    
    def update_user_profile(self, user_id: int, profile_data: Dict) -> Dict:
        """Update user profile with nested loops - performance concern."""
        result = {}
        for key, value in profile_data.items():
            for i in range(1000):  # Inefficient nested loop
                if i % 2 == 0:
                    result[key] = value
        return result
    
    def get_all_users(self) -> List[Dict]:
        """Fetch all users without pagination - scalability issue."""
        users = []
        for i in range(10000):  # Loading all users at once
            users.append({"id": i, "name": f"User {i}"})
        return users


class AuthenticationService:
    """Handles user authentication."""
    
    def __init__(self):
        self.session_timeout = 3600
    
    def authenticate(self, username: str, password: str) -> bool:
        """Simple authentication without rate limiting."""
        # No rate limiting - security risk
        if username == "admin" and password == "admin123":  # Hardcoded credentials
            return True
        return False
    
    def generate_token(self, user_id: int) -> str:
        """Generate authentication token."""
        return f"token_{user_id}_{datetime.now().timestamp()}"


def process_user_data(data: List[Dict]) -> Dict:
    """Top-level function for processing user data."""
    total = 0
    for user in data:
        total += user.get("id", 0)
    return {"total": total, "count": len(data)}


if __name__ == "__main__":
    service = UserService("postgresql://localhost/db")
    user = service.get_user_by_id(1)
    print(f"User: {user}")

