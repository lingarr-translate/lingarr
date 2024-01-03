#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Lingarr.Client/nuget.config", "Lingarr.Client/"]
COPY ["Lingarr.Server/Lingarr.Server.csproj", "Lingarr.Server/"]
COPY ["Lingarr.Client/Lingarr.client.esproj", "Lingarr.Client/"]
RUN dotnet restore "./Lingarr.Server/./Lingarr.Server.csproj"
COPY . .
WORKDIR "/src/Lingarr.Server"
RUN dotnet build "./Lingarr.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Lingarr.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Lingarr.Server.dll"]