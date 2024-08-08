FROM mcr.microsoft.com/dotnet/sdk:8.0 AS AdaptorAPI
WORKDIR /app
COPY . ./
RUN dotnet restore ./api/CcsSso.Adaptor.Api/CcsSso.Adaptor.Api.csproj
COPY api/CcsSso.Adaptor.Api/appsecrets.json /app/appsecrets.json
COPY api/CcsSso.Adaptor.Api/appsettings.json /app/appsettings.json
RUN dotnet build --configuration Release ./api/CcsSso.Adaptor.Api/CcsSso.Adaptor.Api.csproj
EXPOSE 5000
ENTRYPOINT ["dotnet","api/CcsSso.Adaptor.Api/bin/Release/net8.0/CcsSso.Adaptor.Api.dll"]
