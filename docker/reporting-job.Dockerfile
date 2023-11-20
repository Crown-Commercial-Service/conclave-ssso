FROM mcr.microsoft.com/dotnet/sdk:6.0.416-bookworm-slim AS SecurityAPI
WORKDIR /app
COPY . ./
RUN dotnet restore ./api/CcsSso.Core.ReportingScheduler/CcsSso.Core.ReportingScheduler.csproj
COPY api/CcsSso.Core.ReportingScheduler/appsecrets.json /app/appsecrets.json
COPY api/CcsSso.Core.ReportingScheduler/appsettings.json /app/appsettings.json
RUN dotnet build --configuration Release ./api/CcsSso.Core.ReportingScheduler/CcsSso.Core.ReportingScheduler.csproj
EXPOSE 5000
ENTRYPOINT ["dotnet","api/CcsSso.Core.ReportingScheduler/bin/Release/net6.0/CcsSso.Core.ReportingScheduler.dll"]
