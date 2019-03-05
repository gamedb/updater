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
COPY ./health-check.sh .
RUN apk update && apk add libc6-compat
RUN touch google-auth.json && touch last-changenumber.txt
ENTRYPOINT ["dotnet", "Updater.dll"]
HEALTHCHECK --interval=1m --timeout=5s --start-period=2m --retries=2 CMD ./health-check.sh
