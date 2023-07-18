FROM mcr.microsoft.com/dotnet/sdk:6.0 AS AdaptorSQSListener
WORKDIR /app
COPY . ./
RUN dotnet restore ./api/CcsSso.Adaptor.SqsListener/CcsSso.Adaptor.SqsListener.csproj
COPY api/CcsSso.Adaptor.SqsListener/appsecrets.json /app/appsecrets.json
COPY api/CcsSso.Adaptor.SqsListener/appsettings.json /app/appsettings.json
RUN dotnet build --configuration Release ./api/CcsSso.Adaptor.SqsListener/CcsSso.Adaptor.SqsListener.csproj
ENTRYPOINT ["dotnet","api/CcsSso.Adaptor.SqsListener/bin/Release/net6.0/CcsSso.Adaptor.SqsListener.dll"]