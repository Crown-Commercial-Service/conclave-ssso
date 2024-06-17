FROM mcr.microsoft.com/dotnet/sdk:8.0 AS WrapperAPI
WORKDIR /app
COPY . ./
RUN dotnet restore ./api/CcsSso.Core.ExternalApi/CcsSso.Core.ExternalApi.csproj
COPY api/CcsSso.Core.ExternalApi/appsecrets.json /app/appsecrets.json
COPY api/CcsSso.Core.ExternalApi/appsettings.json /app/appsettings.json
RUN dotnet build --configuration Release ./api/CcsSso.Core.ExternalApi/CcsSso.Core.ExternalApi.csproj
EXPOSE 5000
ENTRYPOINT ["dotnet","api/CcsSso.Core.ExternalApi/bin/Release/net8.0/CcsSso.Core.ExternalApi.dll"]
