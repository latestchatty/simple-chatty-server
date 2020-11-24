#!/bin/bash
set -euxo pipefail
mkdir -p publish
dotnet publish ../src/SimpleChattyServer.csproj -c Release --self-contained -o publish/ -r linux-arm64
rm -f publish/*.pdb publish/*.config publish/appsettings.Development.json
