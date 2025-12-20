# Sample Code Files for Multi-Language Analysis

This folder contains sample code files in different programming languages for testing the multi-language analysis capabilities of the Legacy Analyzer.

## Files Included

### Python (`UserService.py`)
- **Classes**: `UserService`, `AuthenticationService`
- **Patterns**: SQL injection risks, hardcoded credentials, weak validation, performance issues (nested loops, no pagination)
- **Security Issues**: Plain text passwords, hardcoded API keys, no rate limiting
- **Performance Issues**: Loading all records, inefficient nested loops

### JavaScript (`CustomerManager.js`)
- **Classes**: `CustomerManager`, `PaymentService`
- **Patterns**: SQL injection risks, global variables, callback hell, race conditions
- **Security Issues**: Hardcoded credentials, weak password validation, plain text storage
- **Performance Issues**: No pagination, nested loops, inefficient algorithms

### TypeScript (`OrderProcessor.ts`)
- **Classes**: `OrderProcessor`, `PaymentGateway`
- **Interfaces**: `Order`, `OrderItem`
- **Patterns**: SQL injection risks, type safety issues (using `any`), async/await without error handling
- **Security Issues**: Hardcoded secrets, no transaction locking
- **Performance Issues**: Loading all records, nested loops

### Java (`ProductService.java`)
- **Classes**: `ProductService`, `Product`, `InventoryService`, `ProductUtils`
- **Patterns**: SQL injection risks, resource leaks, null pointer exceptions, thread safety issues
- **Security Issues**: Hardcoded API keys, weak validation
- **Performance Issues**: No pagination, nested loops, inefficient data structures

### Go (`user_service.go`)
- **Structs**: `UserService`, `User`, `AuthenticationService`
- **Patterns**: SQL injection risks, global variables, potential panics, weak error handling
- **Security Issues**: Hardcoded credentials, plain text passwords, no rate limiting
- **Performance Issues**: Loading all records, nested loops, inefficient algorithms

## Common Patterns Across All Files

### Security Issues
1. **SQL Injection**: String concatenation in SQL queries
2. **Hardcoded Credentials**: API keys and secrets in source code
3. **Weak Validation**: Minimal input validation
4. **Plain Text Storage**: Passwords stored without hashing
5. **No Rate Limiting**: Authentication without throttling

### Performance Issues
1. **No Pagination**: Loading all records into memory
2. **Nested Loops**: O(n²) complexity algorithms
3. **Inefficient Queries**: Fetching more data than needed
4. **No Caching Strategy**: Repeated database calls

### Architecture Issues
1. **Tight Coupling**: Direct database connections in service classes
2. **Global State**: Shared mutable state
3. **Error Handling**: Missing or weak error handling
4. **Resource Management**: Unclosed connections, potential leaks

## Usage

These files can be uploaded to the Legacy Analyzer to test:
- Language detection
- Code structure extraction
- Pattern detection across languages
- Security vulnerability identification
- Performance issue detection
- Multi-language AI analysis

## Testing Recommendations

1. **Single File Analysis**: Upload one file at a time to test language-specific analysis
2. **Multi-File Analysis**: Upload files from different languages to test mixed-language project analysis
3. **Pattern Detection**: Verify that security and performance patterns are detected across all languages
4. **AI Analysis**: Test that AI prompts work correctly with different language structures

## Notes

- These are intentionally simplified examples with common legacy code patterns
- Real-world codebases may have more complex structures
- Some patterns may be language-specific (e.g., callback hell in JavaScript, goroutines in Go)
- The analyzer should detect and report these patterns appropriately for each language

