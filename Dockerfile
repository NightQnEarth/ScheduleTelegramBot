FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build-env
WORKDIR /app

COPY . ./

FROM mcr.microsoft.com/dotnet/core/runtime:2.2 AS runtime
WORKDIR /app
COPY --from=build-env /app ./
CMD NETCORE_URLS=http://*:$PORT dotnet ScheduleTelegramBot.dll