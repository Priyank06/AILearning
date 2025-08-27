#!/bin/bash

echo "💾 AI Learning Space Report"
echo "=========================="

# Color codes
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

print_good() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_alert() {
    echo -e "${RED}🚨 $1${NC}"
}

# Check overall disk usage
USAGE_PERCENT=$(df -h ~ | awk 'NR==2 {print $5}' | sed 's/%//')
AVAILABLE_SPACE=$(df -h ~ | awk 'NR==2 {print $4}')

echo "Overall MacBook Storage:"
echo "========================"
df -h ~ | head -2

echo ""
echo "AI Learning Project Breakdown:"
echo "=============================="
if [ -d ~/AILearning ]; then
    du -sh ~/AILearning/* 2>/dev/null | sort -hr
    echo ""
    TOTAL_SIZE=$(du -sh ~/AILearning 2>/dev/null | cut -f1)
    print_info "Total AI Learning usage: $TOTAL_SIZE"
else
    print_warning "AILearning directory not found"
fi

echo ""
echo "Space Status:"
echo "============="
if [ $USAGE_PERCENT -lt 70 ]; then
    print_good "Plenty of space available ($AVAILABLE_SPACE free)"
elif [ $USAGE_PERCENT -lt 85 ]; then
    print_warning "Moderate usage ($USAGE_PERCENT% used, $AVAILABLE_SPACE free)"
    echo "          Consider running cleanup soon"
else
    print_alert "High usage ($USAGE_PERCENT% used, $AVAILABLE_SPACE free)"
    echo "          Run cleanup immediately: ~/AILearning/Scripts/cleanup.sh"
fi

# Check NuGet cache size
if [ -d ~/.nuget ]; then
    NUGET_SIZE=$(du -sh ~/.nuget 2>/dev/null | cut -f1)
    echo ""
    print_info "NuGet cache size: $NUGET_SIZE"
    NUGET_BYTES=$(du -s ~/.nuget 2>/dev/null | cut -f1)
    if [ $NUGET_BYTES -gt 5242880 ]; then  # > 5GB in KB
        print_warning "NuGet cache is large. Consider running: dotnet nuget locals all --clear"
    fi
fi

echo ""
echo "Quick Actions:"
echo "=============="
echo "Clean projects:  ~/AILearning/Scripts/cleanup.sh"
echo "Archive old PoC: ~/AILearning/Scripts/archive.sh [project-name]"
echo "Check this:      ~/AILearning/Scripts/space-check.sh"
