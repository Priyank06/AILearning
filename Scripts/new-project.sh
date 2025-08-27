#!/bin/bash

if [ $# -eq 0 ]; then
    echo "Usage: $0 <project-name> [maui|console|blazor]"
    echo "Example: $0 MyAIApp maui"
    exit 1
fi

PROJECT_NAME=$1
PROJECT_TYPE=${2:-console}

cd ~/AILearning

case $PROJECT_TYPE in
    maui)
        echo "🚀 Creating MAUI project: $PROJECT_NAME"
        dotnet new maui -n "$PROJECT_NAME"
        cd "$PROJECT_NAME"
        dotnet add package Microsoft.CodeAnalysis.CSharp --version 4.8.0
        dotnet add package CommunityToolkit.Maui --version 7.0.1
        ;;
    console)
        echo "🚀 Creating Console project: $PROJECT_NAME"
        dotnet new console -n "$PROJECT_NAME"
        cd "$PROJECT_NAME"
        dotnet add package Microsoft.CodeAnalysis.CSharp --version 4.8.0
        ;;
    blazor)
        echo "🚀 Creating Blazor project: $PROJECT_NAME"
        dotnet new blazorserver -n "$PROJECT_NAME"
        cd "$PROJECT_NAME"
        dotnet add package Microsoft.CodeAnalysis.CSharp --version 4.8.0
        ;;
    *)
        echo "❌ Unknown project type: $PROJECT_TYPE"
        echo "Available types: maui, console, blazor"
        exit 1
        ;;
esac

echo "✅ Project created: $PROJECT_NAME"
echo "📁 Location: $(pwd)"
echo "🔧 Added packages: Microsoft.CodeAnalysis.CSharp"
echo ""
echo "Next steps:"
echo "1. cd ~/AILearning/$PROJECT_NAME"
echo "2. code ."
echo "3. dotnet run"
