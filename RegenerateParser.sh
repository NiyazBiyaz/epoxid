dotnet build ./src/PySharp.SyntaxAnalysis.Generator -c Release
cd ./src/PySharp/SyntaxAnalysis
rm -rf ./Generated
../../PySharp.SyntaxAnalysis.Generator/bin/Release/net10.0/pegennet Python.ebnf -o Generated --split-files
cd ../../..
