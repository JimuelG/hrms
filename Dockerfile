FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Core/Core.csproj Core/
COPY Application/Application.csproj Application/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
COPY API/API.csproj API/
RUN dotnet restore API/API.csproj
COPY Core/ Core/
COPY Application/ Application/
COPY Infrastructure/ Infrastructure/
COPY API/ API/
RUN dotnet publish API/API.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM build AS bundle
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet ef migrations bundle -p Infrastructure/Infrastructure.csproj -s API/API.csproj \
    -o /bundle/efbundle --self-contained -r linux-x64

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS migrator
WORKDIR /migrate
COPY --from=bundle /bundle/efbundle .
COPY --from=build /src/API/appsettings.json .
USER $APP_UID
ENTRYPOINT ["./efbundle"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN mkdir -p /data/keys /data/uploads && chown -R $APP_UID:$APP_UID /data
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DataProtection__KeysPath=/data/keys \
    FileStorage__LocalPath=/data/uploads
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "API.dll"]