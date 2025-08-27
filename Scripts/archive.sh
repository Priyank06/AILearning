#!/bin/bash

# Archive old projects to save space

if [ $# -eq 0 ]; then
    echo "Usage: $0 <project-folder-name>"
    echo "Example: $0 PoC1-LegacyAnalyzer"
    echo ""
    echo "Available projects to archive:"
    ls ~/AILearning/ | grep -E "^PoC|Week" | head -10
    exit 1
fi

PROJECT_NAME=$1
PROJECT_PATH="$HOME/AILearning/$PROJECT_NAME"
ARCHIVE_PATH="$HOME/AILearning/Archive"

if [ ! -d "$PROJECT_PATH" ]; then
    echo "❌ Project folder not found: $PROJECT_PATH"
    exit 1
fi

echo "📦 Archiving project: $PROJECT_NAME"

# Create archive directory if it doesn't exist
mkdir -p "$ARCHIVE_PATH"

# Get current date for archive name
DATE=$(date +%Y%m%d)
ARCHIVE_FILE="$ARCHIVE_PATH/${PROJECT_NAME}_${DATE}.tar.gz"

echo "Creating archive: $ARCHIVE_FILE"
tar -czf "$ARCHIVE_FILE" -C "$HOME/AILearning" "$PROJECT_NAME"

if [ $? -eq 0 ]; then
    echo "✅ Archive created successfully"
    echo "📊 Original size: $(du -sh "$PROJECT_PATH" | cut -f1)"
    echo "📊 Archive size:  $(du -sh "$ARCHIVE_FILE" | cut -f1)"
    echo ""
    read -p "Delete original folder? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        rm -rf "$PROJECT_PATH"
        echo "✅ Original folder deleted"
        echo "📊 Space saved: $(du -sh "$ARCHIVE_FILE" | cut -f1)"
    else
        echo "ℹ️  Original folder kept"
    fi
else
    echo "❌ Failed to create archive"
    exit 1
fi
