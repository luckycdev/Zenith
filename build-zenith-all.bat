@echo off

echo Building for win-x64...
dotnet publish GettingOverItMP.Server\GettingOverItMP.Server.csproj -c Debug -r win-x64 --self-contained true /p:PublishSingleFile=true -o GettingOverItMP.Server\bin\Debug\net5.0\win-x64\publish

echo Building for linux-x64...
dotnet publish GettingOverItMP.Server\GettingOverItMP.Server.csproj -c Debug -r linux-x64 --self-contained true /p:PublishSingleFile=true -o GettingOverItMP.Server\bin\Debug\net5.0\linux-x64\publish

echo Building for linux-arm64...
dotnet publish GettingOverItMP.Server\GettingOverItMP.Server.csproj -c Debug -r linux-arm64 --self-contained true /p:PublishSingleFile=true -o GettingOverItMP.Server\bin\Debug\net5.0\linux-arm64\publish

echo Building for osx-x64...
dotnet publish GettingOverItMP.Server\GettingOverItMP.Server.csproj -c Debug -r osx-x64 --self-contained true /p:PublishSingleFile=true -o GettingOverItMP.Server\bin\Debug\net5.0\osx-x64\publish

echo Done! Each architecture's server files are located at GettingOverItMP.Server/bin/Debug/net5.0/
pause