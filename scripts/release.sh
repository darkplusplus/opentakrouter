#!/bin/bash
set -e

SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
DIST_DIR="$SCRIPT_DIR/../dist"

mkdir -p $DIST_DIR

dotnet clean
dotnet build dpp.opentakrouter -c Release

# windows
dotnet publish dpp.opentakrouter -c Release -r win10-x64 --self-contained=true 
pushd ./dpp.opentakrouter/bin/Release/net5.0/win10-x64/publish/
zip -r $DIST_DIR/opentakrouter-$VERSION-$HASH.zip .
popd

# linux
dotnet publish dpp.opentakrouter -c Release -r linux-x64 --self-contained=true
pushd ./dpp.opentakrouter/bin/Release/net5.0/linux-x64/publish/
tar -czvf $DIST_DIR/opentakrouter-$VERSION-$HASH.tar.gz .
popd