#!/bin/bash
set -e

VERSION=`git branch --show-current`
HASH=`git rev-parse --short HEAD`

SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
DIST_DIR="$SCRIPT_DIR/../dist"

mkdir -p $DIST_DIR

# linux
dotnet publish dpp.opentakrouter -c Release -r linux-x64 --self-contained=true -p:PublishSingleFile=true 
pushd ./dpp.opentakrouter/bin/Release/net5.0/linux-x64/publish/
tar -czvf $DIST_DIR/opentakrouter-$VERSION-$HASH.tar.gz .
popd
