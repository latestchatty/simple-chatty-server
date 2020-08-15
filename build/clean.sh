#!/bin/bash
set -euxo pipefail
rm -rf publish/
pushd ../src
dotnet clean
popd
