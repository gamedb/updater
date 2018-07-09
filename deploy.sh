#!/bin/sh

cd ../

echo "### Pulling"
git fetch origin
git reset --hard origin/master

echo "### Building"
docker build -t steam-updater .
docker run -p 9090:80 --name "steam-updater" --restart="unless-stopped" --rm steam-updater

echo "### Talking to Rollbar"
curl https://api.rollbar.com/api/1/deploy/ \
  -F access_token=${STEAM_PROXY_ROLLBAR_PRIVATE} \
  -F environment=${ENV} \
  -F revision=$(git log -n 1 --pretty=format:"%H") \
  -F local_username=Jleagle \
  --silent > /dev/null

#echo "### Restarting"
#/etc/init.d/steam-updater restart # Needs pkill SteamUpdater

#dotnet publish --runtime osx-x64 --self-contained --configuration release
#dotnet publish --runtime debian-x64 --self-contained --configuration release
# Run /steam-updater/bin/Release/netcoreapp2.0/osx-x64/publish/SteamUpdater
