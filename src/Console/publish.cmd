dotnet publish -r win-x64 -p:PublishSingleFile=true --self-contained true -c Release
dotnet publish -r linux-x64 -p:PublishSingleFile=true --self-contained true -c Release
dotnet publish -r linux-arm64 -p:PublishSingleFile=true --self-contained true -c Release