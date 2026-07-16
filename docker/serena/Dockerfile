# Extends the upstream Serena MCP image with the .NET 10 SDK.
#
# Serena's C# language server (Roslyn-based) requires a matching .NET runtime to start.
# The stock ghcr.io/oraios/serena:latest image ships no .NET, so on a .NET 10 project it fails with:
#   "Required .NET runtime version 10.0 not found (installed versions: [])"
# which disables every semantic C# tool (find_symbol, find_referencing_symbols, get_symbols_overview, ...).
# Layering the SDK here fixes that while keeping the upstream entrypoint/command intact.
FROM ghcr.io/oraios/serena:latest

USER root

# ICU is needed by the .NET runtime for globalization; curl/ca-certificates for the install script.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates libicu-dev \
    && rm -rf /var/lib/apt/lists/* \
    && curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && chmod +x /tmp/dotnet-install.sh \
    && /tmp/dotnet-install.sh --channel 10.0 --install-dir /usr/share/dotnet \
    && rm /tmp/dotnet-install.sh \
    && ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet

ENV DOTNET_ROOT=/usr/share/dotnet \
    PATH="/usr/share/dotnet:${PATH}" \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Fail the build early if .NET didn't install correctly.
RUN dotnet --info
