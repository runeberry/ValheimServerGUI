# Reproducible, cacheable build environment for packaging ValheimServerGUI (v3.0 / Avalonia).
# Everything the package build needs lives here — the host only needs Docker. Build once
# (scripts/package.sh does this automatically and reuses the image); rebuild with --rebuild-image.
FROM mcr.microsoft.com/dotnet/sdk:10.0

# AppImage packaging tooling. FUSE isn't available inside the build container, so appimagetool is
# unpacked at image-build time (`--appimage-extract`, which needs no FUSE) and run from its AppRun.
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates wget file zsync desktop-file-utils squashfs-tools \
    && rm -rf /var/lib/apt/lists/* \
    && wget -qO /tmp/appimagetool.AppImage \
        https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage \
    && chmod +x /tmp/appimagetool.AppImage \
    && ( cd /opt && /tmp/appimagetool.AppImage --appimage-extract ) \
    && mv /opt/squashfs-root /opt/appimagetool \
    && printf '#!/bin/sh\nexec /opt/appimagetool/AppRun "$@"\n' > /usr/local/bin/appimagetool \
    && chmod +x /usr/local/bin/appimagetool \
    && rm /tmp/appimagetool.AppImage

# appimagetool embeds the arch into the output; pin it so it never has to guess.
ENV ARCH=x86_64
