# Isolated environments for the live integration suite (scripts/integration.sh). Each final stage is one
# server configuration; the suite runs inside the container so ValheimServer launches the real binary as a
# local child process, exactly as the app does.
#
#   vanilla           - .NET 10 SDK + the Valheim dedicated server (Steam app 896660, current public build)
#   bepinex           - vanilla + BepInExPack_Valheim + the plugins in server-configs.env, pinned by version and sha256
#   bepinex-external  - the same pack + plugin outside the server folder, loaded via the configured paths
#
# The server download is cached as a layer; scripts/integration.sh --update-server re-runs it to pick up a
# new Valheim build. The bepinex pins live in server-configs.env (shared with the Windows VM image) and arrive
# here as build args. Third-party mods are downloaded at build time and never stored in this repository.

FROM steamcmd/steamcmd:ubuntu-24 AS server
# Changing this value invalidates the cached download (integration.sh --update-server passes a timestamp).
ARG SERVER_CACHE_BUST=0
RUN echo "server download ${SERVER_CACHE_BUST}" \
    && steamcmd +force_install_dir /opt/valheim +login anonymous +app_update 896660 validate +quit \
    && grep -E '"buildid"' /opt/valheim/steamapps/appmanifest_896660.acf | tr -dc '0-9' > /opt/valheim/vsg-buildid.txt \
    && rm -rf /opt/valheim/steamapps

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS vanilla
# Runtime libraries the dedicated server needs (per Iron Gate's own docker/Dockerfile in the server install, which
# gets libpulse-mainloop-glib0 through libpulse-dev). Without it PlayFab Party's libparty.so does not load, so a
# crossplay server never registers and never gets an invite code.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libatomic1 libpulse0 libpulse-mainloop-glib0 unzip \
    && rm -rf /var/lib/apt/lists/*
COPY --from=server /opt/valheim /opt/valheim
# The suite runs as the host user (not root), so the install must be readable and executable by anyone.
RUN chmod -R a+rX /opt/valheim
# The BepInEx paths are where the pack goes in the server folder, as the app's defaults would point; on vanilla
# nothing is there, so the server must start unmodded.
ENV VSG_IT_SERVER_EXE=/opt/valheim/valheim_server.x86_64 \
    VSG_IT_SERVER_CONFIG=vanilla \
    VSG_IT_EXPECTED_PLUGINS= \
    VSG_IT_BEPINEX_PRELOADER=/opt/valheim/BepInEx/core/BepInEx.Preloader.dll \
    VSG_IT_DOORSTOP_LIBRARY=/opt/valheim/doorstop_libs/libdoorstop_x64.so

# The pack and plugin, downloaded and verified once for both bepinex configurations.
FROM vanilla AS bepinex-files
# Values come from server-configs.env, which documents each one.
ARG BEPINEX_PACK_URL
ARG BEPINEX_PACK_SHA256
ARG PLUGINS
ARG EXPECTED_PLUGINS
RUN set -e; cd /tmp \
    && test -n "$BEPINEX_PACK_SHA256" && test -n "$PLUGINS" && test -n "$EXPECTED_PLUGINS" \
    && curl -fsSL -o pack.zip "$BEPINEX_PACK_URL" \
    && echo "$BEPINEX_PACK_SHA256  pack.zip" | sha256sum -c - \
    && unzip -q pack.zip -d pack \
    && mkdir -p /opt/bepinex-files \
    && cp -a pack/BepInExPack_Valheim/. /opt/bepinex-files/ \
    && for plugin in $PLUGINS; do \
         dir="${plugin%%|*}"; rest="${plugin#*|}"; url="${rest%%|*}"; sha="${rest#*|}"; \
         curl -fsSL -o plugin.zip "$url" \
         && echo "$sha  plugin.zip" | sha256sum -c - \
         && mkdir -p "/opt/bepinex-files/BepInEx/plugins/$dir" \
         && unzip -q plugin.zip '*.dll' -d "/opt/bepinex-files/BepInEx/plugins/$dir" \
         && rm plugin.zip || exit 1; \
       done \
    && rm -rf /tmp/pack /tmp/pack.zip

# The pack installed into the server folder, the usual way.
FROM vanilla AS bepinex
ARG EXPECTED_PLUGINS
COPY --from=bepinex-files /opt/bepinex-files/ /opt/valheim/
# BepInEx writes its config, cache and LogOutput.log under BepInEx/ at runtime, hence a+rwX there.
RUN chmod -R a+rwX /opt/valheim/BepInEx
ENV VSG_IT_SERVER_CONFIG=bepinex \
    VSG_IT_EXPECTED_PLUGINS="$EXPECTED_PLUGINS"

# The pack kept outside the server folder, loaded through the configured preloader and Doorstop paths.
FROM vanilla AS bepinex-external
ARG EXPECTED_PLUGINS
COPY --from=bepinex-files /opt/bepinex-files/ /opt/bepinex-external/
RUN chmod -R a+rX /opt/bepinex-external && chmod -R a+rwX /opt/bepinex-external/BepInEx
ENV VSG_IT_SERVER_CONFIG=bepinex \
    VSG_IT_EXPECTED_PLUGINS="$EXPECTED_PLUGINS" \
    VSG_IT_BEPINEX_PRELOADER=/opt/bepinex-external/BepInEx/core/BepInEx.Preloader.dll \
    VSG_IT_DOORSTOP_LIBRARY=/opt/bepinex-external/doorstop_libs/libdoorstop_x64.so
