@echo off
echo Restoring dotnet tools...
dotnet tool restore
dotnet paket restore

dotnet run --project ./build/build.fsproj -- %*
