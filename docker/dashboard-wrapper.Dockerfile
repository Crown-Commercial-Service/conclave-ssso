FROM mcr.microsoft.com/dotnet/sdk:6.0 AS CoreAPI
WORKDIR /app
COPY . ./
RUN dotnet restore ./api/CcsSso.Core.Api/CcsSso.Core.Api.csproj
COPY api/CcsSso.Core.Api/appsecrets-template.json /app/appsecrets.json
COPY api/CcsSso.Core.Api/appsettings.json /app/appsettings.json
RUN dotnet build --configuration Release ./api/CcsSso.Core.Api/CcsSso.Core.Api.csproj
EXPOSE 5000
ENTRYPOINT ["dotnet","api/CcsSso.Core.Api/bin/Release/net6.0/CcsSso.Core.Api.dll"]