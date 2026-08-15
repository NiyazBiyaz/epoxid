set -x

dotnet build ./src/Epoxid.SyntaxAnalysis.Generator -c Release
cd ./src/Epoxid/SyntaxAnalysis
rm -rf ./Generated
../../Epoxid.SyntaxAnalysis.Generator/bin/Release/net10.0/pegennet Python.ebnf -o Generated --split-files
cd ../../..
