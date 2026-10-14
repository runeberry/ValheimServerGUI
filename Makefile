# Valheim Server GUI — local dogfood install targets.
#
# Run `make launcher` as YOUR OWN user to add Valheim Server GUI to your
# application menu. Run `make install` to (re)publish the stable build AND
# install the launcher. Override the install location with
# `make INSTALL_DIR=/path ...`.

INSTALL_DIR   ?= /home/developers/Apps/valheim-server-gui
LAUNCHER_DIR  := $(HOME)/.local/share/applications
DESKTOP       := valheim-server-gui.desktop
APPHOST       := $(INSTALL_DIR)/ValheimServerGUI.App
APP_PROJ      := src/ValheimServerGUI.App/ValheimServerGUI.App.csproj

.PHONY: help launcher install run launch uninstall

help:
	@echo "Valheim Server GUI — local install targets:"
	@echo "  make launch      Build & run from source for manual testing"
	@echo "  make install     Publish the stable build + install the launcher"
	@echo "  make launcher    Add Valheim Server GUI to your application menu (run as yourself)"
	@echo "  make run         Launch the installed build"
	@echo "  make uninstall   Remove the launcher from your menu"
	@echo ""
	@echo "  App is published to: $(INSTALL_DIR)"
	@echo "  Launcher installs to: $(LAUNCHER_DIR)"

# Build and run the app straight from source — the manual-testing entry point.
# Build output is redirected per-user by Directory.Build.props (shared-tree collision fix).
launch:
	@dotnet run --project $(APP_PROJ)

# Full setup / update: publish the stable build, then install the launcher.
install:
	@bash scripts/install-local.sh

# The "setup part" — install just the menu shortcut into the current user's menu.
# Regenerates the .desktop from the committed template pointed at $(INSTALL_DIR).
launcher:
	@mkdir -p "$(LAUNCHER_DIR)"
	@sed 's#@INSTALL_DIR@#$(INSTALL_DIR)#g' packaging/$(DESKTOP).in > "$(LAUNCHER_DIR)/$(DESKTOP)"
	@update-desktop-database "$(LAUNCHER_DIR)" 2>/dev/null || true
	@echo "Installed launcher -> $(LAUNCHER_DIR)/$(DESKTOP)"
	@test -x "$(APPHOST)" || echo "NOTE: $(APPHOST) not found yet — run 'make install' to publish it."
	@echo "'Valheim Server GUI' should appear in your application menu shortly."

run:
	@"$(APPHOST)"

uninstall:
	@rm -f "$(LAUNCHER_DIR)/$(DESKTOP)"
	@update-desktop-database "$(LAUNCHER_DIR)" 2>/dev/null || true
	@echo "Removed launcher from $(LAUNCHER_DIR)"
