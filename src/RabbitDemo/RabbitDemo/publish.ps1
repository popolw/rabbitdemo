#remove-item bin/* -recurse
dotnet clean
dotnet restore
dotnet publish -c release -r win10-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true 