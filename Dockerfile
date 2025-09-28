# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY *.sln ./
COPY src/PLCUnifiedSimulator.Core/*.csproj ./src/PLCUnifiedSimulator.Core/
COPY src/PLCUnifiedSimulator.Protocols.Mitsubishi/*.csproj ./src/PLCUnifiedSimulator.Protocols.Mitsubishi/
COPY src/PLCUnifiedSimulator.Protocols.Omron/*.csproj ./src/PLCUnifiedSimulator.Protocols.Omron/
COPY src/PLCUnifiedSimulator.Simulators/*.csproj ./src/PLCUnifiedSimulator.Simulators/
COPY src/PLCUnifiedSimulator.Console/*.csproj ./src/PLCUnifiedSimulator.Console/
COPY tests/PLCUnifiedSimulator.Tests/*.csproj ./tests/PLCUnifiedSimulator.Tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Run tests
RUN dotnet test --configuration Release --no-restore

# Build and publish
RUN dotnet publish src/PLCUnifiedSimulator.Console/PLCUnifiedSimulator.Console.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --gecos '' --uid 1000 plcuser && \
    chown -R plcuser:plcuser /app
USER plcuser

# Copy published application
COPY --from=build --chown=plcuser:plcuser /app/publish .

# Expose default ports for PLC simulators
# Q/L/iQ-R series
EXPOSE 5000-5002 5010-5012
# FX5U series
EXPOSE 5020-5021
# QnA series
EXPOSE 5030-5031
# A series
EXPOSE 5040
# FX series
EXPOSE 5050

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD timeout 5s netstat -tuln | grep -q ':5000 ' || exit 1

# Entry point
ENTRYPOINT ["dotnet", "PLCUnifiedSimulator.Console.dll"]

# Metadata
LABEL org.opencontainers.image.title="PLC Unified Simulator"
LABEL org.opencontainers.image.description="Multi-protocol PLC simulator supporting Mitsubishi and Omron protocols"
LABEL org.opencontainers.image.vendor="PLCUnifiedSimulator"
LABEL org.opencontainers.image.version="1.0.0"