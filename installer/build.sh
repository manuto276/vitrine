#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ISS_FILE="$SCRIPT_DIR/vitrine.iss"

# Check if running on Windows (native iscc) or Linux (wine)
if command -v iscc >/dev/null 2>&1; then
    echo "Building installer with native Inno Setup..."
    iscc "$ISS_FILE"
elif [ -f /opt/innosetup/ISCC.exe ] && command -v wine >/dev/null 2>&1; then
    echo "Building installer with Wine + Inno Setup..."

    # Convert Linux paths to Wine paths in a temp copy
    WORK_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
    WINE_WORK="Z:$(echo "$WORK_DIR" | sed 's|/|\\|g')"
    WINE_ISS="Z:$(echo "$ISS_FILE" | sed 's|/|\\|g')"

    wine /opt/innosetup/ISCC.exe "$WINE_ISS" /O"${WINE_WORK}\\publish\\installer" 2>&1 | grep -v "^wine:" || true

    echo ""
    echo "Installer built: publish/installer/"
    ls -lh "$WORK_DIR/publish/installer/"*.exe 2>/dev/null || echo "Warning: installer exe not found"
else
    echo "Error: Inno Setup not found."
    echo ""
    echo "Options:"
    echo "  - Rebuild the devcontainer (includes Wine + Inno Setup)"
    echo "  - On Windows: install Inno Setup from https://jrsoftware.org/isinfo.php"
    exit 1
fi
