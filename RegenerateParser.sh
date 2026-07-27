CUR_DIR=$(pwd)
cd ./src/PySharp/SyntaxAnalysis
dotnet run --project ../../PySharp.SyntaxAnalysis.Generator -- Python.ebnf -o Generated --split-files
cd $CUR_DIR
