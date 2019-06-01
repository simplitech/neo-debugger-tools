#!/bin/bash
set -ev
git clone https://github.com/AvaloniaUI/AvaloniaEdit.git
dotnet remove NEODebuggerUI package Avalonia.AvaloniaEdit 
dotnet add ./NEODebuggerUI/NEODebuggerUI.csproj reference AvaloniaEdit/src/AvaloniaEdit/AvaloniaEdit.csproj 
dotnet restore
dotnet build -c Release