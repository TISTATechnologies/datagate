#!/usr/bin/env bash
# Creates the solution file and wires up every project. Run once after cloning.
set -euo pipefail

dotnet new sln -n DataGate --force

dotnet sln add \
  src/DataGate.Domain.Shared/DataGate.Domain.Shared.csproj \
  src/DataGate.Domain/DataGate.Domain.csproj \
  src/DataGate.EntityFrameworkCore/DataGate.EntityFrameworkCore.csproj \
  src/DataGate.Application.Contracts/DataGate.Application.Contracts.csproj \
  src/DataGate.Application/DataGate.Application.csproj \
  src/DataGate.HttpApi.Host/DataGate.HttpApi.Host.csproj \
  workflow/DataGate.Workflow.Activities/DataGate.Workflow.Activities.csproj \
  test/DataGate.Domain.Tests/DataGate.Domain.Tests.csproj \
  test/DataGate.Workflow.Tests/DataGate.Workflow.Tests.csproj

dotnet restore
dotnet build -c Release
dotnet test test/DataGate.Domain.Tests --no-build -c Release

echo
echo "Unit tests are the business spec - they run with no Docker, no database."
echo "Integration tests need the stack: docker compose -f deploy/docker-compose.yml up -d"
