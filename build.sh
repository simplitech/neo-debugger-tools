#!/bin/sh
set -ev
dotnet restore
dotnet publish NEODebuggerUI/NEODebuggerUI.csproj -c release -r osx-x64 -f netcoreapp2.2
dotnet publish NEODebuggerUI/NEODebuggerUI.csproj -c release -r win-x64 -f netcoreapp2.2
dotnet publish NEODebuggerUI/NEODebuggerUI.csproj -c release -r linux-x64 -f netcoreapp2.2
cd NEODebuggerUI/bin/release/netcoreapp2.2/osx-x64/publish
zip -r osx-x64.zip ./*
cd -
cd NEODebuggerUI/bin/release/netcoreapp2.2/win-x64/publish
zip -r win-x64.zip ./*
cd -
cd NEODebuggerUI/bin/release/netcoreapp2.2/linux-x64/publish
zip -r linux-x64.zip ./*
cd -