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
ENV PUID=1000
ENV PGID=1000
RUN groupadd -g ${PGID:-1000} appgroup && \
    useradd -u ${PUID:-1000} -g appgroup -s /bin/bash appuser && \
    chown -R ${PUID:-1000}:${PGID:-1000} /app
USER ${PUID:-1000}:${PGID:-1000}
    
# Start the application
ENTRYPOINT ["dotnet", "Lingarr.Server.dll"]
