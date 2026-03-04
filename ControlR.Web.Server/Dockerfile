# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt update
RUN apt -y install curl
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["ControlR.Web.Server/ControlR.Web.Server.csproj", "ControlR.Web.Server/"]
COPY ["ControlR.Web.Client/ControlR.Web.Client.csproj", "ControlR.Web.Client/"]
COPY ["ControlR.Web.ServiceDefaults/ControlR.Web.ServiceDefaults.csproj", "ControlR.Web.ServiceDefaults/"]
COPY ["Libraries/ControlR.Libraries.DataRedaction/ControlR.Libraries.DataRedaction.csproj", "Libraries/ControlR.Libraries.DataRedaction/"]
COPY ["Libraries/ControlR.Libraries.Messenger.Extensions/ControlR.Libraries.Messenger.Extensions.csproj", "Libraries/ControlR.Libraries.Messenger.Extensions/"]
COPY ["Libraries/ControlR.Libraries.Shared/ControlR.Libraries.Shared.csproj", "Libraries/ControlR.Libraries.Shared/"]
COPY ["Libraries/ControlR.Libraries.Signalr.Client/ControlR.Libraries.Signalr.Client.csproj", "Libraries/ControlR.Libraries.Signalr.Client/"]
COPY ["Libraries/ControlR.Libraries.Viewer.Common/ControlR.Libraries.Viewer.Common.csproj", "Libraries/ControlR.Libraries.Viewer.Common/"]
COPY ["Libraries/ControlR.Libraries.WebSocketRelay.Client/ControlR.Libraries.WebSocketRelay.Client.csproj", "Libraries/ControlR.Libraries.WebSocketRelay.Client/"]
COPY ["Libraries/ControlR.Libraries.WebSocketRelay.Common/ControlR.Libraries.WebSocketRelay.Common.csproj", "Libraries/ControlR.Libraries.WebSocketRelay.Common/"]
COPY ["ControlR.Agent/ControlR.Agent.csproj", "ControlR.Agent/"]
COPY ["ControlR.Agent.Common/ControlR.Agent.Common.csproj", "ControlR.Agent.Common/"]
COPY ["Libraries/ControlR.Libraries.DevicesCommon/ControlR.Libraries.DevicesCommon.csproj", "Libraries/ControlR.Libraries.DevicesCommon/"]
COPY ["Libraries/ControlR.Libraries.Ipc/ControlR.Libraries.Ipc.csproj", "Libraries/ControlR.Libraries.Ipc/"]
COPY ["Libraries/ControlR.Libraries.NativeInterop.Unix/ControlR.Libraries.NativeInterop.Unix.csproj", "Libraries/ControlR.Libraries.NativeInterop.Unix/"]
COPY ["Libraries/ControlR.Libraries.NativeInterop.Windows/ControlR.Libraries.NativeInterop.Windows.csproj", "Libraries/ControlR.Libraries.NativeInterop.Windows/"]
RUN dotnet restore "./ControlR.Web.Server/ControlR.Web.Server.csproj"
RUN dotnet restore "./ControlR.Agent/ControlR.Agent.csproj"
COPY . .

# Build agents for each platform and place in wwwroot/downloads
ARG CURRENT_VERSION
RUN dotnet publish "./ControlR.Agent/ControlR.Agent.csproj" -c Release -r win-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeAllContentForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true \
    -p:Version=$CURRENT_VERSION -p:FileVersion=$CURRENT_VERSION \
    -o /src/ControlR.Web.Server/wwwroot/downloads/win-x64/

RUN dotnet publish "./ControlR.Agent/ControlR.Agent.csproj" -c Release -r win-x86 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeAllContentForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true \
    -p:Version=$CURRENT_VERSION -p:FileVersion=$CURRENT_VERSION \
    -o /src/ControlR.Web.Server/wwwroot/downloads/win-x86/

RUN dotnet publish "./ControlR.Agent/ControlR.Agent.csproj" -c Release -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeAllContentForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true \
    -p:Version=$CURRENT_VERSION -p:FileVersion=$CURRENT_VERSION \
    -o /src/ControlR.Web.Server/wwwroot/downloads/linux-x64/

RUN echo "$CURRENT_VERSION" > /src/ControlR.Web.Server/wwwroot/downloads/Version.txt

WORKDIR "/src/ControlR.Web.Server"
RUN dotnet build "./ControlR.Web.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG CURRENT_VERSION
RUN dotnet publish "./ControlR.Web.Server.csproj" -c $BUILD_CONFIGURATION -p:Version=$CURRENT_VERSION -p:FileVersion=$CURRENT_VERSION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p novnc
ENTRYPOINT ["dotnet", "ControlR.Web.Server.dll"]

HEALTHCHECK \
  CMD curl -f http://localhost:8080/health || exit 1