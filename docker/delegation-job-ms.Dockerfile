FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS Delegationjob
WORKDIR /app
COPY . ./
RUN dotnet restore ./api/CcsSso.Core.DelegationJobScheduler/CcsSso.Core.DelegationJobScheduler.csproj
COPY api/CcsSso.Core.DelegationJobScheduler/appsecrets-template.json /app/appsecrets.json
COPY api/CcsSso.Core.DelegationJobScheduler/appsettings.json /app/appsettings.json
RUN dotnet build --configuration Release ./api/CcsSso.Core.DelegationJobScheduler/CcsSso.Core.DelegationJobScheduler.csproj
EXPOSE 5000
ENTRYPOINT ["dotnet","api/CcsSso.Core.DelegationJobScheduler/bin/Release/net8.0/CcsSso.Core.DelegationJobScheduler.dll"]
