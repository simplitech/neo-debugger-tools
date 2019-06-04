#!/bin/bash
set -ev
dotnet restore
dotnet publish -c release -r osx-x64 -f netcoreapp2.2
dotnet publish -c release -r win-x64 -f netcoreapp2.2
dotnet publish -c release -r linux-x64 -f netcoreapp2.2
zip -r osx-x64-$0.zip NEODebuggerUI/bin/Release/netcoreapp2.2/osx-x64/publish
zip -r win-x64-$0.zip NEODebuggerUI/bin/Release/netcoreapp2.2/win-x64/publish
zip -r linux-x64-$0.zip NEODebuggerUI/bin/Release/netcoreapp2.2/linux-x64/publish