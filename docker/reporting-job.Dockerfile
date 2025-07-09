FROM mcr.microsoft.com/dotnet/sdk:9.0 AS SecurityAPI
WORKDIR /app
COPY . ./
RUN dotnet restore ./api/CcsSso.Core.ReportingScheduler/CcsSso.Core.ReportingScheduler.csproj
COPY api/CcsSso.Core.ReportingScheduler/appsecrets-template.json /app/appsecrets.json
COPY api/CcsSso.Core.ReportingScheduler/appsettings.json /app/appsettings.json
RUN dotnet build --configuration Release ./api/CcsSso.Core.ReportingScheduler/CcsSso.Core.ReportingScheduler.csproj
EXPOSE 5000
ENTRYPOINT ["dotnet","api/CcsSso.Core.ReportingScheduler/bin/Release/net9.0/CcsSso.Core.ReportingScheduler.dll"]
