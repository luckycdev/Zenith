@echo off
setlocal

echo Building for win-x64...
dotnet publish GettingOverItMP.Server\GettingOverItMP.Server.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o GettingOverItMP.Server\bin\Release\net5.0\win-x64\publish

echo Building for linux-x64...
dotnet publish GettingOverItMP.Server\GettingOverItMP.Server.csproj -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true -o GettingOverItMP.Server\bin\Release\net5.0\linux-x64\publish

echo Building for linux-arm64...
dotnet publish GettingOverItMP.Server\GettingOverItMP.Server.csproj -c Release -r linux-arm64 --self-contained true /p:PublishSingleFile=true -o GettingOverItMP.Server\bin\Release\net5.0\linux-arm64\publish

echo Building for osx-x64...
dotnet publish GettingOverItMP.Server\GettingOverItMP.Server.csproj -c Release -r osx-x64 --self-contained true /p:PublishSingleFile=true -o GettingOverItMP.Server\bin\Release\net5.0\osx-x64\publish

echo Building for osx-arm64...
dotnet publish GettingOverItMP.Server\GettingOverItMP.Server.csproj -c Release -r osx-arm64 --self-contained true /p:PublishSingleFile=true -o GettingOverItMP.Server\bin\Release\net5.0\osx-arm64\publish

echo Done!
pause
