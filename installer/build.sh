#!/usr/bin/env bash
# Build the Vitrine Windows installer using NSIS.
# Runs on Linux (devcontainer) or Windows (native).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

VERSION="${VERSION:-1.1.1}"
SOURCE_DIR="$PROJECT_ROOT/publish/release"
OUTPUT_DIR="$PROJECT_ROOT/publish/installer"

# Verify the release build exists
if [ ! -f "$SOURCE_DIR/Vitrine.exe" ]; then
    echo "ERROR: Release build not found at $SOURCE_DIR/Vitrine.exe"
    echo "Run 'make release' first."
    exit 1
fi

# Ensure output directory exists
mkdir -p "$OUTPUT_DIR"

# Detect makensis
if ! command -v makensis &>/dev/null; then
    echo "ERROR: makensis not found."
    echo "Install NSIS: sudo apt install nsis"
    exit 1
fi

echo "Building Vitrine installer v${VERSION}..."
echo "  Source:  $SOURCE_DIR"
echo "  Output:  $OUTPUT_DIR"

makensis \
    -DVERSION="$VERSION" \
    -DSOURCE_DIR="$SOURCE_DIR" \
    -DOUTPUT_DIR="$OUTPUT_DIR" \
    "$SCRIPT_DIR/vitrine.nsi"

INSTALLER="$OUTPUT_DIR/VitrineSetup-${VERSION}.exe"

if [ -f "$INSTALLER" ]; then
    SIZE=$(du -h "$INSTALLER" | cut -f1)
    echo ""
    echo "Installer built successfully: $INSTALLER ($SIZE)"
else
    echo "ERROR: Installer was not created."
    exit 1
fi
