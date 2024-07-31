dotnet publish -r win-x64 -p:PublishSingleFile=true --self-contained true --output ./out
git push origin tag TagName
