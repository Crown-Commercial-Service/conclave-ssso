FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS FileuploadJob
WORKDIR /app
COPY . ./
RUN dotnet restore ./api/CcsSso.Core.DormancyJobScheduler/CcsSso.Core.DormancyJobScheduler.csproj
COPY api/CcsSso.Core.DormancyJobScheduler/appsecrets.json /app/appsecrets.json
COPY api/CcsSso.Core.DormancyJobScheduler/appsettings.json /app/appsettings.json
RUN dotnet build --configuration Release ./api/CcsSso.Core.DormancyJobScheduler/CcsSso.Core.DormancyJobScheduler.csproj
EXPOSE 5000
ENTRYPOINT ["dotnet","api/CcsSso.Core.DormancyJobScheduler/bin/Release/net8.0/CcsSso.Core.DormancyJobScheduler.dll"]
