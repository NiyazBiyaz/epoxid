#!/bin/bash

set -xe

# Build main project
dotnet publish src/Epoxid -o bin/epoxid-linux

# Build PegenNet (it can, idk)
dotnet publish src/Epoxid.SyntaxAnalysis.Generator -o bin/pegennet-linux
