FROM mcr.microsoft.com/dotnet/sdk:10.0 AS SecurityAPI
WORKDIR /app
COPY . ./
RUN dotnet restore ./api/CcsSso.Core.ReportingScheduler/CcsSso.Core.ReportingScheduler.csproj
COPY api/CcsSso.Core.ReportingScheduler/appsecrets-template.json /app/appsecrets.json
COPY api/CcsSso.Core.ReportingScheduler/appsettings.json /app/appsettings.json
RUN dotnet build --configuration Release ./api/CcsSso.Core.ReportingScheduler/CcsSso.Core.ReportingScheduler.csproj
EXPOSE 8080
ENTRYPOINT ["dotnet","api/CcsSso.Core.ReportingScheduler/bin/Release/net10.0/CcsSso.Core.ReportingScheduler.dll"]
