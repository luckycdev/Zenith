@echo off
echo Building for win-x64...
dotnet publish GettingOverItMP.Server\GettingOverItMP.Server.csproj -c Debug -r win-x64 --self-contained true /p:PublishSingleFile=true -o GettingOverItMP.Server\bin\Debug\net5.0\win-x64\publish
echo Done! Server files are located at GettingOverItMP.Server/bin/Debug/net5.0/win-x64/publish
pause