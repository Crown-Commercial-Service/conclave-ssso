FROM mcr.microsoft.com/dotnet/sdk:10.0 AS Orgderegjob
WORKDIR /app
COPY . ./
RUN dotnet restore ./api/CcsSso.Core.JobScheduler/CcsSso.Core.JobScheduler.csproj
COPY api/CcsSso.Core.JobScheduler/appsecrets-template.json /app/appsecrets.json
COPY api/CcsSso.Core.JobScheduler/appsettings.json /app/appsettings.json
RUN dotnet build --configuration Release ./api/CcsSso.Core.JobScheduler/CcsSso.Core.JobScheduler.csproj
EXPOSE 8080
ENTRYPOINT ["dotnet","api/CcsSso.Core.JobScheduler/bin/Release/net10.0/CcsSso.Core.JobScheduler.dll"]
