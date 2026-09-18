FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/DataGate.Domain.Shared/ src/DataGate.Domain.Shared/
COPY src/DataGate.Domain/ src/DataGate.Domain/
COPY src/DataGate.GateApi/ src/DataGate.GateApi/
RUN dotnet publish src/DataGate.GateApi/DataGate.GateApi.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "DataGate.GateApi.dll"]
