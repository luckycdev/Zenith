@echo off
echo Building for linux-x64...
dotnet publish GettingOverItMP.Server\GettingOverItMP.Server.csproj -c Debug -r linux-x64 --self-contained true /p:PublishSingleFile=true -o GettingOverItMP.Server\bin\Debug\net5.0\linux-x64\publish
echo Done! Server files are located at GettingOverItMP.Server/bin/Debug/net5.0/linux-x64/publish
pause