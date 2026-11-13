FROM mcr.microsoft.com/dotnet/sdk:10.0 AS WrapperAPI
WORKDIR /app
COPY . ./
RUN dotnet restore ./api/CcsSso.Core.ExternalApi/CcsSso.Core.ExternalApi.csproj
COPY api/CcsSso.Core.ExternalApi/appsecrets-template.json /app/appsecrets.json
COPY api/CcsSso.Core.ExternalApi/appsettings.json /app/appsettings.json
RUN dotnet build --configuration Release ./api/CcsSso.Core.ExternalApi/CcsSso.Core.ExternalApi.csproj
EXPOSE 8080
ENTRYPOINT ["dotnet","api/CcsSso.Core.ExternalApi/bin/Release/net10.0/CcsSso.Core.ExternalApi.dll"]
