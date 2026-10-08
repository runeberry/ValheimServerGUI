# Reproducible, cacheable build environment for packaging ValheimServerGUI (v3.0 / Avalonia).
# Everything the package build needs lives here — the host only needs Docker. Build once
# (scripts/package.sh does this automatically and reuses the image); rebuild with --rebuild-image.
FROM mcr.microsoft.com/dotnet/sdk:10.0

# AppImage packaging tooling — shared with the release workflow via install-tools.sh.
COPY install-tools.sh /tmp/install-tools.sh
RUN bash /tmp/install-tools.sh && rm /tmp/install-tools.sh

# appimagetool embeds the arch into the output; pin it so it never has to guess.
ENV ARCH=x86_64
