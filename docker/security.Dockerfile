FROM mcr.microsoft.com/dotnet/sdk:8.0 AS SecurityAPI
WORKDIR /app
COPY . ./
RUN dotnet restore ./api/CcsSso.Security.Api/CcsSso.Security.Api.csproj
COPY api/CcsSso.Security.Api/appsecrets.json /app/appsecrets.json
COPY api/CcsSso.Security.Api/appsettings.json /app/appsettings.json
COPY api/CcsSso.Security.Api/Static /app/Static/
RUN dotnet build --configuration Release ./api/CcsSso.Security.Api/CcsSso.Security.Api.csproj
EXPOSE 5000
ENTRYPOINT ["dotnet","api/CcsSso.Security.Api/bin/Release/net8.0/CcsSso.Security.Api.dll"]
