#!/bin/bash
set -euxo pipefail
ASPNETCORE_ENVIRONMENT=Development dotnet run --project ../src/SimpleChattyServer.csproj -c Debug
