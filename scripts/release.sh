#!/bin/bash
set -e

if [[ -z "$CI" ]]; then
    echo "Sorry, but this assumes running in a Github Action" 1>&2
    exit 1
fi

VERSION=$GITHUB_REF_NAME

SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
DIST_DIR="$SCRIPT_DIR/../dist"

mkdir -p $DIST_DIR

dotnet clean

# win10-x64
ARCH=win10-x64
dotnet publish dpp.opentakrouter -c Release -r $ARCH --self-contained=true -p:PublishSingleFile=true
pushd ./dpp.opentakrouter/bin/Release/net5.0/$ARCH/publish/
zip -r $DIST_DIR/opentakrouter-$VERSION-$ARCH.zip .
popd

# linux-x64
ARCH=linux-x64
dotnet publish dpp.opentakrouter -c Release -r $ARCH --self-contained=true -p:PublishSingleFile=true
pushd ./dpp.opentakrouter/bin/Release/net5.0/$ARCH/publish/
tar -czvf $DIST_DIR/opentakrouter-$VERSION-$ARCH.tar.gz .
popd

# linux-arm64
ARCH=linux-arm64
dotnet publish dpp.opentakrouter -c Release -r $ARCH --self-contained=true -p:PublishSingleFile=true
pushd ./dpp.opentakrouter/bin/Release/net5.0/$ARCH/publish/
tar -czvf $DIST_DIR/opentakrouter-$VERSION-$ARCH.tar.gz .
popd
