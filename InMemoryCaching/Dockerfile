FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

WORKDIR /src
COPY ./InMemoryCaching.sln .
COPY ./InMemoryCachingUtils/SolarWinds.InMemoryCachingUtils.csproj InMemoryCachingUtils/SolarWinds.InMemoryCachingUtils.csproj
COPY ./InMemoryCacheUser/InMemoryCacheUser.csproj InMemoryCacheUser/InMemoryCacheUser.csproj


RUN dotnet restore "InMemoryCaching.sln"
COPY . .
WORKDIR "/src/."

RUN dotnet publish "InMemoryCaching.sln" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/core/runtime:3.1
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InMemoryCacheUser.dll"]