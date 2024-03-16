FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Pixel.Api/Pixel.Api.csproj", "Pixel.Api/"]
COPY ["Pixel.Contracts/Pixel.Contracts.csproj", "Pixel.Contracts/"]
RUN dotnet restore "Pixel.Api/Pixel.Api.csproj"
COPY . .
WORKDIR "/src/Pixel.Api"
RUN dotnet build "Pixel.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Pixel.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Pixel.Api.dll"]
