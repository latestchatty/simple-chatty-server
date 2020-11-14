#!/bin/bash
set -euxo pipefail
cd ../src/
ASPNETCORE_ENVIRONMENT=Development dotnet watch run -c Debug
