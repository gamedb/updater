FROM microsoft/dotnet
COPY bin/Release/netcoreapp2.0/osx-x64/publish/SteamUpdater /SteamUpdater
COPY last-changenumber.txt /last-changenumber.txt
EXPOSE 8087
CMD ["/SteamUpdater"]
