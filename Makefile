APP_PROJ := src/ValheimServerGUI.App/ValheimServerGUI.App.csproj

.PHONY: launch

# Build and run the app straight from source — the manual-testing entry point.
# Build output goes to a per-user directory (see Directory.Build.props).
launch:
	@dotnet run --project $(APP_PROJ)
