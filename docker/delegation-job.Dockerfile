FROM mcr.microsoft.com/dotnet/sdk:9.0 AS Delegationjob
WORKDIR /app
COPY . ./
RUN dotnet restore ./api/CcsSso.Core.DelegationJobScheduler/CcsSso.Core.DelegationJobScheduler.csproj
COPY api/CcsSso.Core.DelegationJobScheduler/appsecrets.json /app/appsecrets.json
COPY api/CcsSso.Core.DelegationJobScheduler/appsettings.json /app/appsettings.json
RUN dotnet build --configuration Release ./api/CcsSso.Core.DelegationJobScheduler/CcsSso.Core.DelegationJobScheduler.csproj
EXPOSE 5000
ENTRYPOINT ["dotnet","api/CcsSso.Core.DelegationJobScheduler/bin/Release/net9.0/CcsSso.Core.DelegationJobScheduler.dll"]
