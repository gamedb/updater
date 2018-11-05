# Build image
FROM microsoft/dotnet:2.2-sdk-alpine AS build-env
WORKDIR /root
COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

# Runtime image
FROM microsoft/dotnet:2.2-runtime-alpine
WORKDIR /root
COPY --from=build-env /root/out .
RUN touch google-auth.json && touch last-changenumber.txt
ENTRYPOINT ["dotnet", "Updater.dll"]
