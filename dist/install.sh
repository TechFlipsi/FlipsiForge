#!/bin/bash
# FlipsiForge Linux Installer v0.2.0
# TechFlipsi (Fabian Kirchweger) — GPL-3.0
set -e

VERSION="0.2.0"
INSTALL_DIR="/opt/flipsiforge"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

echo "╔══════════════════════════════════════════╗"
echo "║   FlipsiForge v${VERSION} Installer       ║"
echo "╚══════════════════════════════════════════╝"
echo ""

# Root check
if [ "$EUID" -ne 0 ]; then
  echo "⚠️  Bitte als root ausführen: sudo ./install.sh"
  exit 1
fi

echo "Was installieren?"
echo "  1) Desktop App (GUI)"
echo "  2) Server (REST API + Web-UI)"
echo "  3) Beides"
read -p "Auswahl [1-3]: " choice

case $choice in
  1|3)
    echo ""
    echo "📦 Installiere Desktop App..."
    mkdir -p "$INSTALL_DIR/desktop"
    cp -r "$SCRIPT_DIR/linux-desktop/"* "$INSTALL_DIR/desktop/"
    chmod +x "$INSTALL_DIR/desktop/FlipsiForge.Desktop"
    ln -sf "$INSTALL_DIR/desktop/FlipsiForge.Desktop" /usr/bin/flipsiforge

    # .desktop file
    mkdir -p /usr/share/applications
    cat > /usr/share/applications/flipsiforge.desktop << 'DESKTOP'
[Desktop Entry]
Name=FlipsiForge
Comment=3D Printer Management
Exec=/opt/flipsiforge/desktop/FlipsiForge.Desktop
Terminal=false
Type=Application
Categories=Utility;3DGraphics;
DESKTOP
    echo "✅ Desktop App installiert → /usr/bin/flipsiforge"
    ;;
esac

case $choice in
  2|3)
    echo ""
    echo "📦 Installiere Server..."
    mkdir -p "$INSTALL_DIR/server"
    cp -r "$SCRIPT_DIR/linux-server/"* "$INSTALL_DIR/server/"
    chmod +x "$INSTALL_DIR/server/FlipsiForge.Server"
    ln -sf "$INSTALL_DIR/server/FlipsiForge.Server" /usr/bin/flipsiforge-server

    # systemd service
    mkdir -p /lib/systemd/system
    cat > /lib/systemd/system/flipsiforge.service << 'SERVICE'
[Unit]
Description=FlipsiForge 3D Printer Management Server
After=network.target

[Service]
Type=simple
ExecStart=/opt/flipsiforge/server/FlipsiForge.Server --urls http://0.0.0.0:5000 --no-launch-profile
Restart=on-failure
RestartSec=5

[Install]
WantedBy=multi-user.target
SERVICE
    systemctl daemon-reload
    echo "✅ Server installiert → /usr/bin/flipsiforge-server"
    echo "   Start: sudo systemctl start flipsiforge"
    echo "   Auto-start: sudo systemctl enable flipsiforge"
    ;;
esac

echo ""
echo "🎉 Installation abgeschlossen!"