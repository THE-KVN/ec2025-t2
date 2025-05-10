FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY ./ .

RUN dotnet restore
RUN dotnet publish NETCoreBot.csproj --configuration Debug --output ./publish
RUN find ./publish

FROM mcr.microsoft.com/dotnet/runtime:8.0

WORKDIR /app

COPY --from=build /app/publish/ .

CMD ["dotnet", "NETCoreBot.dll"]