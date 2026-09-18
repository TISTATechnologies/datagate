FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/DataGate.Domain.Shared/ src/DataGate.Domain.Shared/
COPY src/DataGate.Domain/ src/DataGate.Domain/
COPY src/DataGate.Application.Contracts/ src/DataGate.Application.Contracts/
COPY src/DataGate.EntityFrameworkCore/ src/DataGate.EntityFrameworkCore/
COPY src/DataGate.Application/ src/DataGate.Application/
COPY src/DataGate.HttpApi.Host/ src/DataGate.HttpApi.Host/
RUN dotnet publish src/DataGate.HttpApi.Host/DataGate.HttpApi.Host.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
# curl: compose healthcheck hits pending-list
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "DataGate.HttpApi.Host.dll"]
