# Isolated environments for the live integration suite (scripts/integration.sh). Each final stage is one
# server configuration; the suite runs inside the container so ValheimServer launches the real binary as a
# local child process, exactly as the app does.
#
#   vanilla  - .NET 10 SDK + the Valheim dedicated server (Steam app 896660, current public build)
#   bepinex  - vanilla + BepInExPack_Valheim + one popular server-side plugin, pinned by version and sha256
#
# The server download is cached as a layer; scripts/integration.sh --update-server re-runs it to pick up a
# new Valheim build. Third-party mods are downloaded at build time and never stored in this repository.

FROM steamcmd/steamcmd:ubuntu-24 AS server
# Changing this value invalidates the cached download (integration.sh --update-server passes a timestamp).
ARG SERVER_CACHE_BUST=0
RUN echo "server download ${SERVER_CACHE_BUST}" \
    && steamcmd +force_install_dir /opt/valheim +login anonymous +app_update 896660 validate +quit \
    && grep -E '"buildid"' /opt/valheim/steamapps/appmanifest_896660.acf | tr -dc '0-9' > /opt/valheim/vsg-buildid.txt \
    && rm -rf /opt/valheim/steamapps

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS vanilla
# Runtime libraries the dedicated server needs (per Iron Gate's own docker/Dockerfile in the server install).
RUN apt-get update \
    && apt-get install -y --no-install-recommends libatomic1 libpulse0 unzip \
    && rm -rf /var/lib/apt/lists/*
COPY --from=server /opt/valheim /opt/valheim
# The suite runs as the host user (not root), so the install must be readable and executable by anyone.
RUN chmod -R a+rX /opt/valheim
ENV VSG_IT_SERVER_EXE=/opt/valheim/valheim_server.x86_64 \
    VSG_IT_SERVER_CONFIG=vanilla \
    VSG_IT_EXPECTED_PLUGIN=

FROM vanilla AS bepinex
# The mod loader (Thunderstore denikson/BepInExPack_Valheim). Its archive holds the server-folder files under
# BepInExPack_Valheim/.
ARG BEPINEX_PACK_URL=https://thunderstore.io/package/download/denikson/BepInExPack_Valheim/5.4.2351/
ARG BEPINEX_PACK_SHA256=bce631497976a93977ceb08e166712e6c31d15244956f89f17df092a9b62e29f
# The plugin (Thunderstore JereKuusela/Server_devcommands, Unlicense). Its DLL sits at the archive root.
ARG PLUGIN_URL=https://thunderstore.io/package/download/JereKuusela/Server_devcommands/1.115.0/
ARG PLUGIN_SHA256=b2752c18342bc9cabf4734e3480cd2cc4aa9faa8955ba503d4f7cefcfa3e4b33
ARG PLUGIN_DIR=JereKuusela-Server_devcommands
# BepInEx writes its config, cache and LogOutput.log under BepInEx/ at runtime, hence a+rwX there.
RUN set -e; cd /tmp \
    && curl -fsSL -o pack.zip "$BEPINEX_PACK_URL" \
    && echo "$BEPINEX_PACK_SHA256  pack.zip" | sha256sum -c - \
    && unzip -q pack.zip -d pack \
    && cp -a pack/BepInExPack_Valheim/. /opt/valheim/ \
    && curl -fsSL -o plugin.zip "$PLUGIN_URL" \
    && echo "$PLUGIN_SHA256  plugin.zip" | sha256sum -c - \
    && mkdir -p "/opt/valheim/BepInEx/plugins/$PLUGIN_DIR" \
    && unzip -q plugin.zip '*.dll' -d "/opt/valheim/BepInEx/plugins/$PLUGIN_DIR" \
    && rm -rf /tmp/pack /tmp/pack.zip /tmp/plugin.zip \
    && chmod -R a+rwX /opt/valheim/BepInEx
# What the BepInEx chainloader prints for the plugin: "Loading [<Name> <Version>]", from the plugin's own
# [BepInPlugin] attribute (version 1.115, not the package's 1.115.0). Update it with PLUGIN_URL.
ENV VSG_IT_SERVER_CONFIG=bepinex \
    VSG_IT_EXPECTED_PLUGIN="Server Devcommands 1.115"
