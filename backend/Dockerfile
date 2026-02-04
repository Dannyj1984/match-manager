# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy csproj and restore
COPY ["FairPlay.Api.csproj", "./"]
RUN dotnet restore "FairPlay.Api.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "FairPlay.Api.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "FairPlay.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .

# Set dynamic timestamp behavior for Npgsql
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
RUN apk add --no-cache icu-libs

ENTRYPOINT ["dotnet", "FairPlay.Api.dll"]
