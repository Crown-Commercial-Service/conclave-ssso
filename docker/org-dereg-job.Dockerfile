FROM mcr.microsoft.com/dotnet/sdk:9.0 AS Orgderegjob
WORKDIR /app
COPY . ./
RUN dotnet restore ./api/CcsSso.Core.JobScheduler/CcsSso.Core.JobScheduler.csproj
COPY api/CcsSso.Core.JobScheduler/appsecrets.json /app/appsecrets.json
COPY api/CcsSso.Core.JobScheduler/appsettings.json /app/appsettings.json
RUN dotnet build --configuration Release ./api/CcsSso.Core.JobScheduler/CcsSso.Core.JobScheduler.csproj
EXPOSE 5000
ENTRYPOINT ["dotnet","api/CcsSso.Core.JobScheduler/bin/Release/net9.0/CcsSso.Core.JobScheduler.dll"]
