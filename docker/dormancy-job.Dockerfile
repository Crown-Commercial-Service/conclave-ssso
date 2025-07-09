FROM mcr.microsoft.com/dotnet/sdk:9.0 AS FileuploadJob
WORKDIR /app
COPY . ./
RUN dotnet restore ./api/CcsSso.Core.DormancyJobScheduler/CcsSso.Core.DormancyJobScheduler.csproj
COPY api/CcsSso.Core.DormancyJobScheduler/appsecrets-template.json /app/appsecrets.json
COPY api/CcsSso.Core.DormancyJobScheduler/appsettings.json /app/appsettings.json
RUN dotnet build --configuration Release ./api/CcsSso.Core.DormancyJobScheduler/CcsSso.Core.DormancyJobScheduler.csproj
EXPOSE 5000
ENTRYPOINT ["dotnet","api/CcsSso.Core.DormancyJobScheduler/bin/Release/net9.0/CcsSso.Core.DormancyJobScheduler.dll"]
