FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Storage.Api/Storage.Api.csproj", "Storage.Api/"]
COPY ["Pixel.Contracts/Pixel.Contracts.csproj", "Pixel.Contracts/"]
RUN dotnet restore "Storage.Api/Storage.Api.csproj"
COPY . .
WORKDIR "/src/Storage.Api"
RUN dotnet build "Storage.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Storage.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Storage.Api.dll"]
