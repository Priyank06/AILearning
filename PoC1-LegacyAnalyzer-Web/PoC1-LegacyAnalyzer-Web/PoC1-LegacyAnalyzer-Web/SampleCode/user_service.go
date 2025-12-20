// Sample Go code for multi-language analysis testing.
// Contains structs, methods, and common Go patterns.

package main

import (
	"database/sql"
	"fmt"
	"log"
	"time"
)

// Global variable - anti-pattern
var globalAPIKey = "hardcoded-api-key-go456" // Security issue

// UserService handles user operations
type UserService struct {
	dbConnection string
	apiKey       string
	usersCache   map[int]*User
}

// User represents a user entity
type User struct {
	ID        int
	Username  string
	Email     string
	Password  string // Stored in plain text - security issue
	CreatedAt time.Time
}

// NewUserService creates a new UserService instance
func NewUserService(dbConnection string) *UserService {
	return &UserService{
		dbConnection: dbConnection,
		apiKey:       globalAPIKey,
		usersCache:   make(map[int]*User),
	}
}

// GetUserByID retrieves a user by ID with potential SQL injection
func (s *UserService) GetUserByID(userID int) (*User, error) {
	// SQL injection risk
	query := fmt.Sprintf("SELECT * FROM users WHERE id = %d", userID)
	
	db, err := sql.Open("postgres", s.dbConnection)
	if err != nil {
		return nil, err
	}
	defer db.Close()
	
	row := db.QueryRow(query)
	user := &User{}
	err = row.Scan(&user.ID, &user.Username, &user.Email, &user.Password, &user.CreatedAt)
	if err != nil {
		return nil, err
	}
	
	return user, nil
}

// GetAllUsers fetches all users without pagination - performance issue
func (s *UserService) GetAllUsers() ([]*User, error) {
	var users []*User
	// Loading all users at once
	for i := 0; i < 100000; i++ {
		users = append(users, &User{
			ID:       i,
			Username: fmt.Sprintf("user%d", i),
			Email:    fmt.Sprintf("user%d@example.com", i),
		})
	}
	return users, nil
}

// CreateUser creates a new user with weak validation
func (s *UserService) CreateUser(username, email, password string) (*User, error) {
	// Weak password validation
	if len(password) < 3 {
		return nil, fmt.Errorf("password too short")
	}
	
	// No password hashing
	user := &User{
		ID:        len(s.usersCache) + 1,
		Username:  username,
		Email:     email,
		Password:  password, // Stored in plain text
		CreatedAt: time.Now(),
	}
	
	s.usersCache[user.ID] = user
	return user, nil
}

// ProcessUsers has nested loops - performance concern
func (s *UserService) ProcessUsers(users []*User) []*User {
	var processed []*User
	for _, user := range users {
		for _, other := range users {
			if user.ID == other.ID && user.Email == other.Email {
				processed = append(processed, user)
			}
		}
	}
	return processed
}

// UpdateUser updates user without proper error handling
func (s *UserService) UpdateUser(userID int, updates map[string]interface{}) error {
	user, exists := s.usersCache[userID]
	if !exists {
		return fmt.Errorf("user not found")
	}
	
	// No validation of updates
	for key, value := range updates {
		switch key {
		case "username":
			user.Username = value.(string)
		case "email":
			user.Email = value.(string)
		}
	}
	
	return nil
}

// AuthenticationService handles authentication
type AuthenticationService struct {
	sessionTimeout int
}

// NewAuthenticationService creates a new AuthenticationService
func NewAuthenticationService() *AuthenticationService {
	return &AuthenticationService{
		sessionTimeout: 3600,
	}
}

// Authenticate performs authentication without rate limiting
func (a *AuthenticationService) Authenticate(username, password string) bool {
	// No rate limiting - security risk
	if username == "admin" && password == "admin123" { // Hardcoded credentials
		return true
	}
	return false
}

// GenerateToken generates authentication token
func (a *AuthenticationService) GenerateToken(userID int) string {
	return fmt.Sprintf("token_%d_%d", userID, time.Now().Unix())
}

// Top-level function
func calculateTotalUsers(users []*User) int {
	total := 0
	for range users {
		total++
	}
	return total
}

// Function with potential panic
func getUserByIndex(users []*User, index int) *User {
	// No bounds checking
	return users[index] // Potential panic if index out of bounds
}

func main() {
	service := NewUserService("postgres://localhost/db")
	user, err := service.GetUserByID(1)
	if err != nil {
		log.Fatal(err)
	}
	fmt.Printf("User: %+v\n", user)
}

