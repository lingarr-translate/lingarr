# Set default build configuration
ARG BUILD_CONFIGURATION=Debug
FROM node:20.10.0-slim AS client

# Prepare Client
COPY ./Lingarr.Client/ /Client
WORKDIR /Client
# Install dependencies
RUN if [ "$BUILD_CONFIGURATION" = "Release" ] ; then npm ci --only=production ; else npm install ; fi
RUN npm run build

# Step 2: Prepare the base image for the server
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Step 3: Build the server
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Debug
ARG TARGETARCH
RUN echo "Current BUILD_CONFIGURATION: $BUILD_CONFIGURATION"
WORKDIR /src
COPY ["Lingarr.Server/Lingarr.Server.csproj", "Lingarr.Server/"]
RUN dotnet restore "./Lingarr.Server/./Lingarr.Server.csproj" --arch $TARGETARCH
COPY . .
WORKDIR "/src/Lingarr.Server"
COPY --from=client /Client/dist/ ./wwwroot
RUN dotnet build "./Lingarr.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build --arch $TARGETARCH

# Step 4: Publish the application
FROM --platform=$BUILDPLATFORM build AS publish
ARG BUILD_CONFIGURATION=Debug
ARG TARGETARCH
RUN echo "Current BUILD_CONFIGURATION: $BUILD_CONFIGURATION"
RUN dotnet publish "./Lingarr.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --arch $TARGETARCH

# Step 5: Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
    
# Start the application
RUN echo '#!/bin/bash\n\
if [ "${PUID:-0}" != "0" ] && [ "${PGID:-0}" != "0" ]; then\n\

    # Create user and group with specified IDs if they dont exist\n\
    groupadd -g ${PGID} lingarr 2>/dev/null || groupmod -g ${PGID} lingarr\n\
    useradd -u ${PUID} -g lingarr -s /bin/bash lingarr 2>/dev/null || usermod -u ${PUID} lingarr\n\
    
    # Set ownership\n\
    chown -R lingarr:lingarr /app\n\
    
    # Run app as lingarr user\n\
    exec su lingarr -c "dotnet Lingarr.Server.dll"\n\
else\n\
    # Run as root\n\
    exec dotnet Lingarr.Server.dll\n\
fi' > /entrypoint.sh && \
    chmod +x /entrypoint.sh

VOLUME /app/config
ENTRYPOINT ["/entrypoint.sh"]