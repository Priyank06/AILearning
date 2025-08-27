#!/bin/bash

echo "🧹 AI Learning Environment Cleanup"
echo "=================================="

# Color codes
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m'

print_status() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

print_size() {
    echo -e "${YELLOW}📊 $1${NC}"
}

# Show initial sizes
print_info "Current project sizes:"
du -sh ~/AILearning/* 2>/dev/null | head -10

echo ""
print_info "Starting cleanup process..."

# Clean build outputs
print_info "Cleaning build outputs (bin/obj folders)..."
CLEANED_DIRS=0
find ~/AILearning -name "bin" -type d 2>/dev/null | while read dir; do
    rm -rf "$dir"
    ((CLEANED_DIRS++))
done
find ~/AILearning -name "obj" -type d 2>/dev/null | while read dir; do
    rm -rf "$dir"
    ((CLEANED_DIRS++))
done
print_status "Build outputs cleaned"

# Clean NuGet cache
print_info "Cleaning NuGet cache..."
dotnet nuget locals all --clear > /dev/null 2>&1
print_status "NuGet cache cleared"

# Clean temporary files
print_info "Cleaning temporary files..."
rm -rf ~/AILearning/*/temp 2>/dev/null
rm -rf ~/AILearning/*/*.tmp 2>/dev/null
rm -rf ~/AILearning/*/*.log 2>/dev/null
print_status "Temporary files cleaned"

# Clean old test projects (older than 7 days)
print_info "Cleaning old test projects..."
find ~/AILearning/Learning -name "*test*" -type d -mtime +7 -exec rm -rf {} + 2>/dev/null
find ~/AILearning/Learning -name "*experiment*" -type d -mtime +7 -exec rm -rf {} + 2>/dev/null
print_status "Old test projects cleaned"

echo ""
print_info "Cleanup complete! New sizes:"
du -sh ~/AILearning/* 2>/dev/null | head -10

echo ""
print_size "Total AI Learning space usage:"
du -sh ~/AILearning 2>/dev/null

# Check available disk space
AVAILABLE=$(df -h ~ | awk 'NR==2 {print $4}')
print_size "Available disk space: $AVAILABLE"

echo ""
print_status "Cleanup completed successfully!"
