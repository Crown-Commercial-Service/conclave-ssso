FROM mcr.microsoft.com/dotnet/sdk:8.0 AS PPONJob
WORKDIR /app
COPY . ./
RUN dotnet restore ./api/CcsSso.Core.PPONScheduler/CcsSso.Core.PPONScheduler.csproj
COPY api/CcsSso.Core.PPONScheduler/appsecrets-template.json /app/appsecrets.json
COPY api/CcsSso.Core.PPONScheduler/appsettings.json /app/appsettings.json
RUN dotnet build --configuration Release ./api/CcsSso.Core.PPONScheduler/CcsSso.Core.PPONScheduler.csproj
EXPOSE 5000
ENTRYPOINT ["dotnet","api/CcsSso.Core.PPONScheduler/bin/Release/net8.0/CcsSso.Core.PPONScheduler.dll"]
