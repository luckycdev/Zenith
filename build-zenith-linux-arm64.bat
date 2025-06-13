@echo off
echo Building for linux-arm64...
dotnet publish GettingOverItMP.Server\GettingOverItMP.Server.csproj -c Debug -r linux-arm64 --self-contained true /p:PublishSingleFile=true -o GettingOverItMP.Server\bin\Debug\net5.0\linux-arm64\publish
echo Done! Server files are located at GettingOverItMP.Server/bin/Debug/net5.0/linux-arm64/publish
pause