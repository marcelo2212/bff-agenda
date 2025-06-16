FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["BffAgenda.API/BffAgenda.API.csproj", "BffAgenda.API/"]
COPY ["BffAgenda.Application/BffAgenda.Application.csproj", "BffAgenda.Application/"]
COPY ["BffAgenda.Infrastructure/BffAgenda.Infrastructure.csproj", "BffAgenda.Infrastructure/"]
COPY ["BffAgenda.Domain/BffAgenda.Domain.csproj", "BffAgenda.Domain/"]

RUN dotnet restore "BffAgenda.API/BffAgenda.API.csproj"

COPY . .
WORKDIR "/src/BffAgenda.API"
RUN dotnet publish "BffAgenda.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENTRYPOINT ["dotnet", "BffAgenda.API.dll"]
