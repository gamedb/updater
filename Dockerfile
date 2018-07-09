# Build image
FROM microsoft/dotnet:2.1-sdk AS build-env
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o out

# Runtime image
FROM microsoft/dotnet:2.1-runtime
WORKDIR /app
COPY --from=build-env /app/out .

# Needs to be synced!
COPY last-changenumber.txt /last-changenumber.txt
CMD ["/SteamUpdater"]
