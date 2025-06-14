FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS FileuploadJob
WORKDIR /app
COPY . ./
RUN dotnet restore ./api/CcsSso.Core.DataMigrationJobScheduler/CcsSso.Core.DataMigrationJobScheduler.csproj
COPY api/CcsSso.Core.DataMigrationJobScheduler/appsecrets-template.json /app/appsecrets.json
COPY api/CcsSso.Core.DataMigrationJobScheduler/appsettings.json /app/appsettings.json
RUN dotnet build --configuration Release ./api/CcsSso.Core.DataMigrationJobScheduler/CcsSso.Core.DataMigrationJobScheduler.csproj
EXPOSE 5000
ENTRYPOINT ["dotnet","api/CcsSso.Core.DataMigrationJobScheduler/bin/Release/net8.0/CcsSso.Core.DataMigrationJobScheduler.dll"]
